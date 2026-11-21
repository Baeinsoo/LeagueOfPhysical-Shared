using UnityEngine;

namespace LOP
{
    public enum TriGrabColor { Red = 0, Yellow = 1, Blue = 2 }
    public enum TriGrabShape { Circle = 0, Triangle = 1, Square = 2 }
    /// <summary>
    /// 주문 종류. Ink = 글자색, Word = 쓰인 말(색 이름 0~2 · 모양 이름 3~5), Match = 글자(색 이름)와 글자색이 같음,
    /// Shape = 카드 모양, ShapeMatch = 글자(모양 이름)와 카드 모양이 같음.
    /// </summary>
    public enum TriGrabRuleType { Ink = 0, Word = 1, Match = 2, Shape = 3, ShapeMatch = 4 }
    public enum TriGrabPhase { Waiting = 0, Prep = 1, Live = 2, Resolved = 3, Over = 4 }
    public enum TriGrabHookResult { None = 0, Correct = 1, Wrong = 2 }

    /// <summary>카드에 쓰이는 말. 0~2는 색 이름(빨강·노랑·파랑), 3~5는 모양 이름(원·세모·네모).</summary>
    public static class TriGrabWords
    {
        public const int Count = 6;
        public const int FirstShapeWord = 3;

        public static bool IsColorWord(int word) => word >= 0 && word < FirstShapeWord;
        public static bool IsShapeWord(int word) => word >= FirstShapeWord && word < Count;
        public static int ShapeWord(int shape) => FirstShapeWord + shape;

        public static string ColorName(int c)
        {
            switch (c) { case 0: return "빨강"; case 1: return "노랑"; default: return "파랑"; }
        }

        public static string ShapeName(int s)
        {
            switch (s) { case 0: return "원"; case 1: return "세모"; default: return "네모"; }
        }

        public static string Name(int word) => IsShapeWord(word) ? ShapeName(word - FirstShapeWord) : ColorName(word);

        /// <summary>마지막 글자에 받침이 있나(조사 고르기 — "빨강이라고" / "세모라고").</summary>
        public static bool HasFinalConsonant(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;
            char ch = word[word.Length - 1];
            return ch >= 0xAC00 && ch <= 0xD7A3 && (ch - 0xAC00) % 28 != 0;
        }
    }

    public struct TriGrabCard
    {
        public int Id, Word, Ink, Shape;
        public bool Cycles;
        public int PeriodTicks;
        public long T0Tick;

        /// <summary>도는 카드는 T0부터 주기마다 빨→노→파로 한 칸씩 간다.</summary>
        public int InkAt(long tick)
        {
            if (!Cycles || PeriodTicks <= 0) return Ink;
            long k = tick > T0Tick ? (tick - T0Tick) / PeriodTicks : 0;
            return (int)((Ink + k) % 3);
        }
    }

