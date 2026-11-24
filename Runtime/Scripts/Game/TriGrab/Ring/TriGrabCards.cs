using System;

namespace LOP.TriGrab
{
    /// <summary>시안(trigrab-ring3d-v9)의 pickRule·randCard·valid·isOk·isTrap. 바탕 속임은 늘 켜짐.</summary>
    public static class TriGrabCards
    {
        const int MaxTries = 800;

        /// <summary>솟아오르는 정도 0~1(시안 popK). 0.5 미만이면 아직 손에 안 닿는다.</summary>
        public static float PopK(TriGrabCard c, long tick)
        {
            float k = Math.Min(1f, (float)(tick - c.BornTick) / TriGrabConfig.PopTicks);
            return 1f - (1f - k) * (1f - k);
        }

        public static TriGrabRule PickRule(Random rng, TriGrabRule? previous)
        {
            int k = rng.Next(3);
            if (previous.HasValue && k == (int)previous.Value.Type)
            {
                // 같은 종류가 이어지지 않게 나머지 둘 중 하나.
                int o = rng.Next(2);
                k = (k + 1 + o) % 3;
            }
            int v = rng.Next(TriGrabColors.Count);
            int lure = rng.Next(TriGrabColors.Count - 1);
            if (lure >= v) lure++;
            return new TriGrabRule((TriGrabRuleType)k, v, lure);
        }

        public static (int word, int ink, int fill) Make(TriGrabRule rule, Random rng)
        {
            for (int i = 0; i < MaxTries; i++)
            {
                var c = Raw(rule, rng);
                if (Valid(c.word, c.ink, c.fill, rule)) return c;
            }
            // 800번 안에 못 찾는 일은 사실상 없다. 그래도 항상 규칙에 맞는 카드를 돌려준다.
            return (0, 1, 2);
        }

        static (int word, int ink, int fill) Raw(TriGrabRule R, Random rng)
        {
            int n = TriGrabColors.Count;
            int ink = rng.Next(n), fill = rng.Next(n), word = rng.Next(n);
            if (R.Type == TriGrabRuleType.Match)
            {
                if (rng.NextDouble() < .3) ink = word;
                if (rng.NextDouble() < .4) fill = word;
            }
            else
            {
                if (rng.NextDouble() < .3) word = R.Value;
                if (rng.NextDouble() < .3) ink = R.Value;
                if (rng.NextDouble() < .35) fill = R.Value;
                if (rng.NextDouble() < .12) ink = R.Lure;
            }
            return (word, ink, fill);
        }

        /// <summary>색 범위 검사만 한다(규칙과 무관한 구조 검사).</summary>
        public static bool Valid(int word, int ink)
        {
            return word >= 0 && word < TriGrabColors.Count && ink >= 0 && ink < TriGrabColors.Count;
        }

        /// <summary>글씨 색과 바탕이 같으면 글씨가 안 보이고, Match가 아니면 글자와 글씨 색이 같은 카드도 헷갈리게 걸러낸다.</summary>
        public static bool Valid(int word, int ink, int fill, TriGrabRule rule)
        {
            return Valid(word, ink) && fill >= 0 && fill < TriGrabColors.Count
                && fill != ink && (rule.Type == TriGrabRuleType.Match || word != ink);
        }

        public static bool IsOk(int word, int ink, TriGrabRule rule)
        {
            switch (rule.Type)
            {
                case TriGrabRuleType.Ink: return ink == rule.Value;
                case TriGrabRuleType.Word: return word == rule.Value;
                default: return word == ink;
            }
        }

        public static bool IsTrap(int word, int ink, int fill, TriGrabRule rule)
        {
            if (rule.Type == TriGrabRuleType.Match) return fill == word;
            return ink == rule.Value || word == rule.Value || fill == rule.Value || ink == rule.Lure;
        }
    }
}
