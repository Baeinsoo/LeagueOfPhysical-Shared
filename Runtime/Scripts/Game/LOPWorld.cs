namespace LOP
{
    public class LOPWorld : GameFramework.World.WorldBase
    {
        private readonly MovementSystem _movementSystem;
        private readonly AbilitySystem _abilitySystem;
        private readonly StatusEffectSystem _statusEffectSystem;

        public LOPWorld(
            GameFramework.World.EntityRegistry entityRegistry,
            GameFramework.World.WorldEventBuffer eventBuffer,
            MovementSystem movementSystem,
            AbilitySystem abilitySystem,
            StatusEffectSystem statusEffectSystem)
            : base(entityRegistry, eventBuffer)
        {
            _movementSystem = movementSystem;
            _abilitySystem = abilitySystem;
            _statusEffectSystem = statusEffectSystem;
        }

        protected override void Mutation(long tick, float deltaTime)
        {
            // 이동은 어빌리티 페이즈 전진보다 먼저 — 대시 발동 틱의 입력 게이트 타이밍이 이 순서에 걸려 있다.
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>())
                {
                    _movementSystem.Tick(entity, tick, deltaTime);
                }
            }

            // 어빌리티 페이즈 전진(Active 진입 시 효과 적용) + 상태이상 만료.
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>())
                {
                    _abilitySystem.Tick(entity, tick);
                    _statusEffectSystem.Tick(entity, tick);
                }
            }
        }
    }
}
