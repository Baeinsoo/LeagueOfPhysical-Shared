namespace LOP
{
    /// <summary>코스 구간 한 줄 — 이 칸부터 한 색이 떠 있는 시간.</summary>
    [System.Serializable]
    public struct CardRushSection
    {
        public int FromStep;
        public float DwellSeconds;

        public CardRushSection(int fromStep, float dwellSeconds) { FromStep = fromStep; DwellSeconds = dwellSeconds; }
    }

    public static class CardRushSections
    {
        public const float MinDwellSeconds = 0.1f;

        public static readonly CardRushSection[] Default =
        {
            new CardRushSection(0, 0.40f), new CardRushSection(15, 0.33f), new CardRushSection(30, 0.27f),
        };

        public static bool Validate(System.Collections.Generic.IReadOnlyList<CardRushSection> sections, out string error)
        {
            error = null;
            if (sections == null || sections.Count == 0) { error = "구간 표가 비었다"; return false; }
            if (sections[0].FromStep != 0) { error = "첫 구간은 0번 칸에서 시작해야 한다"; return false; }
            for (int i = 0; i < sections.Count; i++)
            {
                if (sections[i].DwellSeconds < MinDwellSeconds) { error = $"구간 {i}의 시간이 {MinDwellSeconds}초보다 짧다"; return false; }
                if (i > 0 && sections[i].FromStep <= sections[i - 1].FromStep) { error = $"구간 {i}의 시작 칸이 앞 구간보다 크지 않다"; return false; }
            }
            return true;
        }
    }
}
