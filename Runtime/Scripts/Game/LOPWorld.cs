namespace LOP
{
    public class LOPWorld : GameFramework.World.WorldBase
    {
        private readonly AbilitySystem _abilitySystem;
        private readonly StatusEffectSystem _statusEffectSystem;

        public LOPWorld(
            GameFramework.World.EntityRegistry entityRegistry,
            GameFramework.World.WorldEventBuffer eventBuffer,
            AbilitySystem abilitySystem,
            StatusEffectSystem statusEffectSystem)
            : base(entityRegistry, eventBuffer)
        {
            _abilitySystem = abilitySystem;
            _statusEffectSystem = statusEffectSystem;
        }

        // 4c: 어빌리티 페이즈 전진(Active 진입 시 효과 적용) + 상태이상 만료를 매 틱 구동.
        protected override void Mutation(long tick, float deltaTime)
        {
            foreach (var entity in EntityRegistry.All)
            {
                _abilitySystem.Tick(entity, tick);
                _statusEffectSystem.Tick(entity, tick);
            }
        }
    }
}
