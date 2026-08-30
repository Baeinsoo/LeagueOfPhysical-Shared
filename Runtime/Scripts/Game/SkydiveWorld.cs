using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// Skydive의 시뮬 코어. 클·서가 같은 구체 클래스를 돌려 결과가 갈리지 않게 한다.
    /// 슬라이스 1의 한 틱: Simulated 캐릭터를 모아 중력으로 떨어뜨린다.
    /// 레이저 판정은 Detection에 들어오지만(슬라이스 4) 지금은 비어 있다.
    /// </summary>
    public class SkydiveWorld : GameFramework.World.WorldBase
    {
        private readonly SkydiveMoveSystem _moveSystem;

        // TODO(Task 2): SkydiveMoveSystem.Tick이 자세 기반 3인자로 바뀌면서 컴파일만 맞춘 자리다.
        // 진짜 config는 Task 2가 생성자로 주입한다(MasterData TbSkydiveConfig 연동).
        private static readonly SkydiveConfig _placeholderConfig = new SkydiveConfig(
            spreadFallSpeed: 25f, diveFallSpeed: 45f, glideFallSpeed: 6f,
            spreadMoveSpeed: 12f, diveMoveSpeed: 18f, glideMoveSpeed: 14f,
            spreadTurnAccel: 22f, diveTurnAccel: 6f, glideTurnAccel: 18f,
            fallApproach: 30f, postureRate: 4f,
            bodyRadius: 0.4f, bodyHeight: 1.8f, groundY: 0f,
            staminaMax: 100f, glideDrain: 20f, groundRecover: 40f, emergencyGlideTime: 1f);

        // 매 틱 도는 코드라 목록을 새로 만들지 않고 비워서 다시 쓴다.
        private readonly List<GameFramework.World.Entity> _divers = new List<GameFramework.World.Entity>();

        public SkydiveWorld(
            GameFramework.World.EntityRegistry entityRegistry,
            GameFramework.World.WorldEventBuffer eventBuffer,
            SkydiveMoveSystem moveSystem)
            : base(entityRegistry, eventBuffer)
        {
            _moveSystem = moveSystem;
        }

        protected override void Mutation(long tick, float deltaTime)
        {
            CollectDivers();

            if (HasStarted(tick) == false)
            {
                // 출발 전. 속도를 명시적으로 0으로 둔다 — 스냅샷과 물리 팔로워가 이 값을 읽는다.
                for (int i = 0; i < _divers.Count; i++)
                {
                    var velocity = _divers[i].Get<GameFramework.World.Velocity>();
                    if (velocity == null)
                    {
                        continue;
                    }
                    velocity.Linear = System.Numerics.Vector3.Zero;
                }
                return;
            }

            for (int i = 0; i < _divers.Count; i++)
            {
                _moveSystem.Tick(_divers[i], deltaTime, _placeholderConfig);
            }
        }

        // id 순으로 세운다. 레지스트리 순회 순서는 정해져 있지 않은데, 처리 순서가 클·서에서
        // 같아야 두 쪽이 같은 결과에 이른다.
        private void CollectDivers()
        {
            _divers.Clear();
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Get<EntityKind>()?.Kind != EntityType.Character)
                {
                    continue;
                }
                if (entity.Has<GameFramework.World.Simulated>() == false)
                {
                    continue;   // 클라에서 남은 보간으로 그린다
                }
                _divers.Add(entity);
            }
            _divers.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
        }
    }
}
