namespace LOP
{
    /// <summary>
    /// 대시 중이면 통과되는 벽. 판정은 콜라이더 그대로 하고, 이 레이어만 대시 중인 새의 sweep에서
    /// 빠진다(<see cref="FlappyWorld"/>). 새마다 자기 대시 상태로 고르니 남이 뚫었다고 열리지 않는다.
    /// </summary>
    public static class FlappyHologram
    {
        /// <summary>클·서 두 프로젝트의 TagManager 10번 레이어. 번호가 다르면 서버에서 다른 층이 된다.</summary>
        public const string LayerName = "Hologram";
    }
}
