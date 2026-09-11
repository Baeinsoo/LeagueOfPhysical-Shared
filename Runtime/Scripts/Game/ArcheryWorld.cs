using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// Archery의 시뮬 코어. 매 틱 조준을 갱신하고, 떠난 화살을 목록에 담아 수명이 다하면 지운다.
    /// <b>화살은 엔티티가 아니다</b> — 발사 정보만 있으면 위치가 계산되므로 레지스트리에 넣지 않는다.
    /// </summary>
    public class ArcheryWorld : GameFramework.World.WorldBase
    {
        private readonly ArcheryAimSystem aimSystem;
        private readonly float tickInterval;
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

            public SavedState(List<ArcheryShot> shots, Dictionary<string, ArcheryAim> aims)
            {
                Shots = shots;
                Aims = aims;
            }
        }

        public IReadOnlyList<ArcheryShot> Shots => shots;

        public ArcheryWorld(GameFramework.World.EntityRegistry entityRegistry,
                            GameFramework.World.WorldEventBuffer eventBuffer,
                            ArcheryAimSystem aimSystem,
                            float tickInterval)
            : base(entityRegistry, eventBuffer)
        {
            this.aimSystem = aimSystem;
            this.tickInterval = tickInterval;
        }

        protected override void Mutation(long tick, float deltaTime)
        {
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>() == false)
                {
                    continue;
                }

                var shot = aimSystem.Tick(entity, tick, tickInterval);
                if (shot.HasValue)
                {
                    shots.Add(shot.Value);
                }
            }

            RemoveExpired(tick);
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
                    };
                }
            }
            saved.Record(tick, new SavedState(new List<ArcheryShot>(shots), aims));
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
            }

            return true;
        }
    }
}
