namespace LOP
{
    /// <summary>
    /// 마스터데이터의 튜닝 값(길이·빈도·벌)을 판정이 쓰는 정수 mm·틱으로 한 번 바꿔 둔 것(spec §3.1).
    /// 판정은 이것만 보므로 클·서가 같은 표를 읽는 한 같은 코스·같은 벌이 나온다.
    /// </summary>
    public readonly struct CardRushTuning
    {
        public readonly int TrackMm;
        public readonly int ObstacleGapMinMm;
        public readonly int ObstacleGapMaxMm;
        public readonly int BarricadePermille;
        public readonly int ZoneGapMinMm;
        public readonly int ZoneGapMaxMm;
        public readonly int BarricadeWaitTicks;
        public readonly int RollKnockMm;
        public readonly int RollStunTicks;
        public readonly int ZoneKnockMm;
        public readonly int ZoneStunTicks;

        /// <summary>검증은 <see cref="CardRushConfig"/>가 한다 — 시험은 손으로 고른 값을 바로 넣는다.</summary>
        public CardRushTuning(int trackMm, int obstacleGapMinMm, int obstacleGapMaxMm, int barricadePermille,
                              int zoneGapMinMm, int zoneGapMaxMm, int barricadeWaitTicks,
                              int rollKnockMm, int rollStunTicks, int zoneKnockMm, int zoneStunTicks)
        {
            TrackMm = trackMm;
            ObstacleGapMinMm = obstacleGapMinMm;
            ObstacleGapMaxMm = obstacleGapMaxMm;
            BarricadePermille = barricadePermille;
            ZoneGapMinMm = zoneGapMinMm;
            ZoneGapMaxMm = zoneGapMaxMm;
            BarricadeWaitTicks = barricadeWaitTicks;
            RollKnockMm = rollKnockMm;
            RollStunTicks = rollStunTicks;
            ZoneKnockMm = zoneKnockMm;
            ZoneStunTicks = zoneStunTicks;
        }

        //  float 곱은 7.98f × 1000 같은 값이 7979.99…로 떨어질 수 있어 double로 곱하고 반올림을 고정한다.
        public static int Mm(float meters)
            => (int)System.Math.Round((double)meters * 1000.0, System.MidpointRounding.AwayFromZero);

        public static int Permille(float share)
            => (int)System.Math.Round((double)share * 1000.0, System.MidpointRounding.AwayFromZero);
    }
}
