namespace LOP
{
    /// <summary>카드 러시의 시간은 전부 정수 틱이다. 초는 설정을 읽을 때 여기서 한 번만 바꾼다.</summary>
    public static class CardRushTiming
    {
        public const float TickInterval = 0.02f;

        //  float 나눗셈은 0.33/0.02가 16.4999…로 떨어질 수 있어 double로 나누고 반올림을 고정한다.
        public static int Ticks(float seconds)
            => System.Math.Max(1, (int)System.Math.Round((double)seconds / TickInterval, System.MidpointRounding.AwayFromZero));
    }
}
