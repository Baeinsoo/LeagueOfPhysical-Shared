namespace LOP
{
    public enum CardRushTapResult { Ignored, Started, Correct, Wrong, Finished }

    public static class CardRushJudge
    {
        /// <summary>
        /// 한 선수의 한 틱. 탭이 없어도 매 틱 부른다 — 6초 자동 던짐이 여기서 나야 서버와 내 예측이 같은 틱에 던진다.
        /// 대기(뒷면)에서 탭 = 시작, 도는 중 탭 = 그 틱 색으로 던짐. 이동·멈춤 중 탭은 버린다(쌓아 두면 도착 틱에 공짜로 돈다).
        /// </summary>
        public static CardRushTapResult Advance(ref CardRushRunnerState s, long tick, bool tapped, long gameplayStartTick,
                                                CardRushCourse course, in CardRushTiming timing)
        {
            //  틱 0은 "대기" 표시(SpinStartTick == 0)와 겹쳐 시작 틱으로 쓸 수 없다.
            if (course == null || tick <= 0 || tick < gameplayStartTick || s.IsFinished(course.StepCount))
                return CardRushTapResult.Ignored;
            if (tick < CardRushCard.ReadyTick(s, gameplayStartTick, timing.MoveTicks))
                return CardRushTapResult.Ignored;

            if (s.SpinStartTick == 0)
            {
                if (tapped == false) return CardRushTapResult.Ignored;   // 대기엔 시간 제한이 없다
                s.SpinStartTick = tick;
                return CardRushTapResult.Started;
            }

            bool timeUp = tick >= CardRushCard.DeadlineTick(s);
            //  같은 틱에 시작과 던짐은 없다 — 시작 다음 틱부터 던질 수 있다(연타 = 첫 장 도박).
            if (timeUp == false && (tapped == false || tick <= s.SpinStartTick)) return CardRushTapResult.Ignored;

            CardRushCard.TryColorAt(s, tick, course, out var shown);
            s.SpinStartTick = 0;

            if (shown == course.RequiredColor(s.Step))
            {
                s.MoveStartTick = tick;
                s.Step++;
                s.Attempt = 0;
                s.Streak++;
                if (s.Streak > s.BestStreak) s.BestStreak = s.Streak;
                return s.IsFinished(course.StepCount) ? CardRushTapResult.Finished : CardRushTapResult.Correct;
            }

            s.StunEndTick = tick + timing.StunTicks;
            s.Attempt++;
            s.Streak = 0;
            s.Misses++;
            return CardRushTapResult.Wrong;
        }
    }
}
