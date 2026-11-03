namespace LOP
{
    public enum CardRushGrabResult { Ignored, Correct, Wrong, Finished }

    public static class CardRushJudge
    {
        public static CardRushGrabResult Grab(ref CardRushRunnerState s, long tick, long gameplayStartTick,
                                              CardRushCourse course, in CardRushTiming timing)
        {
            if (course == null || tick < gameplayStartTick) return CardRushGrabResult.Ignored;
            //  뒷면(이동·멈춤 중)이면 무시 — 벌도 없고 쌓아 두지도 않는다(연타가 공짜가 되지 않게).
            if (!CardRushCard.TryColorAt(s, tick, gameplayStartTick, course, timing.MoveTicks, out var shown)) return CardRushGrabResult.Ignored;

            if (shown == course.RequiredColor(s.Step))
            {
                s.MoveStartTick = tick;
                s.Step++;
                s.Attempt = 0;
                s.Streak++;
                if (s.Streak > s.BestStreak) s.BestStreak = s.Streak;
                return s.IsFinished(course.StepCount) ? CardRushGrabResult.Finished : CardRushGrabResult.Correct;
            }

            s.StunEndTick = tick + timing.StunTicks;
            s.Attempt++;
            s.Streak = 0;
            s.Misses++;
            return CardRushGrabResult.Wrong;
        }
    }
}
