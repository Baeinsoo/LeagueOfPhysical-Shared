namespace LOP
{
    /// <summary>
    /// Flappy Race의 시뮬 코어. 클·서가 같은 구체 클래스를 돌려 결과가 갈리지 않게 한다.
    /// 지금은 비어 있다 — 전진·플랩·중력은 다음 슬라이스에서 들어온다.
    /// </summary>
    public class FlappyWorld : GameFramework.World.WorldBase
    {
        public FlappyWorld(
            GameFramework.World.EntityRegistry entityRegistry,
            GameFramework.World.WorldEventBuffer eventBuffer)
            : base(entityRegistry, eventBuffer)
        {
        }
    }
}
