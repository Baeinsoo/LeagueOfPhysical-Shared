using NUnit.Framework;

namespace LOP.Tests
{
    public class CardRushJudgeTests
    {
        private const long Start = 1000;
        private static readonly CardRushCourse course = new CardRushCourse(99, 40, CardRushSections.Default);
        private static readonly CardRushTiming timing = new CardRushTiming(15, 30);

        private static CardRushTapResult Tick(ref CardRushRunnerState s, long t, bool tap)
            => CardRushJudge.Advance(ref s, t, tap, Start, course, timing);

        //  돌고 있는 상태에서 want 색이 처음 보이는(showing) / 안 보이는(!showing) 시작 뒤 틱
        private static long FirstTick(CardRushRunnerState s, bool showing, CardColor want)
        {
            for (long t = s.SpinStartTick + 1; t < s.SpinStartTick + CardRushCard.WindowTicks; t++)
                if (CardRushCard.TryColorAt(s, t, course, out var c) && (c == want) == showing) return t;
            throw new System.InvalidOperationException("못 찾음");
        }

        [Test]
        public void 첫_탭은_시작만_한다()
        {
            var s = new CardRushRunnerState { Streak = 2, BestStreak = 2 };
            Assert.AreEqual(CardRushTapResult.Started, Tick(ref s, Start + 5, true));
            Assert.AreEqual(Start + 5, s.SpinStartTick);
            Assert.AreEqual(0, s.Step); Assert.AreEqual(0, s.Attempt); Assert.AreEqual(0, s.Misses); Assert.AreEqual(2, s.Streak);
            Assert.IsTrue(CardRushCard.TryColorAt(s, Start + 5, course, out var first));
            Assert.AreEqual(course.StartColor(0, 0), first, "시작 틱에 첫 장이 바로 보인다");
        }

        [Test]
        public void 두번째_탭이_보이는_색으로_던진다()
        {
            var s = new CardRushRunnerState { SpinStartTick = Start };
            long t = FirstTick(s, true, course.RequiredColor(0));
            Assert.AreEqual(CardRushTapResult.Correct, Tick(ref s, t, true));
            Assert.AreEqual(1, s.Step); Assert.AreEqual(t, s.MoveStartTick); Assert.AreEqual(0, s.SpinStartTick);
            Assert.AreEqual(1, s.Streak); Assert.AreEqual(0, s.Attempt);
        }

        [Test]
        public void 틀리면_30틱_멈춤_뒤_같은_칸_대기()
        {
            var s = new CardRushRunnerState { SpinStartTick = Start, Streak = 4, BestStreak = 4 };
            long t = FirstTick(s, false, course.RequiredColor(0));
            Assert.AreEqual(CardRushTapResult.Wrong, Tick(ref s, t, true));
            Assert.AreEqual(0, s.Step); Assert.AreEqual(t + 30, s.StunEndTick); Assert.AreEqual(1, s.Attempt);
            Assert.AreEqual(0, s.Streak); Assert.AreEqual(4, s.BestStreak); Assert.AreEqual(1, s.Misses);
            Assert.AreEqual(0, s.SpinStartTick, "카드가 손을 떠났다");
            Assert.AreEqual(t + 30, CardRushCard.ReadyTick(s, Start, 15));
            //  멈춤이 끝나도 탭 전엔 대기 — 저절로 돌지 않는다
            for (long u = t + 30; u < t + 400; u++) Assert.AreEqual(CardRushTapResult.Ignored, Tick(ref s, u, false));
            Assert.AreEqual(0, s.SpinStartTick);
        }

        [Test]
        public void 이동_중_멈춤_중_출발_전_탭은_시작도_안_한다()
        {
            var moving = new CardRushRunnerState { Step = 3, MoveStartTick = 2000 };
            Assert.AreEqual(CardRushTapResult.Ignored, Tick(ref moving, 2010, true));
            Assert.AreEqual(0, moving.SpinStartTick);
            var stunned = new CardRushRunnerState { StunEndTick = 3000, Attempt = 1 };
            Assert.AreEqual(CardRushTapResult.Ignored, Tick(ref stunned, 2990, true));
            Assert.AreEqual(0, stunned.SpinStartTick);
            var early = new CardRushRunnerState();
            Assert.AreEqual(CardRushTapResult.Ignored, Tick(ref early, Start - 1, true));
            Assert.AreEqual(0, early.SpinStartTick);
            //  양성 대조: 준비 틱에는 시작된다 — 무시가 "언제나 무시"가 아니다
            Assert.AreEqual(CardRushTapResult.Started, Tick(ref moving, 2015, true));
            Assert.AreEqual(CardRushTapResult.Started, Tick(ref stunned, 3000, true));
            Assert.AreEqual(CardRushTapResult.Started, Tick(ref early, Start, true));
        }

