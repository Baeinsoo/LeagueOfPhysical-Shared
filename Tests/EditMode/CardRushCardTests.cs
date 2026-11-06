using NUnit.Framework;

namespace LOP.Tests
{
    public class CardRushCardTests
    {
        [Test]
        public void 돌아가는_순서는_빨강_노랑_파랑()
        {
            Assert.AreEqual(CardColor.Red, CardColors.Next(CardColor.Blue));
            Assert.AreEqual(CardColor.Yellow, CardColors.Next(CardColor.Red));
            Assert.AreEqual(CardColor.Blue, CardColors.Next(CardColor.Yellow));
        }

        private const long G = 100;
        private static readonly CardRushTrack EmptyTrack =
            CardRushTrack.ForTests(140000, new CardRushObstacle[0], new CardRushZone[0], 99);

        [Test]
        public void 출발_전엔_뒷면()
        {
            var s = new CardRushRunnerState();
            Assert.IsFalse(CardRushCard.TryCardAt(s, G - 1, G, 0, EmptyTrack, out _));
            Assert.IsTrue(CardRushCard.TryCardAt(s, G, G, 0, EmptyTrack, out _));
            Assert.IsFalse(CardRushCard.TryCardAt(s, 5000, long.MaxValue, 0, EmptyTrack, out _), "출발 틱이 아직 없으면 뒷면");
        }

        [Test]
        public void 출발_틱에_첫_장_23틱마다_다음_색()
        {
            var s = new CardRushRunnerState();
            const int lane = 3;
            var first = EmptyTrack.FirstCard(lane, 0);
            Assert.IsTrue(CardRushCard.TryCardAt(s, G, G, lane, EmptyTrack, out var c0)); Assert.AreEqual(first, c0);
            Assert.IsTrue(CardRushCard.TryCardAt(s, G + 22, G, lane, EmptyTrack, out var c22)); Assert.AreEqual(first, c22);
            Assert.IsTrue(CardRushCard.TryCardAt(s, G + 23, G, lane, EmptyTrack, out var c23)); Assert.AreEqual(CardColors.Next(first), c23);
            Assert.IsTrue(CardRushCard.TryCardAt(s, G + 46, G, lane, EmptyTrack, out var c46)); Assert.AreEqual(CardColors.Next(CardColors.Next(first)), c46);
            Assert.IsTrue(CardRushCard.TryCardAt(s, G + 69, G, lane, EmptyTrack, out var c69)); Assert.AreEqual(first, c69, "세 장이면 한 바퀴");
        }

        [Test]
        public void 쓰면_20틱_뒷면_뒤_새_첫_장()
        {
            var s = new CardRushRunnerState();
            const int lane = 2;
            const long use = G + 30;
            for (long t = G; t <= use; t++)
                CardRushRunnerSim.Step(ref s, t, t == use, G, lane, EmptyTrack, CardRushTestTuning.Default);
            Assert.AreEqual(1, s.UseCount);
            Assert.AreEqual(use + CardRushRules.CooldownTicks, s.SpinStartTick);
            Assert.IsFalse(CardRushCard.TryCardAt(s, use + 1, G, lane, EmptyTrack, out _), "쓴 다음 틱부터 뒷면");
            Assert.IsFalse(CardRushCard.TryCardAt(s, use + 19, G, lane, EmptyTrack, out _));
            Assert.IsFalse(CardRushCard.CanUse(s, use + 19, G));
            Assert.IsTrue(CardRushCard.TryCardAt(s, use + 20, G, lane, EmptyTrack, out var c));
            Assert.AreEqual(EmptyTrack.FirstCard(lane, 1), c, "새 첫 장 = (레인, 쓴 횟수 1)");
            Assert.IsTrue(CardRushCard.CanUse(s, use + 20, G));
        }

        [Test]
        public void 기절_중엔_색은_보이지만_못_쓴다()
        {
            var s = new CardRushRunnerState { StunEndTick = 500 };
            Assert.IsTrue(CardRushCard.TryCardAt(s, 450, G, 0, EmptyTrack, out _), "기절 중에도 카드는 돈다");
            Assert.IsFalse(CardRushCard.CanUse(s, 450, G));
            Assert.IsFalse(CardRushCard.CanUse(s, 499, G));
            Assert.IsTrue(CardRushCard.CanUse(s, 500, G));

            //  한 틱으로도: 기절 중 탭은 아무것도 안 쓴다.
            CardRushRunnerSim.Step(ref s, 450, true, G, 0, EmptyTrack, CardRushTestTuning.Default);
            Assert.AreEqual(0, s.UseCount);
        }

        [Test]
        public void 틱_0과_출발_전엔_못_쓴다()
        {
            var s = new CardRushRunnerState();
            Assert.IsFalse(CardRushCard.CanUse(s, 0, 0), "틱 0은 '없음'");
            Assert.IsFalse(CardRushCard.CanUse(s, G - 1, G));
            Assert.IsTrue(CardRushCard.CanUse(s, G, G));
            Assert.IsFalse(CardRushCard.CanUse(new CardRushRunnerState { FinishTick = 300 }, 400, G), "완주 뒤엔 못 쓴다");
        }
    }
}