    /// <summary>
    /// 주문. 한 조건(<see cref="Type"/>·<see cref="Color"/>)에, 후반 라운드엔 모양 거르기(<see cref="ShapeFilter"/>)와
    /// 뒤집기(<see cref="Negate"/> — "~이 아닌")가 붙는다. <see cref="DecoyInk"/>는 주문 글의 색 이름을 다른 색으로
    /// 칠하라는 표시일 뿐 판정에는 안 쓴다(주문을 읽는 순간부터 속이는 장치).
    /// </summary>
    public readonly struct TriGrabRule
    {
        public readonly TriGrabRuleType Type;
        /// <summary>Ink는 색, Word는 말 번호(0~5), Shape는 모양. Match·ShapeMatch는 안 쓴다.</summary>
        public readonly int Color;
        // 기본값(default)이 "거르기·속임 없음"이 되도록 +1 해서 담는다.
        readonly int shapeFilterPlus1, decoyInkPlus1;
        /// <summary>-1이면 없음. 0~2면 그 모양 카드 중에서만 고른다(복합 주문).</summary>
        public int ShapeFilter => shapeFilterPlus1 - 1;
        public readonly bool Negate;
        /// <summary>-1이면 없음. 주문 글의 색 이름을 이 색으로 칠한다.</summary>
        public int DecoyInk => decoyInkPlus1 - 1;

        public TriGrabRule(TriGrabRuleType type, int color) : this(type, color, -1, false, -1) { }

        public TriGrabRule(TriGrabRuleType type, int color, int shapeFilter, bool negate, int decoyInk)
        {
            Type = type; Color = color; Negate = negate;
            shapeFilterPlus1 = shapeFilter < 0 ? 0 : shapeFilter + 1;
            decoyInkPlus1 = decoyInk < 0 ? 0 : decoyInk + 1;
        }

        public bool IsCompound => ShapeFilter >= 0;

        /// <summary>판정이 글자색을 보나 — 그러면 도는 카드는 때에 따라 정답이 된다.</summary>
        public bool DependsOnInk => Type == TriGrabRuleType.Ink || Type == TriGrabRuleType.Match;

        public bool Matches(in TriGrabCard c, long tick)
        {
            if (ShapeFilter >= 0 && c.Shape != ShapeFilter) return false;
            return Judge(c, c.InkAt(tick));
        }

        /// <summary>글자색을 <paramref name="ink"/>로 본 판정(도는 카드가 언젠가 정답이 되나 따질 때).</summary>
        public bool MatchesWithInk(in TriGrabCard c, int ink)
        {
            if (ShapeFilter >= 0 && c.Shape != ShapeFilter) return false;
            return Judge(c, ink);
        }

        // "글자와 색이 다른"은 색 이름이 쓰인 카드 중에서만 고른다 — 모양 이름 카드는 색과 같을 수도 다를 수도 없다.
        // "글자와 모양이 다른"도 같은 이유로 모양 이름 카드만.
        bool Judge(in TriGrabCard c, int ink)
        {
            if (Negate && Type == TriGrabRuleType.Match && !TriGrabWords.IsColorWord(c.Word)) return false;
            if (Negate && Type == TriGrabRuleType.ShapeMatch && !TriGrabWords.IsShapeWord(c.Word)) return false;
            return BaseMatches(c, ink) != Negate;
        }

        bool BaseMatches(in TriGrabCard c, int ink)
        {
            switch (Type)
            {
                case TriGrabRuleType.Ink: return ink == Color;
                case TriGrabRuleType.Word: return c.Word == Color;
                case TriGrabRuleType.Match: return TriGrabWords.IsColorWord(c.Word) && c.Word == ink;
                case TriGrabRuleType.Shape: return c.Shape == Color;
                default: return TriGrabWords.IsShapeWord(c.Word) && c.Word - TriGrabWords.FirstShapeWord == c.Shape;
            }
        }

        /// <summary>주문 글에 든 색 이름(HUD가 칠한다). 없으면 null.</summary>
        public string ColorToken
        {
            get
            {
                if (Type == TriGrabRuleType.Ink) return TriGrabWords.ColorName(Color);
                if (Type == TriGrabRuleType.Word && TriGrabWords.IsColorWord(Color)) return TriGrabWords.ColorName(Color);
                return null;
            }
        }

        /// <summary>색 이름을 칠할 색 — 속임이면 다른 색, 아니면 그 색 그대로.</summary>
        public int ColorTokenInk => DecoyInk >= 0 ? DecoyInk : Color;

        public string KoreanText
        {
            get
            {
                if (IsCompound) return Modifier() + " " + TriGrabWords.ShapeName(ShapeFilter);
                if (Type == TriGrabRuleType.Shape && !Negate) return TriGrabWords.ShapeName(Color) + " 카드";
                return Modifier() + " 카드";
            }
        }

        string Modifier()
        {
            switch (Type)
            {
                case TriGrabRuleType.Ink:
                {
                    string c = TriGrabWords.ColorName(Color);
                    return Negate ? "글자색이 " + c + (TriGrabWords.HasFinalConsonant(c) ? "이" : "가") + " 아닌" : "글자색이 " + c + "인";
                }
                case TriGrabRuleType.Word:
                {
                    string w = TriGrabWords.Name(Color);
                    string quote = "'" + w + "'" + (TriGrabWords.HasFinalConsonant(w) ? "이라고" : "라고");
                    return Negate ? quote + " 쓰이지 않은" : quote + " 쓰인";
                }
                case TriGrabRuleType.Match: return Negate ? "글자와 색이 다른" : "글자와 색이 같은";
                case TriGrabRuleType.Shape:
                {
                    string s = TriGrabWords.ShapeName(Color);
                    return Negate ? s + (TriGrabWords.HasFinalConsonant(s) ? "이" : "가") + " 아닌" : s + "인";
                }
                default: return Negate ? "글자와 모양이 다른" : "글자와 모양이 같은";
            }
        }

        public static string ColorName(int c) => TriGrabWords.ColorName(c);
    }

