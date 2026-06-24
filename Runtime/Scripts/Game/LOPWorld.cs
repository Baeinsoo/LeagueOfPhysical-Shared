namespace LOP
{
    public class LOPWorld : GameFramework.World.WorldBase
    {
        public LOPWorld(
            GameFramework.World.EntityRegistry entityRegistry,
            GameFramework.World.WorldEventBuffer eventBuffer)
            : base(entityRegistry, eventBuffer) { }

        // 4c가 Collection/Mutation/Detection override로 매니저 로직을 흡수.
    }
}
