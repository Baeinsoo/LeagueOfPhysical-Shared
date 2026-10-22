using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// 말 타고 활쏘기의 시뮬 코어. 탈것이 머리·속도를 정하고, 같은 틱에 당김·발사가 이어진다.
    /// 화살은 엔티티가 아니라 <see cref="ArcheryShotBook"/>에 담는다(<see cref="ArcheryWorld"/>와 같다).
    /// </summary>
    public class MountedArcheryWorld : GameFramework.World.WorldBase
    {
        private readonly ArcheryAimSystem aimSystem;
        private readonly MountSystem mountSystem;
        private readonly KinematicMoveSystem kinematicMoveSystem;
        private readonly GameFramework.World.IMotionBridge motionBridge;
        private readonly float tickInterval;
        private readonly ArcheryShotBook shotBook = new ArcheryShotBook();

        //  서버가 확정한 남의 화살. 늦게 도착하므로 그보다 앞서 저장된 되감기 저장본에는 없다 —
        //  되감으면 지워지고, 남의 몸은 재생에서 안 굴리므로 다시 생기지도 않는다. 서버 사실이라 되감기 밖에 두고
        //  복원 뒤에 다시 합친다. (ArcheryWorld는 남도 다시 굴려서 재생이 화살을 다시 쏘므로 이 목록이 필요 없다.)
        private readonly List<ArcheryShot> confirmedRemoteShots = new List<ArcheryShot>();

        // 되감기용 보관. 베이스가 SaveCapacity틱보다 오래된 프레임은 못 찾으므로 링버퍼로 충분하다.
        private readonly GameFramework.Netcode.SequenceBuffer<SavedState> saved
            = new GameFramework.Netcode.SequenceBuffer<SavedState>(SaveCapacity);

        private readonly struct SavedState
        {
            public readonly List<ArcheryShot> Shots;
            public readonly Dictionary<string, ArcheryAim> Aims;

            public SavedState(List<ArcheryShot> shots, Dictionary<string, ArcheryAim> aims)
            {
                Shots = shots;
                Aims = aims;
            }
        }

        public IReadOnlyList<ArcheryShot> Shots => shotBook.Shots;

        public MountedArcheryWorld(GameFramework.World.EntityRegistry entityRegistry,
                                   GameFramework.World.WorldEventBuffer eventBuffer,
                                   ArcheryAimSystem aimSystem,
                                   MountSystem mountSystem,
                                   KinematicMoveSystem kinematicMoveSystem,
                                   GameFramework.World.IMotionBridge motionBridge,
                                   float tickInterval)
            : base(entityRegistry, eventBuffer)
        {
            this.aimSystem = aimSystem;
            this.mountSystem = mountSystem;
            this.kinematicMoveSystem = kinematicMoveSystem;
            this.motionBridge = motionBridge;
            this.tickInterval = tickInterval;
        }

        /// <summary>서버가 확정한 남의 발사를 받는다. 내 발사는 예측으로 이미 있으므로 부르는 쪽이 거른다.</summary>
        public void IngestRemoteShot(in ArcheryShot shot)
        {
            if (IndexOf(confirmedRemoteShots, shot.ShooterId, shot.FireTick) < 0)
            {
                confirmedRemoteShots.Add(shot);
            }
            if (IndexOf(shotBook.Shots, shot.ShooterId, shot.FireTick) < 0)
            {
                shotBook.Add(shot);
            }
        }

        /// <summary>그 사람이 (afterTick, upToTick] 구간에 쏜 화살을 지운다. 서버가 안 받아 준 예측 화살 정리용.</summary>
        public void CancelShots(string shooterId, long afterTick, long upToTick)
        {
            shotBook.Cancel(shooterId, afterTick, upToTick);
            confirmedRemoteShots.RemoveAll(s => s.ShooterId == shooterId && s.FireTick > afterTick && s.FireTick <= upToTick);
        }

        protected override void Mutation(long tick, float deltaTime)
        {
            //  머리·속도를 먼저 정한다. 모두의 속도가 정해진 뒤에 움직여야 서로 미는 판정이 순서를 안 탄다.
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>())
                {
                    mountSystem.Tick(entity, deltaTime);
                }
            }

            motionBridge.SyncTransforms();
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>() == false)
                {
                    continue;
                }

                motionBridge.Depenetrate(entity);
                motionBridge.Separate(entity);
                kinematicMoveSystem.Tick(entity, deltaTime);
                motionBridge.PushMotion(entity);
            }

            //  쏘는 건 이동이 끝난 뒤 — 화살이 떠나는 자리가 실제로 선 자리가 된다.
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>() == false)
                {
                    continue;
                }

                var shot = aimSystem.Tick(entity, tick, tickInterval);
                if (shot.HasValue)
                {
                    var fired = shot.Value;
                    shotBook.Add(fired);
                    EventBuffer.Append(new ArcheryShotFiredEvent(
                        fired.ShooterId, fired.FireTick, fired.Origin, fired.Velocity));
                }
            }

            shotBook.RemoveExpired(tick, tickInterval);
            float lifetimeTicks = ArcheryTrajectory.LifetimeSeconds / tickInterval;
            confirmedRemoteShots.RemoveAll(s => tick - s.FireTick > lifetimeTicks);
        }

        protected override void SaveGameState(long tick)
        {
            saved.Record(tick, new SavedState(shotBook.Capture(), ArcheryAimSnapshot.Capture(EntityRegistry)));
        }

        protected override bool LoadGameState(long tick)
        {
            if (saved.TryGet(tick, out var state) == false)
            {
                return false;
            }

            shotBook.Restore(state.Shots);
            foreach (var shot in confirmedRemoteShots)
            {
                if (IndexOf(shotBook.Shots, shot.ShooterId, shot.FireTick) < 0)
                {
                    shotBook.Add(shot);
                }
            }
            ArcheryAimSnapshot.Restore(EntityRegistry, state.Aims);
            return true;
        }

        private static int IndexOf(IReadOnlyList<ArcheryShot> shots, string shooterId, long fireTick)
        {
            for (int i = 0; i < shots.Count; i++)
            {
                if (shots[i].ShooterId == shooterId && shots[i].FireTick == fireTick)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
