namespace LOP
{
    /// <summary>카드 색. 회전은 늘 +1(빨 → 노 → 파 → 빨).</summary>
    public enum CardColor { Red = 0, Yellow = 1, Blue = 2 }

    public static class CardColors
    {
        public const int Count = 3;
        public static CardColor Next(CardColor c) => (CardColor)(((int)c + 1) % Count);
    }
}
