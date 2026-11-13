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

        private static void Step(ref CardRushRunnerState s, long tick, bool tap, int lane = 0)
            => CardRushRunnerSim.Step(ref s, tick, tap, G, lane, EmptyTrack, CardRushTestTuning.Default);

        private static CardColor ColorAt(in CardRushRunnerState s, long tick, int lane = 0)
        {
            Assert.IsTrue(CardRushCard.CardAt(s, tick, G, lane, EmptyTrack, out var c), $"tick {tick}에 카드가 보여야 한다");
            return c;
        }

        [Test]
        public void 출발_전엔_뒷면()
        {
            var s = new CardRushRunnerState();
            Assert.IsFalse(CardRushCard.CardAt(s, G - 1, G, 0, EmptyTrack, out _));
            Assert.IsTrue(CardRushCard.CardAt(s, G, G, 0, EmptyTrack, out _));
            Assert.IsFalse(CardRushCard.CardAt(s, 5000, long.MaxValue, 0, EmptyTrack, out _), "출발 틱이 아직 없으면 뒷면");
            Assert.IsFalse(CardRushCard.CardAt(s, G, G, 0, null, out _), "코스가 없으면 뒷면");
        }

        [Test]
        public void 출발_틱에_첫_장_23틱마다_다음_색()
        {
            var s = new CardRushRunnerState();
            const int lane = 3;
            var first = EmptyTrack.FirstCard(lane, 0);
            Assert.AreEqual(first, ColorAt(s, G, lane));
            Assert.AreEqual(first, ColorAt(s, G + 22, lane));
            Assert.AreEqual(CardColors.Next(first), ColorAt(s, G + 23, lane));
            Assert.AreEqual(CardColors.Next(CardColors.Next(first)), ColorAt(s, G + 46, lane));
            Assert.AreEqual(first, ColorAt(s, G + 69, lane), "세 장이면 한 바퀴");
            Assert.AreEqual(CardRushCard.SpinColor(s, G + 46, G, lane, EmptyTrack), ColorAt(s, G + 46, lane));
        }

        [Test]
        public void 탭하면_잡고_색이_멈춘다()
        {
            var s = new CardRushRunnerState();
            const long grab = G + 30;
            for (long t = G; t <= grab; t++) Step(ref s, t, t == grab);
            var held = CardRushCard.SpinColor(s, grab, G, 0, EmptyTrack);
            Assert.AreEqual(grab, s.LockTick);
            Assert.AreEqual(0, s.UseCount, "잡기는 쓰기가 아니다");
            for (long t = grab; t < grab + 200; t += 7)
                Assert.AreEqual(held, ColorAt(s, t), $"tick {t} — 잡은 카드는 돌지 않는다");
            Assert.AreNotEqual(held, CardRushCard.SpinColor(s, grab + CardRushRules.CardDwellTicks, G, 0, EmptyTrack), "안 잡았으면 돌았을 것");
        }

        [Test]
        public void 잡은_틱엔_못_쓴다_다음_탭에_쓴다()
        {
            var s = new CardRushRunnerState();
            Step(ref s, G, true);
            Assert.AreEqual(G, s.LockTick);
            Assert.IsFalse(CardRushCard.CanUse(s, G, G, 0, EmptyTrack), "잡은 틱엔 잡기 하나뿐");
            Assert.IsFalse(CardRushCard.CanLock(s, G + 1, G), "이미 잡았다");
            Assert.IsTrue(CardRushCard.CanUse(s, G + 1, G, 0, EmptyTrack));

            Step(ref s, G + 1, true);
            Assert.AreEqual(0, s.LockTick);
            Assert.AreEqual(1, s.UseCount);
            Assert.AreEqual(G + 1 + CardRushRules.CooldownTicks, s.SpinStartTick);
        }

        [Test]
        public void 쓰면_20틱_뒷면_뒤_새_첫_장()
        {
            var s = new CardRushRunnerState();
            const int lane = 2;
            const long use = G + 31;
            for (long t = G; t <= use; t++) Step(ref s, t, t == use - 1 || t == use, lane);
            Assert.AreEqual(1, s.UseCount);
            Assert.AreEqual(use + CardRushRules.CooldownTicks, s.SpinStartTick);
            Assert.IsFalse(CardRushCard.CardAt(s, use + 1, G, lane, EmptyTrack, out _), "쓴 다음 틱부터 뒷면");
            Assert.IsFalse(CardRushCard.CardAt(s, use + 19, G, lane, EmptyTrack, out _));
            Assert.IsFalse(CardRushCard.CanLock(s, use + 19, G));
            Assert.AreEqual(EmptyTrack.FirstCard(lane, 1), ColorAt(s, use + 20, lane), "새 첫 장 = (레인, 쓴 횟수 1)");
            Assert.IsTrue(CardRushCard.CanLock(s, use + 20, G));
        }

        [Test]
        public void 잡고_250틱에_사라진다_쿨다운_뒤_새_첫_장()
        {
            var s = new CardRushRunnerState();
            const long grab = G + 5;
            for (long t = G; t <= grab; t++) Step(ref s, t, t == grab);
            long expire = grab + CardRushRules.LockHoldTicks;
            for (long t = grab + 1; t < expire; t++) Step(ref s, t, false);
            Assert.AreEqual(grab, s.LockTick, "249틱째까지는 남는다");
            Assert.IsFalse(CardRushCard.LockExpired(s, expire - 1));
            Assert.IsTrue(CardRushCard.LockExpired(s, expire));

            Step(ref s, expire, false);
            Assert.AreEqual(0, s.LockTick);
            Assert.AreEqual(expire, s.ExpireTick);
            Assert.AreEqual(1, s.UseCount, "사라져도 다음 첫 장으로 넘어간다");
            Assert.AreEqual(expire + CardRushRules.CooldownTicks, s.SpinStartTick);
            Assert.IsFalse(CardRushCard.CardAt(s, expire + 1, G, 0, EmptyTrack, out _), "쿨다운 동안 뒷면");
            Assert.AreEqual(EmptyTrack.FirstCard(0, 1), ColorAt(s, expire + CardRushRules.CooldownTicks));
            Assert.AreEqual(0, s.ShotTick + s.JumpStartTick + s.DashStartTick, "사라진 카드는 아무것도 안 했다");
        }

        [Test]
        public void 사라지는_틱의_탭은_잡지도_쓰지도_않는다()
        {
            var s = new CardRushRunnerState();
            Step(ref s, G, true);
            for (long t = G + 1; t < G + CardRushRules.LockHoldTicks; t++) Step(ref s, t, false);
            long expire = G + CardRushRules.LockHoldTicks;
            Step(ref s, expire, true);
            Assert.AreEqual(0, s.LockTick);
            Assert.AreEqual(expire, s.ExpireTick);
            Assert.AreEqual(1, s.UseCount, "사라짐 한 번만 — 쓰기로 또 세지 않는다");
            Assert.AreEqual(0, s.ShotTick + s.JumpStartTick + s.DashStartTick);
        }

        [Test]
        public void 기절_중엔_못_잡고_못_쓴다_잡은_시간은_흐른다()
        {
            var s = new CardRushRunnerState { StunEndTick = 500 };
            Assert.IsTrue(CardRushCard.CardAt(s, 450, G, 0, EmptyTrack, out _), "기절 중에도 카드는 돈다");
            Assert.IsFalse(CardRushCard.CanLock(s, 450, G));
            Assert.IsFalse(CardRushCard.CanLock(s, 499, G));
            Assert.IsTrue(CardRushCard.CanLock(s, 500, G));
            Step(ref s, 450, true);
            Assert.AreEqual(0, s.LockTick, "기절 중 탭은 안 잡는다");

            var held = new CardRushRunnerState { LockTick = 400, SpinStartTick = 300, StunEndTick = 700 };
            Assert.IsFalse(CardRushCard.CanUse(held, 450, G, 0, EmptyTrack), "기절 중엔 못 쓴다");
            Assert.IsTrue(CardRushCard.LockExpired(held, 400 + CardRushRules.LockHoldTicks), "5초는 기절 중에도 흐른다");
            Step(ref held, 400 + CardRushRules.LockHoldTicks, true);
            Assert.AreEqual(0, held.LockTick);
            Assert.AreEqual(400 + CardRushRules.LockHoldTicks, held.ExpireTick);
        }

        [Test]
        public void 틱_0과_출발_전_완주_뒤엔_못_잡는다()
        {
            var s = new CardRushRunnerState();
            Assert.IsFalse(CardRushCard.CanLock(s, 0, 0), "틱 0은 '없음'");
            Assert.IsFalse(CardRushCard.CanLock(s, G - 1, G));
            Assert.IsTrue(CardRushCard.CanLock(s, G, G));
            Assert.IsFalse(CardRushCard.CanLock(new CardRushRunnerState { FinishTick = 300 }, 400, G), "완주 뒤엔 못 잡는다");
            Assert.IsFalse(CardRushCard.CanUse(new CardRushRunnerState { FinishTick = 300, LockTick = 290 }, 400, G, 0, EmptyTrack), "완주 뒤엔 못 쓴다");
            Assert.IsFalse(CardRushCard.CanUse(s, G + 5, G, 0, EmptyTrack), "잡지 않으면 쓸 것이 없다");
        }
    }
}
