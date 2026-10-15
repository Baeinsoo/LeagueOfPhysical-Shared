namespace LOP
{
    /// <summary>
    /// 투석기 사냥의 자리. 모두 레인 0 사대에 <see cref="ArcheryShootOffSeats.SpacingMeters"/> 간격으로 나란히 서고,
    /// 첫 라운드는 입장 순서(명단 = 엔티티 id 서수), 그 뒤 라운드는 씨앗으로 섞는다. 가장자리가 가운데보다 표적이
    /// 조금 멀어서(8명이면 평균 0.4 m) 한 사람이 내내 가장자리에 묶이지 않게 하려는 것이다.
    /// </summary>
    public static class ArcheryCatapultSeats
    {
        /// <summary>사대 한 줄에 설 수 있는 사람 수(활쏘기 최대 인원).</summary>
        public const int MaxSeats = 8;

        public static readonly ulong Salt = GameFramework.Rng.Hashing.Fnv1a64("archery-catapult-seats");

        public static int SlotOf(int rosterIndex, int round, int count, ulong matchSeed)
        {
            if (count <= 0)
            {
                return 0;
            }
            int at = ((rosterIndex % count) + count) % count;
            if (round <= 0)
            {
                return at;
            }

            System.Span<int> order = stackalloc int[count];
            for (int i = 0; i < count; i++)
            {
                order[i] = i;
            }
            //  피셔–예이츠 — 뒤에서부터 한 칸씩, 난수를 정확히 count−1번 쓴다.
            var rng = new GameFramework.Rng.DeterministicRandom(GameFramework.Rng.Hashing.Combine(
                GameFramework.Rng.Hashing.Combine(matchSeed, Salt), (ulong)round));
            for (int i = count - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            return order[at];
        }
    }
}
