using NUnit.Framework;

namespace LOP.Tests
{
    public class CardRushCardTests
    {
        private static readonly CardRushCourse course = new CardRushCourse(99, 40, CardRushSections.Default);

        [Test]
        public void 출발_전엔_뒷면()
        {
            var s = new CardRushRunnerState();
            Assert.IsFalse(CardRushCard.TryColorAt(s, 999, 1000, course, 15, out _));
            Assert.IsFalse(CardRushCard.TryColorAt(s, 5000, long.MaxValue, course, 15, out _));
        }

        [Test]
        public void 시작_틱에_시작_색_떠_있는_틱마다_다음_색()
        {
            var s = new CardRushRunnerState();
            var c0 = course.StartColor(0, 0);
            int d = course.DwellTicks(0);
            Assert.IsTrue(CardRushCard.TryColorAt(s, 1000, 1000, course, 15, out var a)); Assert.AreEqual(c0, a);
            CardRushCard.TryColorAt(s, 1000 + d - 1, 1000, course, 15, out var b); Assert.AreEqual(c0, b);
            CardRushCard.TryColorAt(s, 1000 + d, 1000, course, 15, out var c); Assert.AreEqual(CardColors.Next(c0), c);
            CardRushCard.TryColorAt(s, 1000 + 3 * d, 1000, course, 15, out var e); Assert.AreEqual(c0, e);
        }

        [Test]
        public void 카드_시작은_이동_끝과_멈춤_끝_중_늦은_쪽()
        {
            Assert.AreEqual(1015, CardRushCard.CardStartTick(new CardRushRunnerState { MoveStartTick = 1000 }, 500, 15));
            Assert.AreEqual(1100, CardRushCard.CardStartTick(new CardRushRunnerState { MoveStartTick = 1000, StunEndTick = 1100 }, 500, 15));
            Assert.AreEqual(500, CardRushCard.CardStartTick(new CardRushRunnerState(), 500, 15));
        }
    }
}
