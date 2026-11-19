namespace LOP
{
    /// <summary>삼색 그랩 수치. 시안 v2 값을 50Hz 틱으로 옮겼다(ms → 틱은 올림).</summary>
    public static class TriGrabConfig
    {
        public const int Slots = 8;
        public const float RingRadius = 4f;
        public const float RotationBase = 0.45f;
        public const float RotationStep = 0.05f;
        public const float HitRadius = 1.15f;
        public const float HookLength = 9f;
        public const float BodyRadius = 0.7f;
        public const float RetractEmpty = 1.3f;
        public const float RetractCard = 0.9f;
        public const float ValidProbability = 0.28f;
        public const float CycleProbability = 0.35f;

        public const int ReachTicks = 18;
        public const int ReloadMissTicks = 13;
        public const int ReloadWrongTicks = 65;
        public const int RefillTicks = 35;
        public const int ForceValidAfterTicks = 120;
        public const int PrepTicks = 80;
        public const int ResolvedTicks = 70;
        public const int TotalRounds = 5;
        /// <summary>Live가 이만큼 지나면 남은 사람은 0점으로 라운드가 끝난다(20초).</summary>
        public const int RoundLimitTicks = 1000;
        public const int MatchCapTicks = 12000;
        public const int MinPlayersWithBots = 4;
        public const int EndGraceTicks = 150;

        public const float TickSeconds = 0.02f;

        // 봇이 걷는 고리. 맵의 벽과 같은 값이다.
        public const float BotRingMin = 6.3f;
        public const float BotRingMax = 9.4f;
        public const float MoveSpeed = 4.5f;

        public static float HookSpeedPerTick => HookLength / ReachTicks;

        /// <summary>그 라운드에서 k번째로 정답을 낸 사람의 점수: 10, 9, 8, … 최소 1.</summary>
        public static int PlacePoints(int place) => System.Math.Max(1, 11 - place);

        /// <summary>아직 정답을 못 낸 사람이 이 수 이하로 남으면 라운드가 끝난다. 2~7명은 1명, 8명은 2명.</summary>
        public static int RoundEndRemaining(int participants) => System.Math.Max(1, participants / 4);

        /// <summary>도는 카드가 색을 바꾸는 주기. 라운드가 갈수록 짧아진다.</summary>
        public static int CyclePeriodTicks(int round)
        {
            float ms = System.Math.Max(420f, 680f - 30f * (round - 1));
            return (int)System.Math.Ceiling(ms / 20f);
        }
    }
}
