namespace LOP
{
    /// <summary>카드 색. 회전은 늘 +1 — 파 → 빨 → 노 → 파(트페 W 순서, 빨 → 노 → 파와 같은 순환이다). 노랑은 금빛으로 그린다.</summary>
    public enum CardColor { Red = 0, Yellow = 1, Blue = 2 }

    public static class CardColors
    {
        public const int Count = 3;
        public static CardColor Next(CardColor c) => (CardColor)(((int)c + 1) % Count);
    }
}
