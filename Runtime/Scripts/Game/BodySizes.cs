namespace LOP
{
    /// <summary>
    /// 몸 캡슐 치수. 클·서 크리에이터가 같은 값을 붙여야 예측이 서버 권위와 갈리지 않으므로 한 곳에 둔다.
    /// (Flappy의 새는 이 상수가 아니라 마스터데이터 <c>TbFlappyConfig</c> 값을 쓴다.)
    /// </summary>
    public static class BodySizes
    {
        public const float CharacterRadius = 0.35f;
        public const float CharacterHeight = 1.5f;

        /// <summary>바닥에 놓인 아이템 — 이 캡슐이 곧 줍기 판정 범위다(트리거).</summary>
        public const float ItemRadius = 0.35f;
        public const float ItemHeight = 1.5f;
    }
}
