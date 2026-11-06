namespace LOP
{
    /// <summary>
    /// 카드 러시 3차 규칙의 손맛 값(spec §2) — 웹 시안이 확정한 값을 정수 틱·mm로 옮겼다(시안 1 u = 2.6 m / 75).
    /// 바꾸면 규칙이 바뀐다. 튜닝(길이·빈도·벌)은 마스터데이터(<see cref="CardRushTuning"/>)에 있다.
    /// </summary>
    public static class CardRushRules
    {
        public const int RunMmPerTick = 36;          // 1.80 m/s
        public const int CardDwellTicks = 23;        // 한 장 0.46초
        public const int CooldownTicks = 20;         // 쓰고 쉬는 0.4초
        public const int DashTicks = 14;
        public const int DashMm = 3120;
        public const int BlockTicks = 50;
        public const int SmashReachMm = 1660;
        public const int SmashBackMm = 200;
        public const int BarricadeStopMm = 490;
        public const int RollerMmPerTick = 38;       // 1.90 m/s
        public const int RollerRevealMm = 7000;      // 내 앞 이 거리에서 굴러오기 시작한다
        public const int RollerContactMm = 420;
        public const int ZoneMarginMm = 280;
        public const int ZoneAheadMinMm = 350, ZoneAheadMaxMm = 2770;
        public const int ZoneLengthMinMm = 2430, ZoneLengthMaxMm = 4850;
        public const int ZoneWarnStartTicks = 75, ZoneWarnEndTicks = 45;
        public const int FirstObstacleMm = 8000, FirstZoneMm = 14000, FinishClearMm = 5000;
        //  한 레인에 살아 있는 장애물이 늘 하나이려면 — 바리케이드 앞에 서 있는 동안 다음 것이 풀리지 않아야 한다.
        public const int MinObstacleGapMm = RollerRevealMm + BarricadeStopMm + 500;
        public const int MinTrackMm = 40000, MaxTrackMm = 190000;   // 맵 복도가 205 m까지다
        public const int MinZoneGapMm = 5000;

        /// <summary>대시 k틱째까지 간 거리 — 시안의 1 − (1 − p)² 감속. 정수 나눗셈이라 기기마다 같다.</summary>
        public static int DashOffset(int k)
        {
            if (k <= 0) return 0;
            if (k >= DashTicks) return DashMm;
            long d2 = (long)DashTicks * DashTicks;
            long rest = DashTicks - k;
            return (int)(DashMm * (d2 - rest * rest) / d2);
        }
    }
}
