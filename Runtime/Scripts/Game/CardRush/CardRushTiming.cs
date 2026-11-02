namespace LOP
{
    /// <summary>카드 러시의 시간은 전부 정수 틱이다. 초는 설정을 읽을 때 여기서 한 번만 바꾼다.</summary>
    public readonly struct CardRushTiming
    {
        public const float TickInterval = 0.02f;

        public readonly int MoveTicks;
        public readonly int StunTicks;

        public CardRushTiming(int moveTicks, int stunTicks)
        {
            if (moveTicks < 1 || stunTicks < 1) throw new System.ArgumentOutOfRangeException(nameof(moveTicks), "이동·멈춤은 1틱 이상이어야 한다");
            MoveTicks = moveTicks;
            StunTicks = stunTicks;
        }

        public static CardRushTiming FromSeconds(float moveSeconds, float stunSeconds)
            => new CardRushTiming(Ticks(moveSeconds), Ticks(stunSeconds));

        //  float 나눗셈은 0.33/0.02가 16.4999…로 떨어질 수 있어 double로 나누고 반올림을 고정한다.
        public static int Ticks(float seconds)
            => System.Math.Max(1, (int)System.Math.Round((double)seconds / TickInterval, System.MidpointRounding.AwayFromZero));
    }
}
