namespace LOP
{
    public class LOPWorld : GameFramework.World.WorldBase
    {
        private readonly StatusEffectSystem _statusEffectSystem;

        public LOPWorld(
            GameFramework.World.EntityRegistry entityRegistry,
            GameFramework.World.WorldEventBuffer eventBuffer,
            StatusEffectSystem statusEffectSystem)
            : base(entityRegistry, eventBuffer)
        {
            _statusEffectSystem = statusEffectSystem;
        }

        // 4c 3a: 상태이상 만료를 매 틱 구동 — world.Tick의 첫 실내용물. 발동/적용/캐스케이드는 후속 슬라이스.
        protected override void Mutation(long tick, float deltaTime)
        {
            foreach (var entity in EntityRegistry.All)
            {
                _statusEffectSystem.Tick(entity, tick);
            }
        }
    }
}
