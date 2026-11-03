using NUnit.Framework;

namespace LOP.Tests
{
    public class CardRushCardTests
    {
        [Test]
        public void 떠_있는_틱은_0_528초에_가장_가까운_정수()
        {
            Assert.AreEqual(26, CardRushCard.DwellTicks);
            Assert.AreEqual(CardRushTiming.Ticks(0.528f), CardRushCard.DwellTicks, "초 → 틱 규칙과 같은 값이어야 예외가 없다");
        }

        [Test]
        public void 제한은_300틱() => Assert.AreEqual(CardRushTiming.Ticks(6f), CardRushCard.WindowTicks);

        [Test]
        public void 돌아가는_순서는_파랑_빨강_노랑()
        {
            Assert.AreEqual(CardColor.Red, CardColors.Next(CardColor.Blue));
            Assert.AreEqual(CardColor.Yellow, CardColors.Next(CardColor.Red));
            Assert.AreEqual(CardColor.Blue, CardColors.Next(CardColor.Yellow));
        }

        [Test]
        public void 대기_중엔_뒷면()
        {
            var course = new CardRushCourse(99, 40);
            var waiting = new CardRushRunnerState { Step = 2, MoveStartTick = 2000 };   // 준비 틱 2015, 아직 안 눌렀다
            Assert.IsFalse(CardRushCard.TryColorAt(waiting, 5000, course, out _));
            Assert.IsFalse(CardRushCard.IsSpinning(waiting));
            Assert.AreEqual(long.MaxValue, CardRushCard.DeadlineTick(waiting));
        }

        [Test]
        public void 시작_틱에_첫_장_26틱마다_다음_색()
        {
            var course = new CardRushCourse(99, 40);
            var s = new CardRushRunnerState { Step = 4, Attempt = 1, MoveStartTick = 2000, SpinStartTick = 2100 };
            var first = course.StartColor(4, 1);
            Assert.IsTrue(CardRushCard.TryColorAt(s, 2100, course, out var c0)); Assert.AreEqual(first, c0);
            Assert.IsTrue(CardRushCard.TryColorAt(s, 2125, course, out var c25)); Assert.AreEqual(first, c25);
            Assert.IsTrue(CardRushCard.TryColorAt(s, 2126, course, out var c26)); Assert.AreEqual(CardColors.Next(first), c26);
            Assert.IsTrue(CardRushCard.TryColorAt(s, 2100 + 78, course, out var c78)); Assert.AreEqual(first, c78, "세 장이면 한 바퀴");
            Assert.IsFalse(CardRushCard.TryColorAt(s, 2099, course, out _), "시작 전 틱은 뒷면");
            Assert.AreEqual(2400, CardRushCard.DeadlineTick(s));
        }

        [Test]
        public void 준비_틱은_이동_끝과_멈춤_끝_중_늦은_쪽()
        {
            Assert.AreEqual(1015, CardRushCard.ReadyTick(new CardRushRunnerState { MoveStartTick = 1000 }, 500, 15));
            Assert.AreEqual(1100, CardRushCard.ReadyTick(new CardRushRunnerState { MoveStartTick = 1000, StunEndTick = 1100 }, 500, 15));
            Assert.AreEqual(500, CardRushCard.ReadyTick(new CardRushRunnerState(), 500, 15));
        }
    }
}