    /// <summary>회전판 각도. 준비 중엔 안 돌고, Live 동안만 돌고, 라운드가 끝나면 멈춘다.</summary>
    public struct TriGrabRoundClock
    {
        public float BaseAngle0, Omega;
        public long LiveStartTick, FreezeTick;

        public float BaseAt(long tick)
        {
            long t = tick < LiveStartTick ? LiveStartTick : (tick > FreezeTick ? FreezeTick : tick);
            return BaseAngle0 + Omega * TriGrabConfig.TickSeconds * (t - LiveStartTick);
        }
    }

    /// <summary>나가 있는 갈고리 하나. 길이·끝 자리는 틱을 넣어 계산하므로 틱마다 상태를 보내지 않는다.</summary>
    public class TriGrabHook
    {
        public string OwnerId;
        public long FireTick;
        public Vector2 Origin, Dir;
        public long TurnTick;
        public float TurnLength;
        public TriGrabCard? Carried;
        public TriGrabHookResult Result;
        public long EndTick;
        /// <summary>
        /// 서버 전용. 쏜 순간의 회전판으로 미리 계산한, 이 갈고리가 처음 닿을 카드(슬롯·카드 id). 이 카드만 잡는다 —
        /// 그 사이 남이 가져가면 옆 카드를 집지 않고 빈손으로 돌아온다. TargetCardId 0 = 닿을 카드 없음(빗나감).
        /// </summary>
        public int TargetSlot = -1, TargetCardId;

        public static float RetractPerTick(bool carried) =>
            TriGrabConfig.HookSpeedPerTick * (carried ? TriGrabConfig.RetractCard : TriGrabConfig.RetractEmpty);

        public bool Extending(long t) => t <= TurnTick;

        public float LengthAt(long t)
        {
            if (t <= TurnTick)
                return Mathf.Clamp(TriGrabConfig.HookSpeedPerTick * (t - FireTick), 0f, TriGrabConfig.HookLength);
            return Mathf.Max(0f, TurnLength - RetractPerTick(Carried.HasValue) * (t - TurnTick));
        }

        public Vector2 TipAt(long t) => Origin + Dir * (TriGrabConfig.BodyRadius + LengthAt(t));

        /// <summary>꺾인 틱에서 길이만큼 되감는 데 걸리는 틱을 더한 끝 틱. 부동소수 오차로 한 틱 더 세지 않게 살짝 깎아 올린다.</summary>
        public static long ComputeEndTick(long turnTick, float turnLength, bool carried)
        {
            return turnTick + (long)Mathf.Ceil(turnLength / RetractPerTick(carried) - 1e-4f);
        }
    }

    public sealed class TriGrabPlayerScore
    {
        /// <summary>판 전체 누적 점수.</summary>
        public int Score, Mistakes;
        /// <summary>이번 라운드에 몇 번째로 정답을 냈나. 0이면 아직 — 정답을 낸 사람은 그 라운드에서 더 쏘지 못한다.</summary>
        public int RoundPlace;
        public int RoundPoints;
        public long ReloadUntilTick;
        /// <summary>0이면 아직 정답 없음.</summary>
        public int BestRtMs;
    }

    public sealed class TriGrabSlot
    {
        public TriGrabCard? Card;
        public long EmptyUntilTick;
        /// <summary>그 카드가 정답이 된 틱. -1이면 지금 정답이 아니다. 서버·봇만 쓴다.</summary>
        public long ValidSinceTick = -1;
    }

    public static class TriGrabCarousel
    {
        public static Vector2 SlotPosition(int slot, float baseAngle)
        {
            float a = baseAngle + slot * 2f * Mathf.PI / TriGrabConfig.Slots;
            return TriGrabConfig.RingRadius * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }
    }
}
