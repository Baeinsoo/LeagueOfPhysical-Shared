using System.Numerics;

namespace LOP.TriGrab
{
    public enum TriGrabColor { Red = 0, Yellow = 1, Blue = 2, Green = 3 }

    public static class TriGrabColors
    {
        public const int Count = 4;

        /// <summary>글씨용 밝은색(빨강·노랑·파랑·초록 순서).</summary>
        public static readonly string[] InkHex = { "#ff4d4d", "#ffd23f", "#4d8dff", "#3ddc84" };

        /// <summary>바탕용 진한색.</summary>
        public static readonly string[] FillHex = { "#8a2a2a", "#8a7414", "#23468c", "#1b7346" };
    }

    public enum TriGrabRuleType { Ink, Word, Match }

    /// <summary>Prep = 주문, Live = 잡기, Resolved = 결과.</summary>
    public enum TriGrabPhase { Waiting, Prep, Live, Resolved, Over }

    /// <summary>Value는 Match 규칙에선 쓰지 않는다. Lure는 Value와 다른 "미끼" 색.</summary>
    public readonly struct TriGrabRule
    {
        public readonly TriGrabRuleType Type;
        public readonly int Value;
        public readonly int Lure;

        public TriGrabRule(TriGrabRuleType type, int value, int lure)
        {
            Type = type;
            Value = value;
            Lure = lure;
        }
    }

    public sealed class TriGrabCard
    {
        public int Id;
        public int Word;
        public int Ink;
        public int Fill;
        public bool Ok;
        public long BornTick;
        public Vector2 Pos;
        public Vector2 Vel;
        public bool Gone;
        /// <summary>끌고 가는 사람. 없으면 null.</summary>
        public string CarriedBy;
        public long BumpTick;
    }
}
