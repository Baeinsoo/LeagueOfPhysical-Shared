namespace LOP
{
    /// <summary>
    /// <see cref="FlappyConfig"/>에서 새 몸의 <see cref="GameFramework.World.CapsuleShape"/>를
    /// 뽑아내는 한 벌. 클·서 <c>FlappyBirdCreator</c>가 각자 이 식을 베껴 두면 한쪽만 고치고
    /// 다른 쪽을 잊는 일이 생긴다 — 그래서 여기 한 곳에 모은다.
    /// </summary>
    public static class FlappyBodyShape
    {
        /// <summary>
        /// <see cref="FlappyConfig.BodyLength"/>가 지름(반지름×2)보다 크면 그만큼 누운 캡슐,
        /// 아니면(0 포함) 지금까지대로 선 캡슐이다.
        /// </summary>
        public static GameFramework.World.CapsuleShape For(FlappyConfig config) =>
            config.BodyLength > config.BodyRadius * 2f
                ? GameFramework.World.CapsuleShape.Lying(config.BodyRadius, config.BodyLength)
                : new GameFramework.World.CapsuleShape(config.BodyRadius, config.BodyHeight);
    }
}
