namespace LOP
{
    /// <summary>카드 색. 늘 빨 → 노 → 파 순으로 돈다 = 던지기 · 점프 · 대시. 노랑은 금빛으로 그린다.</summary>
    public enum CardColor { Red = 0, Yellow = 1, Blue = 2 }

    public static class CardColors
    {
        public const int Count = 3;
        public static CardColor Next(CardColor c) => (CardColor)(((int)c + 1) % Count);
    }
}
