namespace LOP
{
    /// <summary>
    /// 카드 러시 규칙의 손맛 값(4차 spec §2) — 3차 웹 시안 값을 정수 틱·mm로 옮기고 4차(잡고 쓰기·투사체·점프·구덩이) 값을 더했다.
    /// 바꾸면 규칙이 바뀐다. 튜닝(길이·빈도·벌)은 마스터데이터(<see cref="CardRushTuning"/>)에 있다.
    /// </summary>
    public static class CardRushRules
    {
        public const int RunMmPerTick = 48;          // 2.40 m/s — 4.2차에 1.8 m/s에서 4/3배
        public const int CardDwellTicks = 23;        // 한 장 0.46초
        public const int CooldownTicks = 20;         // 쓰고 쉬는 0.4초
        public const int DashTicks = 14;
        public const int DashMm = 4160;              // 달리기와 같은 4/3배
        public const int RollMaxTicks = 300;         // 돌기 시작해 6초 안에 안 고르면 그때 색을 자동으로 잡는다
        public const int LockHoldTicks = 250;        // 잡은 카드가 남는 5초 — 지나면 사라진다
        public const int ShotMmPerTick = 300;        // 투사체 15 m/s
        public const int ShotRangeMm = 8000;         // 굴러오는 것이 풀리는 7 m보다 길어 풀리자마자 맞힐 수 있다
        public const int ShotStartAheadMm = 300;     // 손에서 떠나는 자리(몸 앞)
        public const int JumpTicks = 40;             // 0.8초
        public const int JumpMmPerTick = 80;         // 공중에서 4 m/s — 40틱에 3.2 m(달리기와 같은 4/3배)
        public const int JumpHeightMm = 1000;        // 그림·자리 높이. 판정엔 안 쓴다
        public const int PitWidthMinMm = 1000, PitWidthMaxMm = 1400;
        public const int PitBackMm = 1500;           // 빠지면 구덩이 앞 가장자리에서 이만큼 뒤에서 다시
        public const int BarricadeStopMm = 490;
        public const int RollerMmPerTick = 38;       // 1.90 m/s
        public const int RollerRevealMm = 7000;      // 내 앞 이 거리에서 굴러오기 시작한다
        public const int RollerContactMm = 420;
        public const int ZoneMarginMm = 280;
        //  구역은 대시 한 번으로 빠져나갈 수 있는 길이로, 경고는 파랑이 돌아올 시간을 준다(spec §2).
        public const int ZoneAheadMinMm = 300, ZoneAheadMaxMm = 900;
        public const int ZoneLengthMinMm = 1800, ZoneLengthMaxMm = 2800;
        public const int ZoneWarnStartTicks = 90, ZoneWarnEndTicks = 60;
        public const int ZoneEscapeSlackMm = 3000;   // 구역 끝 뒤 이만큼은 다음 장애물이 끼지 않는다
        public const int FirstObstacleMm = 8000, FirstZoneMm = 14000, FinishClearMm = 5000;
        //  한 레인에 살아 있는 장애물이 늘 하나이려면 — 바리케이드 앞에 서 있는 동안 다음 것이 풀리지 않아야 하고, 구덩이 폭만큼 더 띄운다.
        //  투사체는 지금 장애물만 본다 — 구덩이 먼 가장자리에서 던지면 다음 장애물이 사거리 안일 수 있어도 판정은 그대로다(spec "구현 중 바뀐 것").
        public const int MinObstacleGapMm = RollerRevealMm + BarricadeStopMm + 500 + PitWidthMaxMm;
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

        /// <summary>점프 k틱째의 높이(mm) — 포물선 4k(J − k)/J². 땅(0)과 끝(J)에서 0. 정수라 기기마다 같다.</summary>
        public static int JumpHeightAt(int k)
        {
            if (k <= 0 || k >= JumpTicks) return 0;
            return (int)((long)JumpHeightMm * 4 * k * (JumpTicks - k) / ((long)JumpTicks * JumpTicks));
        }
    }
}
