namespace LOP
{
    public static class CardRushCard
    {
        /// <summary>카드가 돌기 시작한 틱 — 따로 저장하지 않고 이동·멈춤에서 계산한다.</summary>
        public static long CardStartTick(in CardRushRunnerState s, long gameplayStartTick, int moveTicks)
        {
            if (s.MoveStartTick == 0 && s.StunEndTick == 0) return gameplayStartTick;
            long afterMove = s.MoveStartTick == 0 ? 0 : s.MoveStartTick + moveTicks;
            return System.Math.Max(afterMove, s.StunEndTick);
        }

        /// <summary>그 틱에 카드가 보여 주는 색. 뒷면(이동·멈춤·출발 전·완주)이면 false.</summary>
        public static bool TryColorAt(in CardRushRunnerState s, long tick, long gameplayStartTick,
                                      CardRushCourse course, int moveTicks, out CardColor color)
        {
            color = default;
            if (course == null || gameplayStartTick == long.MaxValue || s.IsFinished(course.StepCount)) return false;
            long start = CardStartTick(s, gameplayStartTick, moveTicks);
            if (tick < start) return false;
            long turns = (tick - start) / course.DwellTicks(s.Step);
            color = (CardColor)(((int)course.StartColor(s.Step, s.Attempt) + (int)(turns % CardColors.Count)) % CardColors.Count);
            return true;
        }
    }
}
