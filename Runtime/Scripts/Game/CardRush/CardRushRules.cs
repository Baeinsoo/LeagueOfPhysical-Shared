namespace LOP
{
    /// <summary>
    /// 카드 러시 규칙의 손맛 값(4차 spec §2) — 3차 웹 시안 값을 정수 틱·mm로 옮기고 4차(잡고 쓰기·투사체·점프·구덩이) 값을 더했다.
    /// 바꾸면 규칙이 바뀐다. 튜닝(길이·빈도·벌)은 마스터데이터(<see cref="CardRushTuning"/>)에 있다.
    /// </summary>
    public static class CardRushRules
    {
        public const int RunMmPerTick = 60;          // 3.00 m/s — 4.4차에 2.4 m/s에서 1.25배(속도감)
        public const int CardDwellTicks = 23;        // 한 장 0.46초
        public const int CooldownTicks = 20;         // 쓰고 쉬는 0.4초
        public const int DashTicks = 14;
        public const int DashMm = 5200;              // 달리기와 같은 배율. 구덩이(최대 1.4 m)를 대시로 넘는다
        public const int RollMaxTicks = 300;         // 돌기 시작해 6초 안에 안 고르면 그때 색을 자동으로 잡는다
        public const int LockHoldTicks = 250;        // 잡은 카드가 남는 5초 — 지나면 사라진다
        public const int ShotMmPerTick = 300;        // 투사체 15 m/s
        public const int ShotRangeMm = 8000;         // 굴러오는 것이 풀리는 7 m보다 길어 풀리자마자 맞힐 수 있다
        public const int ShotStartAheadMm = 300;     // 손에서 떠나는 자리(몸 앞)
        public const int PitWidthMinMm = 1000, PitWidthMaxMm = 1400;
        public const int PitBackMm = 2500;           // 빠지면 구덩이 앞 가장자리에서 이만큼 뒤에서 다시 — 다시 대시를 고를 시간(41틱)이 남게
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
        //  4.4차: 구덩이 앞 가장자리에서 대시해 내려앉는 자리(최대 DashMm 앞)가 다음 장애물보다 앞이어야 한다 — 대시 한 번에 두 장애물을 엮지 않게.
        //  투사체는 지금 장애물만 본다 — 사거리 안에 다음 장애물이 있어도 판정은 그대로다.
        public const int MinObstacleGapMm = DashMm + 800;
        //  황금 고블린(4.4차): 내 앞 RevealMm에서 나타나 느리게 달아나고 LifeTicks 안에 금카를 쓰면 잡힌다. 빨리 잡을수록 골드 러시가 길다.
        public const int GoblinRevealMm = 5000;
        public const int GoblinMmPerTick = 40;       // 2 m/s — 달리기보다 느려 계속 가까워진다
        public const int GoblinLifeTicks = 150;      // 3초 안에 못 잡으면 사라진다
        public const int GoblinGapMm = 9000;         // 고블린 뒤 다음 장애물까지 — 사는 동안 달리고 대시해도 닿지 않게
        public const int GoblinPermille = 150;       // 장애물 자리마다 고블린이 나올 몫
        public const int RushMmPerTick = 96;         // 골드 러시 4.8 m/s — 달리기의 1.6배
        public const int RushMaxTicks = 150, RushMinTicks = 50;   // 나타난 틱에 잡으면 3초, 사라지기 직전이면 1초
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
