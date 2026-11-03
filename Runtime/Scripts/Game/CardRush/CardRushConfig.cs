namespace LOP
{
    /// <summary>카드 러시의 숫자 모음. 마스터데이터 한 행(id 제외)을 그대로 옮긴다. 시간은 <see cref="Timing"/>에서 틱으로 한 번만 바꾼다.</summary>
    public sealed class CardRushConfig
    {
        public float MatchDurationSeconds { get; }
        public float MoveSeconds { get; }
        public float StunSeconds { get; }
        public int MinLanes { get; }
        public float BotReactMinSeconds { get; }
        public float BotReactMaxSeconds { get; }
        public float BotTimingJitterSeconds { get; }
        public float BotMissChance { get; }

        public CardRushTiming Timing { get; }

        public long MatchDurationTicks => CardRushTiming.Ticks(MatchDurationSeconds);

        public CardRushConfig(float matchDurationSeconds, float moveSeconds, float stunSeconds, int minLanes,
                              float botReactMinSeconds, float botReactMaxSeconds, float botTimingJitterSeconds, float botMissChance)
        {
            if (minLanes < 1 || minLanes > CardRushLanes.MaxLanes)
                throw new System.ArgumentOutOfRangeException(nameof(minLanes), $"레인 수는 1~{CardRushLanes.MaxLanes}이어야 한다");
            if (botReactMinSeconds > botReactMaxSeconds)
                throw new System.ArgumentException("봇 반응 최소가 최대보다 크다", nameof(botReactMinSeconds));

            MatchDurationSeconds = matchDurationSeconds;
            MoveSeconds = moveSeconds;
            StunSeconds = stunSeconds;
            MinLanes = minLanes;
            BotReactMinSeconds = botReactMinSeconds;
            BotReactMaxSeconds = botReactMaxSeconds;
            BotTimingJitterSeconds = botTimingJitterSeconds;
            BotMissChance = botMissChance;
            Timing = CardRushTiming.FromSeconds(moveSeconds, stunSeconds);
        }
    }
}
