namespace LOP
{
    /// <summary>
    /// 착지가 치명적인지. <b>공유 구체 코드</b>라 클·서가 같은 답을 낸다 — 죽음을 되돌리는 것은
    /// 서버만 하지만, "치명인가"는 완주를 줄지 정할 때 클라도 물어야 한다.
    /// </summary>
    public static class SkydiveLanding
    {
        public static bool IsLethal(float downwardSpeed, in SkydiveConfig config)
        {
            return downwardSpeed > config.LandingLethalSpeed;
        }

        public static bool IsLethal(GameFramework.World.Entity diver, in SkydiveConfig config)
        {
            var impact = diver?.Get<LandingImpact>();
            //  착지한 틱이 아니면 0이라 자연히 거짓이다 — "착지했나"를 따로 묻지 않는다.
            return impact != null && IsLethal(impact.DownwardSpeed, config);
        }
    }
}