        [Test]
        public void 연타는_첫_장_도박()
        {
            int right = 0, wrong = 0;
            for (int step = 0; step < 40; step++)
            {
                var s = new CardRushRunnerState { Step = step, MoveStartTick = 2000 };
                Assert.AreEqual(CardRushTapResult.Started, Tick(ref s, 2015, true));
                var r = Tick(ref s, 2016, true);
                bool firstIsRight = course.StartColor(step, 0) == course.RequiredColor(step);
                Assert.AreEqual(firstIsRight, r == CardRushTapResult.Correct || r == CardRushTapResult.Finished, $"칸 {step}");
                if (firstIsRight) right++; else wrong++;
            }
            Assert.Greater(right, 0); Assert.Greater(wrong, 0);   // 표본이 두 갈래를 다 지난다
        }

        [Test]
        public void 육초가_되면_그_틱의_색으로_저절로_던진다()
        {
            int right = 0, wrong = 0;
            for (int step = 0; step < 40; step++)
            {
                var s = new CardRushRunnerState { Step = step, MoveStartTick = 2000, SpinStartTick = 2015 };
                for (long t = 2016; t < 2015 + CardRushCard.WindowTicks; t++)
                    Assert.AreEqual(CardRushTapResult.Ignored, Tick(ref s, t, false));
                Assert.AreEqual(2015, s.SpinStartTick, "299틱째엔 아직 돈다");

                var expected = (CardColor)(((int)course.StartColor(step, 0) + CardRushCard.WindowTicks / CardRushCard.DwellTicks) % CardColors.Count);
                var r = Tick(ref s, 2015 + CardRushCard.WindowTicks, false);
                Assert.AreEqual(0, s.SpinStartTick);
                if (expected == course.RequiredColor(step)) { Assert.AreEqual(step + 1, s.Step); right++; }
                else { Assert.AreEqual(CardRushTapResult.Wrong, r); Assert.AreEqual(2315 + 30, s.StunEndTick); wrong++; }
            }
            Assert.Greater(right, 0); Assert.Greater(wrong, 0);
        }

        [Test]
        public void 마감_틱의_탭은_한_번만_던진다()
        {
            var a = new CardRushRunnerState { SpinStartTick = 2000, StunEndTick = 1990, Attempt = 1 };
            var b = a;
            Tick(ref a, 2300, true);
            Tick(ref b, 2300, false);
            Assert.AreEqual(a, b, "마감 틱엔 탭이 있든 없든 같은 던짐 하나");
            Assert.AreEqual(0, a.SpinStartTick);
        }

        [Test]
        public void 대기에는_시간_제한이_없다()
        {
            var s = new CardRushRunnerState();
            for (long t = Start; t < Start + 10_000; t += 7) Assert.AreEqual(CardRushTapResult.Ignored, Tick(ref s, t, false));
            Assert.AreEqual(new CardRushRunnerState(), s);
        }

        [Test]
        public void 틱_0에는_시작하지_않는다()
        {
            var s = new CardRushRunnerState();
            Assert.AreEqual(CardRushTapResult.Ignored, CardRushJudge.Advance(ref s, 0, true, 0, course, timing));
            Assert.AreEqual(0, s.SpinStartTick);
            Assert.AreEqual(CardRushTapResult.Started, CardRushJudge.Advance(ref s, 1, true, 0, course, timing));
        }

        [Test]
        public void 마지막_칸을_맞히면_완주()
        {
            var s = new CardRushRunnerState { Step = 39, MoveStartTick = 5000, SpinStartTick = 5015 };
            long t = FirstTick(s, true, course.RequiredColor(39));
            Assert.AreEqual(CardRushTapResult.Finished, Tick(ref s, t, true));
            Assert.IsTrue(s.IsFinished(40)); Assert.AreEqual(t, s.MoveStartTick);
            Assert.AreEqual(CardRushTapResult.Ignored, Tick(ref s, t + 100, true), "완주 뒤 무시");
            Assert.AreEqual(0, s.SpinStartTick);
        }

        [Test]
        public void 연타는_리듬보다_느리다()
        {
            long Run(System.Func<CardRushRunnerState, long, bool> tapsAt)
            {
                var s = new CardRushRunnerState();
                for (long t = Start; t < Start + 50_000; t++)
                {
                    CardRushJudge.Advance(ref s, t, tapsAt(s, t), Start, course, timing);
                    if (s.IsFinished(40)) return s.MoveStartTick;
                }
                return long.MaxValue;
            }
            long masher = Run((s, t) => true);
            //  리듬: 대기면 바로 시작, 돌면 요구 색 창의 가운데(26틱 창의 13틱째)에 던진다
            long rhythm = Run((s, t) =>
                s.SpinStartTick == 0
                || (CardRushCard.TryColorAt(s, t, course, out var c) && c == course.RequiredColor(s.Step)
                    && (t - s.SpinStartTick) % CardRushCard.DwellTicks == CardRushCard.DwellTicks / 2));
            Assert.Less(rhythm, masher, $"리듬 {rhythm - Start}틱, 연타 {masher - Start}틱");
        }
    }
}
