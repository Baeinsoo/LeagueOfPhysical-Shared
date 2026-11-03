namespace LOP
{
    /// <summary>
    /// 한 판의 코스 — 칸마다 요구 색과 떠 있는 틱, (칸, 시도)마다 시작 색. 매치 씨앗에서 결정론으로 나온다.
    /// 뽑는 순서가 계약이다: 바꾸면 같은 씨앗에서 옛 클라와 다른 줄이 나온다.
    /// </summary>
    public sealed class CardRushCourse
    {
        public const int MaxSameColorRun = 2;
        public static readonly ulong CourseSalt = GameFramework.Rng.Hashing.Fnv1a64("card-rush-course");
        public static readonly ulong StartSalt = GameFramework.Rng.Hashing.Fnv1a64("card-rush-start");

        private readonly CardColor[] required;
        private readonly int[] dwellTicks;
        private readonly ulong startSeed;

        public CardRushCourse(ulong matchSeed, int stepCount, System.Collections.Generic.IReadOnlyList<CardRushSection> sections)
        {
            if (stepCount < 1) throw new System.ArgumentOutOfRangeException(nameof(stepCount));
            if (!CardRushSections.Validate(sections, out var error)) throw new System.ArgumentException(error, nameof(sections));

            required = new CardColor[stepCount];
            dwellTicks = new int[stepCount];

            var rng = new GameFramework.Rng.DeterministicRandom(GameFramework.Rng.Hashing.Combine(matchSeed, CourseSalt));
            for (int i = 0; i < stepCount; i++)
            {
                int c = rng.Range(0, CardColors.Count);
                //  같은 색 셋째 줄은 남은 두 색 중 하나로 — 리듬이 단조로워지는 것만 막는다.
                if (i >= MaxSameColorRun && required[i - 1] == required[i - 2] && c == (int)required[i - 1])
                {
                    c = (c + 1 + rng.Range(0, 2)) % CardColors.Count;
                }
                required[i] = (CardColor)c;
            }

            int s = 0;
            for (int i = 0; i < stepCount; i++)
            {
                while (s + 1 < sections.Count && sections[s + 1].FromStep <= i) s++;
                dwellTicks[i] = CardRushTiming.Ticks(sections[s].DwellSeconds);
            }

            startSeed = GameFramework.Rng.Hashing.Combine(matchSeed, StartSalt);
        }

        public int StepCount => required.Length;
        public CardColor RequiredColor(int step) => required[step];
        public int DwellTicks(int step) => dwellTicks[step];

        /// <summary>같은 (칸, 시도)면 모든 선수에게 같은 시작 색 — 공정.</summary>
        public CardColor StartColor(int step, int attempt)
        {
            var rng = new GameFramework.Rng.DeterministicRandom(
                GameFramework.Rng.Hashing.Combine(GameFramework.Rng.Hashing.Combine(startSeed, (ulong)step), (ulong)attempt));
            return (CardColor)rng.Range(0, CardColors.Count);
        }
    }
}
