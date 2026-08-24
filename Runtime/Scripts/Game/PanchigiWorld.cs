namespace LOP
{
    /// <summary>
    /// 판치기 시뮬. **비어 있는 것이 맞다** — 동전을 굴리는 것은 우리 시뮬이 아니라 유니티 물리이고,
    /// 그 결과는 PhysicsSimulationSystem이 World로 되읽는다. 플레이어는 아바타가 없어 움직이지 않는다.
    /// 월드 자리가 필요한 이유는 Runner가 매 틱 IWorld.Tick을 부르기 때문이다.
    /// </summary>
    public class PanchigiWorld : GameFramework.World.WorldBase
    {
        public PanchigiWorld(
            GameFramework.World.EntityRegistry entityRegistry,
            GameFramework.World.WorldEventBuffer eventBuffer)
            : base(entityRegistry, eventBuffer)
        {
        }

        protected override void Collection(long tick, float deltaTime) { }

        protected override void Mutation(long tick, float deltaTime) { }

        protected override void Detection(long tick, float deltaTime) { }
    }
}
