namespace LOP
{
    public static class CardRushCard
    {
        /// <summary>
        /// 한 색이 떠 있는 틱. 트페 W 한 장 0.528초 ÷ 0.02초 = 26.4 → 26(0.52초).
        /// 27(0.54초)보다 가깝고, 초 → 틱 규칙(<see cref="CardRushTiming.Ticks"/>)이 내는 값 그대로라 예외가 없다.
        /// </summary>
        public const int DwellTicks = 26;

        /// <summary>돌기 시작한 뒤 이 틱 수가 되는 틱에 그 색으로 저절로 던진다(6초).</summary>
        public const int WindowTicks = 300;

        /// <summary>카드가 머리 위에 뒷면으로 떠 첫 탭을 받기 시작하는 틱 — 따로 저장하지 않고 이동·멈춤에서 계산한다.</summary>
        public static long ReadyTick(in CardRushRunnerState s, long gameplayStartTick, int moveTicks)
        {
            if (s.MoveStartTick == 0 && s.StunEndTick == 0) return gameplayStartTick;
            long afterMove = s.MoveStartTick == 0 ? 0 : s.MoveStartTick + moveTicks;
            return System.Math.Max(afterMove, s.StunEndTick);
        }

        public static bool IsSpinning(in CardRushRunnerState s) => s.SpinStartTick != 0;

        /// <summary>자동 던짐 틱. 돌지 않으면 없음(long.MaxValue).</summary>
        public static long DeadlineTick(in CardRushRunnerState s)
            => s.SpinStartTick == 0 ? long.MaxValue : s.SpinStartTick + WindowTicks;

        /// <summary>그 틱에 카드가 보여 주는 색. 돌기 전·완주 뒤면 false(뒷면 또는 손을 떠남).</summary>
        public static bool TryColorAt(in CardRushRunnerState s, long tick, CardRushCourse course, out CardColor color)
        {
            color = default;
            if (course == null || s.SpinStartTick == 0 || tick < s.SpinStartTick || s.IsFinished(course.StepCount)) return false;
            long turns = (tick - s.SpinStartTick) / DwellTicks;
            color = (CardColor)(((int)course.StartColor(s.Step, s.Attempt) + (int)(turns % CardColors.Count)) % CardColors.Count);
            return true;
        }
    }
}
