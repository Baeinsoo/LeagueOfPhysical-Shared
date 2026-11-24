using System;

namespace LOP.TriGrab
{
    /// <summary>
    /// 원형 판 삼색 그랩 수치. 시안 단위 × 0.1 = 미터, 틱 0.02초.
    /// 맵(TriGrabMap.unity) 실측: 받침 반지름 4.6(원기둥 지름 9.2), 안쪽 울타리 중심 5.7(두께 0.4 → 면 5.5~5.9),
    /// 바깥 벽 중심 10.15(두께 0.5 → 안쪽 면 9.9), 바닥 반지름 10.5, 출발점 반지름 7.8.
    /// 스펙 값과 0.5 m 넘게 다르면 맵을 따른다(맵은 고치지 않는다).
    /// </summary>
    public static class TriGrabConfig
    {
        public const float TickSeconds = 0.02f;

        // 맵 실측: 안쪽 울타리 안쪽 면 5.5(스펙 6.0, 차이 0.5라 맵을 따름). 카드 벽 = 원판 - 1.0.
        public const float DiscRadius = 5.5f;
        public const float CardWallRadius = 4.5f;
        // 몸 중심이 걸을 수 있는 길: 울타리 바깥 면 5.9 + 몸 0.7 ~ 바깥 벽 안쪽 면 9.9 - 몸 0.7 (스펙 6.8~8.3).
        public const float LaneMin = 6.6f;
        public const float LaneMax = 9.2f;
        public const float ArenaOuterRadius = 9.9f;

        public const float BodyRadius = 0.7f;
        public const float CoinRadius = 0.54f;
        public const float CardBumpRadius = 0.64f;
        public const float HookSlack = 0.16f;
        public const float MoleRadius = 0.42f;
        // 둘레길 바깥(9.2)에서 반대편 카드 벽(4.5)까지 = 13.7 (스펙 12.5, 맵이 더 넓어 맵을 따름).
        public const float HookRange = 13.7f;
        public const float MoveSpeed = 7f;
        public const float HookOutSpeed = 36f;
        public const float HookBackSpeed = 26f;
        public const float CardSpeed = 1.4f;

        public const int AskTicks = 55;
        public const int LiveLimitTicks = 400;
        public const int ResolvedTicks = 70;
        public const int TotalRounds = 10;

        public const int CardsOnDisc = 5;
        /// <summary>새 카드는 판 한가운데에서 태어나 바깥으로 퍼진다 — 어디에 맞는 카드가 나오는지로 승부가 갈리지 않게.</summary>
        public const float CardSpawnRadius = 0.6f;
        /// <summary>태어날 때 바깥으로 튀어 나가는 속도. 평소 속도(CardSpeed)로 서서히 가라앉는다.</summary>
        public const float CardBurstSpeed = 3.0f;
        public const int RefillTicks = 18;
        public const int PopTicks = 15;

        public const int StunTicks = 50;
        public const int HookCooldownTicks = 8;

        public const int MinPlayersWithBots = 4;
        /// <summary>10라운드가 모두 시간 상한까지 가는 길이 + 여유 500틱.</summary>
        public const int MatchCapTicks = TotalRounds * (AskTicks + LiveLimitTicks + ResolvedTicks) + 500;
        public const int EndGraceTicks = 150;

        /// <summary>한 라운드에 낼 정답 카드 수.</summary>
        public static int QuotaFor(int players) => (int)Math.Round((2 + players * 1.25) * 0.9);
    }
}
