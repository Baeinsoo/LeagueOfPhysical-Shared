using GameFramework;
using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// Archery의 시뮬 코어. 매 틱 조준을 갱신하고, 떠난 화살을 목록에 담아 수명이 다하면 지운다.
    /// <b>화살은 엔티티가 아니다</b> — 발사 정보만 있으면 위치가 계산되므로 레지스트리에 넣지 않는다.
    /// </summary>
    public class ArcheryWorld : GameFramework.World.WorldBase
    {
        private readonly ArcheryAimSystem aimSystem;
        private readonly ArcheryCourse course;
        private readonly float tickInterval;
        private readonly MovementSystem movementSystem;
        private readonly KinematicMoveSystem kinematicMoveSystem;
        private readonly GameFramework.World.IMotionBridge motionBridge;
        private readonly List<ArcheryShot> shots = new List<ArcheryShot>();

        // 되감기용 보관. 틱마다 그 시점의 조준 상태와 화살 목록을 통째로 둔다.
        // 링버퍼(SaveCapacity)로 둔다 — 베이스도 최근 SaveCapacity틱보다 오래된 프레임은
        // LoadState에서 아예 못 찾아 여기까지 오지 않으므로, 그보다 오래된 항목은 영원히 안 쓰이는
        // 죽은 무게가 된다.
        private readonly GameFramework.Netcode.SequenceBuffer<SavedState> saved
            = new GameFramework.Netcode.SequenceBuffer<SavedState>(SaveCapacity);

        private readonly struct SavedState
        {
            public readonly List<ArcheryShot> Shots;
            public readonly Dictionary<string, ArcheryAim> Aims;
            public readonly Dictionary<string, (int Remaining, int RefilledWave)> Quivers;

            public SavedState(List<ArcheryShot> shots, Dictionary<string, ArcheryAim> aims,
                              Dictionary<string, (int Remaining, int RefilledWave)> quivers)
            {
                Shots = shots;
                Aims = aims;
                Quivers = quivers;
            }
        }

        public IReadOnlyList<ArcheryShot> Shots => shots;

        /// <summary>
        /// 서버가 확정한 남의 발사를 받아들인다. 내 발사는 예측으로 이미 목록에 있으므로
        /// 부르는 쪽이 걸러서 넣는다(같은 발이 두 번 그려지지 않게).
        /// </summary>
        public void IngestRemoteShot(in ArcheryShot shot)
        {
            shots.Add(shot);
        }

        public ArcheryWorld(GameFramework.World.EntityRegistry entityRegistry,
                            GameFramework.World.WorldEventBuffer eventBuffer,
                            ArcheryAimSystem aimSystem,
                            ArcheryCourse course,
                            float tickInterval,
                            MovementSystem movementSystem,
                            KinematicMoveSystem kinematicMoveSystem,
                            GameFramework.World.IMotionBridge motionBridge)
            : base(entityRegistry, eventBuffer)
        {
            this.aimSystem = aimSystem;
            this.course = course;
            this.tickInterval = tickInterval;
            this.movementSystem = movementSystem;
            this.kinematicMoveSystem = kinematicMoveSystem;
            this.motionBridge = motionBridge;
        }

        protected override void Mutation(long tick, float deltaTime)
        {
            //  자리마다 화살을 다시 채운다. 쏘기 **전에** 해야 그 자리의 첫 발이 바로 나간다.
            int wave = course.IndexAt(tick, GameplayStartTick);

            Walk(deltaTime, tick);

            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>() == false)
                {
                    continue;
                }

                Refill(entity, wave);

                var shot = aimSystem.Tick(entity, tick, tickInterval);
                if (shot.HasValue)
                {
                    var fired = shot.Value.WithWind(course.WindAt(tick, GameplayStartTick));
                    shots.Add(fired);

                    // 남은 이 사건으로만 내 발사를 안다 — 입력 메아리는 늦게 와서 안 읽힌다.
                    // 바람은 싣지 않는다 — 받는 쪽이 발사 틱으로 다시 계산한다.
                    EventBuffer.Append(new ArcheryShotFiredEvent(
                        fired.ShooterId, fired.FireTick, fired.Origin, fired.Velocity));
                }
            }

            RemoveExpired(tick);
        }

        /// <summary>
        /// 사수를 <b>자기 사대 안에서</b> 걷게 한다. 조준(그리고 화살이 떠나는 자리)보다 먼저
        /// 해야 쏜 자리가 실제로 서 있던 자리가 된다.
        ///
        /// <para><b>속도가 0이면 통째로 건너뛴다</b> — 이동을 안 켠 맵(원형)에서 중력만 돌아
        /// 사수가 바닥으로 떨어지는 일이 없게. 즉 이 슬라이스 이전 동작이 기본값이다.</para>
        ///
        /// <para>속도 계산과 실제 이동을 <b>두 번에 나눠</b> 도는 것은 공용 월드와 같은 모양이다 —
        /// 모두의 속도가 정해진 뒤에 움직여야 서로를 밀어내는 판정이 순서를 안 탄다.</para>
        /// </summary>
        private void Walk(float deltaTime, long tick)
        {
            if (course.MoveSpeed <= 0f)
            {
                return;
            }

            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>() == false)
                {
                    continue;
                }

                //  공용 이동 시스템은 걷는 쪽으로 몸을 돌린다(보통은 맞다). 활쏘기에서 몸은
                //  **겨누는 쪽**을 봐야 하므로 되돌려 둔다 — 회전의 주인은 ArcheryAimSystem
                //  하나다(두 곳이 쓰면 마지막에 쓴 쪽이 이기는, 순서에 기대는 코드가 된다).
                var transform = entity.Get<GameFramework.World.Transform>();
                var facing = transform == null
                    ? default(System.Numerics.Quaternion)
                    : transform.Rotation;

                movementSystem.Tick(entity, tick, deltaTime);

                if (transform != null)
                {
                    transform.Rotation = facing;
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
                ClampToStance(entity);
            }
        }

        /// <summary>
        /// 사대 밖으로 나갔으면 되돌려 놓는다. <b>사대를 모르는 몸은 건드리지 않는다</b> —
        /// 기준이 없는데 원점으로 끌면 맵 한복판으로 순간이동한다.
        /// </summary>
        private void ClampToStance(GameFramework.World.Entity entity)
        {
            var stance = entity.Get<ArcheryStance>();
            var transform = entity.Get<GameFramework.World.Transform>();
            if (stance == null || transform == null)
            {
                return;
            }

            transform.Position = ArcheryShootingBox.Clamp(
                transform.Position.ToUnity(), stance.Origin, stance.Facing,
                course.ShootingBoxHalfWidth, course.ShootingBoxHalfDepth).ToNumerics();
        }

        /// <summary>
        /// 자리가 바뀌었으면 그 자리 몫으로 화살을 다시 채운다. <b>남은 것은 안 넘어간다</b> —
        /// 넘기면 쉬운 자리에서 아껴 어려운 자리에 몰아 쓰는 대신, 거꾸로 <b>쉬운 자리에 다 붓는
        /// 것</b>이 최적이 된다(같은 화살로 얻는 기대 점수가 거리마다 두 배 넘게 차이난다).
        ///
        /// <para>되감기에 안전하다 — <see cref="ArcheryQuiver.RefilledWave"/>도 같이 저장·복원되므로
        /// 재생할 때 같은 틱에서 같은 판단이 나온다.</para>
        /// </summary>
        private void Refill(GameFramework.World.Entity entity, int wave)
        {
            if (wave < 0 || course.ArrowsPerStand <= 0)
            {
                return;   // 아직 출발 전이거나 무제한 맵(원형)
            }

            var quiver = entity.Get<ArcheryQuiver>();
            if (quiver == null || quiver.RefilledWave == wave)
            {
                return;
            }

            quiver.Remaining = course.ArrowsPerStand;
            quiver.RefilledWave = wave;
        }

        // 화면 밖으로 나간 화살을 계속 들고 있으면 목록이 한 판 내내 자란다.
        private void RemoveExpired(long tick)
        {
            float lifetimeTicks = ArcheryTrajectory.LifetimeSeconds / tickInterval;
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                if (tick - shots[i].FireTick > lifetimeTicks)
                {
                    shots.RemoveAt(i);
                }
            }
        }

        protected override void SaveGameState(long tick)
        {
            var aims = new Dictionary<string, ArcheryAim>();
            var quivers = new Dictionary<string, (int Remaining, int RefilledWave)>();
            foreach (var entity in EntityRegistry.All)
            {
                // 원격(비-Simulated) 몸은 베이스가 되감지 않는 대상이다 — 여기서 같이 저장했다가
                // 되돌리면, 되감는 사이 네트워크로 도착한 남의 새 조준값을 옛 값으로 덮어쓴다.
                if (entity.Has<GameFramework.World.Simulated>() == false)
                {
                    continue;
                }

                var aim = entity.Get<ArcheryAim>();
                if (aim != null)
                {
                    aims[entity.Id] = new ArcheryAim
                    {
                        Yaw = aim.Yaw, Pitch = aim.Pitch,
                        Drawing = aim.Drawing, DrawStartTick = aim.DrawStartTick,
                        DrawRatio = aim.DrawRatio,
                    };
                }

                var quiver = entity.Get<ArcheryQuiver>();
                if (quiver != null)
                {
                    quivers[entity.Id] = (quiver.Remaining, quiver.RefilledWave);
                }
            }
            saved.Record(tick, new SavedState(new List<ArcheryShot>(shots), aims, quivers));
        }

        // 베이스가 bool을 요구한다 — 그 틱 기록이 없으면 false다.
        protected override bool LoadGameState(long tick)
        {
            if (saved.TryGet(tick, out var state) == false)
            {
                return false;
            }

            shots.Clear();
            shots.AddRange(state.Shots);

            foreach (var pair in state.Aims)
            {
                var aim = EntityRegistry.Get(pair.Key)?.Get<ArcheryAim>();
                if (aim == null)
                {
                    continue;
                }
                aim.Yaw = pair.Value.Yaw;
                aim.Pitch = pair.Value.Pitch;
                aim.Drawing = pair.Value.Drawing;
                aim.DrawStartTick = pair.Value.DrawStartTick;
                aim.DrawRatio = pair.Value.DrawRatio;
            }

            foreach (var pair in state.Quivers)
            {
                var quiver = EntityRegistry.Get(pair.Key)?.Get<ArcheryQuiver>();
                if (quiver != null)
                {
                    quiver.Remaining = pair.Value.Remaining;
                    quiver.RefilledWave = pair.Value.RefilledWave;
                }
            }

            return true;
        }
    }
}
