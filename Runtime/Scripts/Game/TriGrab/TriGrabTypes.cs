using UnityEngine;

namespace LOP
{
    public enum TriGrabColor { Red = 0, Yellow = 1, Blue = 2 }
    public enum TriGrabRuleType { Ink = 0, Word = 1, Match = 2 }
    public enum TriGrabPhase { Waiting = 0, Prep = 1, Live = 2, Resolved = 3, Over = 4 }
    public enum TriGrabHookResult { None = 0, Correct = 1, Wrong = 2 }

    public struct TriGrabCard
    {
        public int Id, Word, Ink;
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

    public readonly struct TriGrabRule
    {
        public readonly TriGrabRuleType Type;
        public readonly int Color;

        public TriGrabRule(TriGrabRuleType type, int color) { Type = type; Color = color; }

        public bool Matches(in TriGrabCard c, long tick)
        {
            switch (Type)
            {
                case TriGrabRuleType.Ink: return c.InkAt(tick) == Color;
                case TriGrabRuleType.Word: return c.Word == Color;
                default: return c.Word == c.InkAt(tick);
            }
        }

        public string KoreanText
        {
            get
            {
                switch (Type)
                {
                    case TriGrabRuleType.Ink: return "글자색이 " + ColorName(Color) + "인 카드";
                    case TriGrabRuleType.Word: return "'" + ColorName(Color) + "'이라고 쓰인 카드";
                    default: return "글자와 색이 같은 카드";
                }
            }
        }

        public static string ColorName(int c)
        {
            switch (c) { case 0: return "빨강"; case 1: return "노랑"; default: return "파랑"; }
        }
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
