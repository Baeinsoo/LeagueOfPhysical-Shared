using NUnit.Framework;

namespace LOP.Tests
{
    public class CardRushJudgeTests
    {
        private static readonly CardRushCourse course = new CardRushCourse(99, 40, CardRushSections.Default);
        private static readonly CardRushTiming timing = new CardRushTiming(15, 30);

        //  요구 색이 처음 뜨는(또는 안 뜨는) 틱을 찾는 도우미
        private static long FirstTickShowing(CardRushRunnerState s, CardColor want, long from)
        {
            for (long t = from; t < from + 1000; t++)
                if (CardRushCard.TryColorAt(s, t, 1000, course, 15, out var c) && c == want) return t;
            throw new System.InvalidOperationException("못 찾음");
        }

        private static long FirstTickNotShowing(CardRushRunnerState s, CardColor want, long from)
        {
            for (long t = from; t < from + 1000; t++)
                if (CardRushCard.TryColorAt(s, t, 1000, course, 15, out var c) && c != want) return t;
            throw new System.InvalidOperationException("못 찾음");
        }

        [Test]
        public void 맞으면_한_칸_나아가고_이동_시작()
        {
            var s = new CardRushRunnerState();
            long t = FirstTickShowing(s, course.RequiredColor(0), 1000);
            Assert.AreEqual(CardRushGrabResult.Correct, CardRushJudge.Grab(ref s, t, 1000, course, timing));
            Assert.AreEqual(1, s.Step); Assert.AreEqual(t, s.MoveStartTick); Assert.AreEqual(1, s.Streak); Assert.AreEqual(0, s.Attempt);
        }

        [Test]
        public void 틀리면_30틱_멈춤_시도_더하기_연속_0()
        {
            var s = new CardRushRunnerState { Streak = 4, BestStreak = 4 };
            long t = FirstTickNotShowing(s, course.RequiredColor(0), 1000);
            Assert.AreEqual(CardRushGrabResult.Wrong, CardRushJudge.Grab(ref s, t, 1000, course, timing));
            Assert.AreEqual(0, s.Step); Assert.AreEqual(t + 30, s.StunEndTick); Assert.AreEqual(1, s.Attempt);
            Assert.AreEqual(0, s.Streak); Assert.AreEqual(4, s.BestStreak); Assert.AreEqual(1, s.Misses);
        }

        [Test]
        public void 이동_중_멈춤_중_출발_전_탭은_무시()
        {
            var moving = new CardRushRunnerState { Step = 3, MoveStartTick = 2000 };
            Assert.AreEqual(CardRushGrabResult.Ignored, CardRushJudge.Grab(ref moving, 2010, 1000, course, timing)); // 이동 중
            Assert.AreEqual(3, moving.Step); Assert.AreEqual(2000, moving.MoveStartTick); Assert.AreEqual(0, moving.Misses);
            var stunned = new CardRushRunnerState { StunEndTick = 3000, Attempt = 1 };
            Assert.AreEqual(CardRushGrabResult.Ignored, CardRushJudge.Grab(ref stunned, 2990, 1000, course, timing)); // 멈춤 중
            Assert.AreEqual(3000, stunned.StunEndTick); Assert.AreEqual(1, stunned.Attempt);
            var early = new CardRushRunnerState();
            Assert.AreEqual(CardRushGrabResult.Ignored, CardRushJudge.Grab(ref early, 999, 1000, course, timing)); // 출발 전
            Assert.AreEqual(0, early.Misses); Assert.AreEqual(0, early.StunEndTick);
            // 양성 대조: 같은 멈춤 상태라도 멈춤이 끝난 틱 뒤에는 판정된다(무시가 "언제나 무시"가 아니다).
            Assert.AreNotEqual(CardRushGrabResult.Ignored, CardRushJudge.Grab(ref stunned, 3000, 1000, course, timing));
        }

        [Test]
        public void 마지막_칸을_맞히면_완주()
        {
            var s = new CardRushRunnerState { Step = 39, MoveStartTick = 5000 };
            long t = FirstTickShowing(s, course.RequiredColor(39), 5015);
            Assert.AreEqual(CardRushGrabResult.Finished, CardRushJudge.Grab(ref s, t, 1000, course, timing));
            Assert.IsTrue(s.IsFinished(40)); Assert.AreEqual(t, s.MoveStartTick);
            Assert.AreEqual(CardRushGrabResult.Ignored, CardRushJudge.Grab(ref s, t + 100, 1000, course, timing)); // 완주 뒤 무시
        }

        [Test]
        public void 연타는_리듬보다_느리다()
        {
            long Run(System.Func<CardRushRunnerState, long, bool> tapsAt)
            {
                var s = new CardRushRunnerState();
                for (long t = 1000; t < 1000 + 50_000; t++)
                {
                    if (tapsAt(s, t)) CardRushJudge.Grab(ref s, t, 1000, course, timing);
                    if (s.IsFinished(40)) return s.MoveStartTick;
                }
                return long.MaxValue;
            }
            long spammer = Run((s, t) => true);
            // 리듬: 요구 색 창의 가운데에서만 누른다(사람이 리듬을 읽은 모습).
            long rhythm = Run((s, t) =>
                CardRushCard.TryColorAt(s, t, 1000, course, 15, out var c) && c == course.RequiredColor(s.Step)
                && (t - CardRushCard.CardStartTick(s, 1000, 15)) % course.DwellTicks(s.Step) == course.DwellTicks(s.Step) / 2);
            Assert.Less(rhythm, long.MaxValue);
            // 어림: 리듬 ≈ 1650틱, 연타 ≈ 3000틱(칸당 평균 두 번 틀림 × 30틱). 고정 씨앗이라 결과는 늘 같다.
            Assert.Greater(spammer, rhythm, "연타가 리듬보다 늦어야 한다 — 멈춤이 너무 짧으면 막 누르기가 이득이 된다");
        }
    }
}
