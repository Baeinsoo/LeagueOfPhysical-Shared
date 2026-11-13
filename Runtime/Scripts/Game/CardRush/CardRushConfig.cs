namespace LOP
{
    /// <summary>
    /// 카드 러시의 숫자 모음. 마스터데이터 한 행(id 제외)을 그대로 옮긴다(spec §6.1 열 순서 = 인수 순서).
    /// 판정이 쓰는 값은 <see cref="Tuning"/>에서 mm·틱으로 한 번만 바꾼다.
    /// </summary>
    public sealed class CardRushConfig
    {
        public float MatchDurationSeconds { get; }
        public int MinLanes { get; }
        public float TrackLengthM { get; }
        public float ObstacleGapMinM { get; }
        public float ObstacleGapMaxM { get; }
        public float BarricadeShare { get; }
        public float ZoneGapMinM { get; }
        public float ZoneGapMaxM { get; }
        public float BarricadeWaitSeconds { get; }
        public float RollKnockM { get; }
        public float RollStunSeconds { get; }
        public float ZoneKnockM { get; }
        public float ZoneStunSeconds { get; }
        public float BotReactMinSeconds { get; }
        public float BotReactMaxSeconds { get; }
        public float BotReactExtraSeconds { get; }
        public float BotHesitateChance { get; }
        public float BotFreeDashChance { get; }
        public float PitShare { get; }
        public float PitStunSeconds { get; }

        public CardRushTuning Tuning { get; }

        public long MatchDurationTicks => CardRushTiming.Ticks(MatchDurationSeconds);

        public CardRushConfig(float matchDurationSeconds, int minLanes, float trackLengthM,
                              float obstacleGapMinM, float obstacleGapMaxM, float barricadeShare,
                              float zoneGapMinM, float zoneGapMaxM, float barricadeWaitSeconds,
                              float rollKnockM, float rollStunSeconds, float zoneKnockM, float zoneStunSeconds,
                              float botReactMinSeconds, float botReactMaxSeconds, float botReactExtraSeconds,
                              float botHesitateChance, float botFreeDashChance,
                              float pitShare = 0.35f, float pitStunSeconds = 0.8f)
        {
            if (minLanes < 1 || minLanes > CardRushLanes.MaxLanes)
                throw new System.ArgumentOutOfRangeException(nameof(minLanes), $"레인 수는 1~{CardRushLanes.MaxLanes}이어야 한다");
            if (botReactMinSeconds > botReactMaxSeconds)
                throw new System.ArgumentException("봇 반응 최소가 최대보다 크다", nameof(botReactMinSeconds));

            int trackMm = CardRushTuning.Mm(trackLengthM);
            if (trackMm < CardRushRules.MinTrackMm || trackMm > CardRushRules.MaxTrackMm)
                throw new System.ArgumentOutOfRangeException(nameof(trackLengthM),
                    $"코스 길이는 {CardRushRules.MinTrackMm / 1000}~{CardRushRules.MaxTrackMm / 1000} m여야 한다(맵 복도 길이) — 지금 {trackLengthM} m");

            int obstacleMin = CardRushTuning.Mm(obstacleGapMinM), obstacleMax = CardRushTuning.Mm(obstacleGapMaxM);
            if (obstacleMin < CardRushRules.MinObstacleGapMm)
                throw new System.ArgumentOutOfRangeException(nameof(obstacleGapMinM),
                    $"장애물 간격 최소는 {CardRushRules.MinObstacleGapMm} mm 이상이어야 한다(한 레인에 장애물 하나) — 지금 {obstacleMin} mm");
            if (obstacleMax < obstacleMin)
                throw new System.ArgumentOutOfRangeException(nameof(obstacleGapMaxM), "장애물 간격 최대가 최소보다 작다");

            int zoneMin = CardRushTuning.Mm(zoneGapMinM), zoneMax = CardRushTuning.Mm(zoneGapMaxM);
            if (zoneMin < CardRushRules.MinZoneGapMm)
                throw new System.ArgumentOutOfRangeException(nameof(zoneGapMinM),
                    $"구역 간격 최소는 {CardRushRules.MinZoneGapMm} mm 이상이어야 한다 — 지금 {zoneMin} mm");
            if (zoneMax < zoneMin)
                throw new System.ArgumentOutOfRangeException(nameof(zoneGapMaxM), "구역 간격 최대가 최소보다 작다");

            if (!(barricadeShare >= 0f && barricadeShare <= 1f))
                throw new System.ArgumentOutOfRangeException(nameof(barricadeShare), $"바리케이드 비율은 0~1이어야 한다 — 지금 {barricadeShare}");
            if (!(pitShare >= 0f && pitShare <= 1f))
                throw new System.ArgumentOutOfRangeException(nameof(pitShare), $"구덩이 비율은 0~1이어야 한다 — 지금 {pitShare}");
            //  천분율로 더한다 — float 0.35 + 0.65가 1을 살짝 넘어 멀쩡한 표를 거절하지 않게.
            if (CardRushTuning.Permille(barricadeShare) + CardRushTuning.Permille(pitShare) > 1000)
                throw new System.ArgumentOutOfRangeException(nameof(pitShare),
                    $"바리케이드 + 구덩이 비율은 1 이하여야 한다(나머지가 굴러오는 것) — 지금 {barricadeShare} + {pitShare}");

            if (!(barricadeWaitSeconds > 0f)) throw new System.ArgumentOutOfRangeException(nameof(barricadeWaitSeconds), "바리케이드 기다림은 0보다 커야 한다");
            if (!(rollKnockM > 0f)) throw new System.ArgumentOutOfRangeException(nameof(rollKnockM), "꽈당 밀림은 0보다 커야 한다");
            if (!(rollStunSeconds > 0f)) throw new System.ArgumentOutOfRangeException(nameof(rollStunSeconds), "꽈당 기절은 0보다 커야 한다");
            if (!(zoneKnockM > 0f)) throw new System.ArgumentOutOfRangeException(nameof(zoneKnockM), "으악 밀림은 0보다 커야 한다");
            if (!(zoneStunSeconds > 0f)) throw new System.ArgumentOutOfRangeException(nameof(zoneStunSeconds), "으악 기절은 0보다 커야 한다");
            if (!(pitStunSeconds > 0f)) throw new System.ArgumentOutOfRangeException(nameof(pitStunSeconds), "풍덩 기절은 0보다 커야 한다");

            MatchDurationSeconds = matchDurationSeconds;
            MinLanes = minLanes;
            TrackLengthM = trackLengthM;
            ObstacleGapMinM = obstacleGapMinM;
            ObstacleGapMaxM = obstacleGapMaxM;
            BarricadeShare = barricadeShare;
            ZoneGapMinM = zoneGapMinM;
            ZoneGapMaxM = zoneGapMaxM;
            BarricadeWaitSeconds = barricadeWaitSeconds;
            RollKnockM = rollKnockM;
            RollStunSeconds = rollStunSeconds;
            ZoneKnockM = zoneKnockM;
            ZoneStunSeconds = zoneStunSeconds;
            BotReactMinSeconds = botReactMinSeconds;
            BotReactMaxSeconds = botReactMaxSeconds;
            BotReactExtraSeconds = botReactExtraSeconds;
            BotHesitateChance = botHesitateChance;
            BotFreeDashChance = botFreeDashChance;
            PitShare = pitShare;
            PitStunSeconds = pitStunSeconds;

            Tuning = new CardRushTuning(
                trackMm: trackMm,
                obstacleGapMinMm: obstacleMin, obstacleGapMaxMm: obstacleMax,
                barricadePermille: CardRushTuning.Permille(barricadeShare),
                zoneGapMinMm: zoneMin, zoneGapMaxMm: zoneMax,
                barricadeWaitTicks: CardRushTiming.Ticks(barricadeWaitSeconds),
                rollKnockMm: CardRushTuning.Mm(rollKnockM), rollStunTicks: CardRushTiming.Ticks(rollStunSeconds),
                zoneKnockMm: CardRushTuning.Mm(zoneKnockM), zoneStunTicks: CardRushTiming.Ticks(zoneStunSeconds),
                pitPermille: CardRushTuning.Permille(pitShare), pitStunTicks: CardRushTiming.Ticks(pitStunSeconds));
        }
    }
}
