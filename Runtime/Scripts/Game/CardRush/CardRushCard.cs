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

        //  ── 3차: 카드가 늘 돈다(빨 → 노 → 파, 한 장 CardDwellTicks). 탭 = 지금 보이는 카드를 바로 쓴다(spec §3.3) ──

        /// <summary>카드가 도는 기준 틱 — 아직 안 썼으면 출발 틱, 썼으면 마지막 사용 + 쿨다운.</summary>
        public static long SpinBase(in CardRushRunnerState s, long gameplayStartTick)
            => s.SpinStartTick != 0 ? s.SpinStartTick : gameplayStartTick;

        /// <summary>그 틱에 보이는 카드. 출발 전·쿨다운 중이면 false(뒷면). 기절 중에도 돈다(못 쓸 뿐).</summary>
        public static bool TryCardAt(in CardRushRunnerState s, long tick, long gameplayStartTick, int lane, CardRushTrack track, out CardColor color)
        {
            color = default;
            long spinBase = SpinBase(s, gameplayStartTick);
            if (track == null || tick < spinBase) return false;
            long turns = (tick - spinBase) / CardRushRules.CardDwellTicks;
            color = (CardColor)(((int)track.FirstCard(lane, s.UseCount) + (int)(turns % CardColors.Count)) % CardColors.Count);
            return true;
        }

        /// <summary>이 틱에 탭하면 카드를 쓰나. 못 쓰는 틱의 탭은 버린다(쌓지 않는다).</summary>
        public static bool CanUse(in CardRushRunnerState s, long tick, long gameplayStartTick)
            => tick > 0 && tick >= gameplayStartTick && s.FinishTick == 0
               && tick >= SpinBase(s, gameplayStartTick) && tick >= s.StunEndTick;
    }
}
