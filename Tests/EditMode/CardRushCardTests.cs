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
        public void 평소엔_뒷면_탭해야_돈다()
        {
            var s = new CardRushRunnerState();
            Assert.IsFalse(CardRushCard.CardAt(s, G, G, 0, EmptyTrack, out _), "탭 전엔 멈춘 뒷면");
            Assert.IsFalse(CardRushCard.CardAt(s, G + 500, G, 0, EmptyTrack, out _), "아무리 지나도 안 돈다");
            Assert.IsFalse(CardRushCard.CardAt(s, G, G, 0, null, out _), "코스가 없으면 뒷면");
            Assert.IsFalse(CardRushCard.CanStart(s, G - 1, G), "출발 전엔 못 시작");
            Assert.IsTrue(CardRushCard.CanStart(s, G, G));
            Assert.IsFalse(CardRushCard.CanLock(s, G + 5, G), "돌지 않으면 못 잡는다");
            Step(ref s, G + 5, true);
            Assert.AreEqual(G + 5, s.RollStartTick);
            Assert.AreEqual(0, s.LockTick, "시작한 틱엔 잡지 않는다");
            Assert.IsTrue(CardRushCard.CardAt(s, G + 5, G, 0, EmptyTrack, out _));
            Assert.IsFalse(CardRushCard.CardAt(s, G + 4, G, 0, EmptyTrack, out _), "시작 전 틱을 물으면 뒷면(첫 장이 미리 보이지 않는다)");
        }

        [Test]
        public void 시작한_틱부터_첫_장_23틱마다_다음_장()
        {
            var s = new CardRushRunnerState();
            const int lane = 3;
            const long start = G + 40;
            Step(ref s, start, true);
            var first = EmptyTrack.FirstCard(lane, 0);
            Assert.AreEqual(first, ColorAt(s, start, lane));
            Assert.AreEqual(first, ColorAt(s, start + 22, lane));
            //  4.2차: 다음 장은 무작위(바로 앞 장과는 다른 색) — 코스의 줄(CardOf)을 그대로 따른다.
            Assert.AreEqual(EmptyTrack.CardOf(lane, 0, 1), ColorAt(s, start + 23, lane));
            Assert.AreNotEqual(first, ColorAt(s, start + 23, lane), "넘기면 색이 바뀐다");
            Assert.AreEqual(EmptyTrack.CardOf(lane, 0, 2), ColorAt(s, start + 46, lane));
            Assert.AreEqual(EmptyTrack.CardOf(lane, 0, 3), ColorAt(s, start + 69, lane));
            Assert.AreEqual(CardRushCard.SpinColor(s, start + 46, G, lane, EmptyTrack), ColorAt(s, start + 46, lane));
        }

        //  시작 → (몇 틱 돌림) → 잡기까지 해 둔다. 잡은 틱을 돌려준다.
        private static long StartAndLock(ref CardRushRunnerState s, long start, long grab, int lane = 0)
        {
            for (long t = start; t <= grab; t++) Step(ref s, t, t == start || t == grab, lane);
            return grab;
        }

        [Test]
        public void 다음_탭에_잡고_색이_멈춘다()
        {
            var s = new CardRushRunnerState();
            const long start = G + 2;
            const long grab = G + 30;
            StartAndLock(ref s, start, grab);
            var held = CardRushCard.SpinColor(s, grab, G, 0, EmptyTrack);
            Assert.AreEqual(grab, s.LockTick);
            Assert.AreEqual(0, s.UseCount, "잡기는 쓰기가 아니다");
            Assert.AreEqual(start, s.RollStartTick, "잡아도 돌기 시작한 틱은 남는다(색 기준)");
            for (long t = grab; t < grab + 200; t += 7)
                Assert.AreEqual(held, ColorAt(s, t), $"tick {t} — 잡은 카드는 돌지 않는다");
            Assert.AreNotEqual(held, CardRushCard.SpinColor(s, grab + CardRushRules.CardDwellTicks, G, 0, EmptyTrack), "안 잡았으면 돌았을 것");
            Assert.AreEqual(CardRushCard.SpinColor(s, start + (grab - start), G, 0, EmptyTrack), held, "색은 시작 틱 기준으로 넘긴 장");
            Assert.AreNotEqual(EmptyTrack.FirstCard(0, 0), held, "한 장 넘긴 뒤 잡았으니 첫 장이 아니다");
        }

        [Test]
        public void 시작한_틱엔_못_잡고_잡은_틱엔_못_쓴다_또_다음_탭에_쓴다()
        {
            var s = new CardRushRunnerState();
            Step(ref s, G, true);
            Assert.IsFalse(CardRushCard.CanLock(s, G, G), "시작한 틱엔 시작 하나뿐");
            Step(ref s, G + 1, true);
            Assert.AreEqual(G + 1, s.LockTick);
            Assert.IsFalse(CardRushCard.CanUse(s, G + 1, G, 0, EmptyTrack), "잡은 틱엔 잡기 하나뿐");
            Assert.IsFalse(CardRushCard.CanLock(s, G + 2, G), "이미 잡았다");
            Assert.IsFalse(CardRushCard.CanStart(s, G + 2, G), "잡고 있으면 새로 못 시작");
            Assert.IsTrue(CardRushCard.CanUse(s, G + 2, G, 0, EmptyTrack));

            Step(ref s, G + 2, true);
            Assert.AreEqual(0, s.LockTick);
            Assert.AreEqual(0, s.RollStartTick, "쓰면 평소로");
            Assert.AreEqual(1, s.UseCount);
            Assert.AreEqual(G + 2 + CardRushRules.CooldownTicks, s.SpinStartTick);
        }

        [Test]
        public void 쓰면_20틱_쿨다운_뒤_탭하면_새_첫_장부터_돈다()
        {
            var s = new CardRushRunnerState();
            const int lane = 2;
            const long use = G + 31;
            for (long t = G; t <= use; t++) Step(ref s, t, t == G || t == use - 1 || t == use, lane);
            Assert.AreEqual(1, s.UseCount);
            Assert.AreEqual(use + CardRushRules.CooldownTicks, s.SpinStartTick);
            Assert.IsFalse(CardRushCard.CardAt(s, use + 1, G, lane, EmptyTrack, out _), "쓴 다음 틱부터 뒷면");
            Assert.IsFalse(CardRushCard.CardAt(s, use + 60, G, lane, EmptyTrack, out _), "쿨다운이 끝나도 탭 전엔 뒷면");
            Assert.IsFalse(CardRushCard.CanStart(s, use + 19, G));
            Assert.IsTrue(CardRushCard.CanStart(s, use + 20, G));
            Step(ref s, use + 19, true);
            Assert.AreEqual(0, s.RollStartTick, "쿨다운 중 시작 탭은 버린다");
            long again = use + 30;
            Step(ref s, again, true);
            Assert.AreEqual(again, s.RollStartTick);
            Assert.AreEqual(EmptyTrack.FirstCard(lane, 1), ColorAt(s, again, lane), "새 첫 장 = (레인, 쓴 횟수 1), 시작 틱 기준");
        }

        [Test]
        public void 잡고_250틱에_사라진다_쿨다운_뒤_평소()
        {
            var s = new CardRushRunnerState();
            const long grab = G + 5;
            StartAndLock(ref s, G, grab);
            long expire = grab + CardRushRules.LockHoldTicks;
            for (long t = grab + 1; t < expire; t++) Step(ref s, t, false);
            Assert.AreEqual(grab, s.LockTick, "249틱째까지는 남는다");
            Assert.IsFalse(CardRushCard.LockExpired(s, expire - 1));
            Assert.IsTrue(CardRushCard.LockExpired(s, expire));

            Step(ref s, expire, false);
            Assert.AreEqual(0, s.LockTick);
            Assert.AreEqual(0, s.RollStartTick);
            Assert.AreEqual(expire, s.ExpireTick);
            Assert.AreEqual(1, s.UseCount, "사라져도 다음 첫 장으로 넘어간다");
            Assert.AreEqual(expire + CardRushRules.CooldownTicks, s.SpinStartTick);
            Assert.IsFalse(CardRushCard.CardAt(s, expire + 1, G, 0, EmptyTrack, out _), "쿨다운 동안 뒷면");
            Assert.AreEqual(0, s.ShotTick + s.JumpStartTick + s.DashStartTick, "사라진 카드는 아무것도 안 했다");
        }

        [Test]
        public void 사라지는_틱의_탭은_시작도_잡기도_쓰기도_아니다()
        {
            var s = new CardRushRunnerState();
            StartAndLock(ref s, G, G + 1);
            for (long t = G + 2; t < G + 1 + CardRushRules.LockHoldTicks; t++) Step(ref s, t, false);
            long expire = G + 1 + CardRushRules.LockHoldTicks;
            Step(ref s, expire, true);
            Assert.AreEqual(0, s.LockTick);
            Assert.AreEqual(0, s.RollStartTick, "쿨다운 중이라 시작도 안 된다");
            Assert.AreEqual(expire, s.ExpireTick);
            Assert.AreEqual(1, s.UseCount, "사라짐 한 번만 — 쓰기로 또 세지 않는다");
            Assert.AreEqual(0, s.ShotTick + s.JumpStartTick + s.DashStartTick);
        }

        [Test]
        public void 안_고르면_300틱에_자동으로_잡힌다()
        {
            var s = new CardRushRunnerState();
            const long start = G + 3;
            Step(ref s, start, true);
            long auto = start + CardRushRules.RollMaxTicks;
            for (long t = start + 1; t < auto; t++) Step(ref s, t, false);
            Assert.AreEqual(0, s.LockTick, "299틱째까지는 안 잡힌다");
            Assert.IsFalse(CardRushCard.RollExpired(s, auto - 1));
            Assert.IsTrue(CardRushCard.RollExpired(s, auto));

            var expected = CardRushCard.SpinColor(s, auto, G, 0, EmptyTrack);
            Step(ref s, auto, false);
            Assert.AreEqual(auto, s.LockTick);
            Assert.AreEqual(0, s.UseCount, "자동 잡기는 쓰기가 아니다");
            Assert.AreEqual(expected, ColorAt(s, auto + 50), "그 틱에 도는 색을 잡는다");
        }

        [Test]
        public void 자동_잡기_틱의_탭은_쓰기가_안_된다()
        {
            var s = new CardRushRunnerState();
            const long start = G;
            Step(ref s, start, true);
            long auto = start + CardRushRules.RollMaxTicks;
            for (long t = start + 1; t < auto; t++) Step(ref s, t, false);
            Step(ref s, auto, true);
            Assert.AreEqual(auto, s.LockTick, "잡혔고 쓰지는 않았다");
            Assert.AreEqual(0, s.UseCount);
            Step(ref s, auto + 1, true);
            Assert.AreEqual(0, s.LockTick, "다음 탭에 쓴다");
            Assert.AreEqual(1, s.UseCount);
        }

        [Test]
        public void 기절_중엔_시작도_잡기도_못_한다_자동_잡기와_사라짐은_된다()
        {
            var s = new CardRushRunnerState { StunEndTick = 500 };
            Assert.IsFalse(CardRushCard.CardAt(s, 450, G, 0, EmptyTrack, out _), "기절 중이어도 평소는 뒷면");
            Assert.IsFalse(CardRushCard.CanStart(s, 450, G));
            Assert.IsFalse(CardRushCard.CanStart(s, 499, G));
            Assert.IsTrue(CardRushCard.CanStart(s, 500, G));
            Step(ref s, 450, true);
            Assert.AreEqual(0, s.RollStartTick, "기절 중 시작 탭은 버린다");

            var rolling = new CardRushRunnerState { RollStartTick = 300, StunEndTick = 700 };
            Assert.IsTrue(CardRushCard.CardAt(rolling, 450, G, 0, EmptyTrack, out _), "기절 중에도 돈다");
            Assert.IsFalse(CardRushCard.CanLock(rolling, 450, G), "기절 중 잡기 탭은 버린다");
            Step(ref rolling, 450, true);
            Assert.AreEqual(0, rolling.LockTick);
            Step(ref rolling, 300 + CardRushRules.RollMaxTicks, false);
            Assert.AreEqual(300 + CardRushRules.RollMaxTicks, rolling.LockTick, "6초는 기절 중에도 흐른다");

            var held = new CardRushRunnerState { LockTick = 400, RollStartTick = 300, SpinStartTick = 300, StunEndTick = 700 };
            Assert.IsFalse(CardRushCard.CanUse(held, 450, G, 0, EmptyTrack), "기절 중엔 못 쓴다");
            Assert.IsTrue(CardRushCard.LockExpired(held, 400 + CardRushRules.LockHoldTicks), "5초는 기절 중에도 흐른다");
            Step(ref held, 400 + CardRushRules.LockHoldTicks, true);
            Assert.AreEqual(0, held.LockTick);
            Assert.AreEqual(400 + CardRushRules.LockHoldTicks, held.ExpireTick);
        }

        [Test]
        public void 공중에서도_시작하고_잡는다()
        {
            var s = new CardRushRunnerState { JumpStartTick = G + 5, PosMm = 1000 };
            Assert.IsTrue(CardRushCard.CanStart(s, G + 10, G));
            Step(ref s, G + 10, true);
            Assert.AreEqual(G + 10, s.RollStartTick);
            Step(ref s, G + 11, true);
            Assert.AreEqual(G + 11, s.LockTick, "공중에서도 잡는다(쓰기만 색에 따라 막힌다)");
        }

        [Test]
        public void 틱_0과_출발_전_완주_뒤엔_못_잡는다()
        {
            var s = new CardRushRunnerState();
            Assert.IsFalse(CardRushCard.CanStart(s, 0, 0), "틱 0은 '없음'");
            Assert.IsFalse(CardRushCard.CanStart(s, G - 1, G));
            Assert.IsTrue(CardRushCard.CanStart(s, G, G));
            Assert.IsFalse(CardRushCard.CanStart(new CardRushRunnerState { FinishTick = 300 }, 400, G), "완주 뒤엔 못 시작한다");
            Assert.IsFalse(CardRushCard.CanLock(new CardRushRunnerState { FinishTick = 300, RollStartTick = 250 }, 400, G), "완주 뒤엔 못 잡는다");
            Assert.IsFalse(CardRushCard.CanUse(new CardRushRunnerState { FinishTick = 300, LockTick = 290 }, 400, G, 0, EmptyTrack), "완주 뒤엔 못 쓴다");
            Assert.IsFalse(CardRushCard.CanUse(s, G + 5, G, 0, EmptyTrack), "잡지 않으면 쓸 것이 없다");
        }
    }
}
