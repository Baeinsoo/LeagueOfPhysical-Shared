using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;

namespace LOP.TriGrab.Tests
{
    public class TriGrabHookTests
    {
        const float Dt = TriGrabConfig.TickSeconds;

        // yaw 90도 = +x 방향
        static TriGrabHook FireRight() => TriGrabHook.Fire("a", Vector2.Zero, 90f, 10);

        static TriGrabCard Card(int id, float x, float y = 0f) =>
            new TriGrabCard { Id = id, Pos = new Vector2(x, y), BornTick = 0 };

        static TriGrabMole Mole(int id, float x, long spawn = 0) =>
            new TriGrabMole { Id = id, Pos = new Vector2(x, 0), SpawnTick = spawn, StandTicks = 1000 };

        static void RunOut(TriGrabHook h, IList<TriGrabCard> cards, IList<TriGrabMole> moles, float dt, long tick = 500)
        {
            for (int i = 0; i < 400 && h.State == TriGrabHookState.Out; i++)
                TriGrabHook.StepOut(h, cards, moles, tick, dt);
        }

        [Test]
        public void Fire_StartsAtBodyCenterAlongYaw()
        {
            var h = TriGrabHook.Fire("a", new Vector2(1, 2), 90f, 7);
            Assert.AreEqual("a", h.Owner);
            Assert.AreEqual(7, h.FireTick);
            Assert.AreEqual(1f, h.Dir.X, 1e-4f);
            Assert.AreEqual(0f, h.Dir.Y, 1e-4f);
            Assert.AreEqual(0f, h.Length);
            Assert.AreEqual(1f, h.Tip.X, 1e-4f);
            Assert.AreEqual(2f, h.Tip.Y, 1e-4f);
            Assert.AreEqual(TriGrabHookState.Out, h.State);
            Assert.AreEqual(TriGrabHookEnd.None, h.End);
            Assert.AreEqual(-1, h.CardId);
            Assert.AreEqual(-1, h.MoleId);
        }

        [Test]
        public void Math_YawRoundTrip()
        {
            var d = TriGrabMath.DirFromYaw(0f);
            Assert.AreEqual(0f, d.X, 1e-4f);
            Assert.AreEqual(1f, d.Y, 1e-4f);
            Assert.AreEqual(30f, TriGrabMath.YawFromDir(TriGrabMath.DirFromYaw(30f)), 1e-3f);
        }

        [Test]
        public void FirstTouch_FrontCardBlocksBack()
        {
            var front = Card(1, 5f);
            var back = Card(2, 8f);
            var h = FireRight();
            RunOut(h, new List<TriGrabCard> { back, front }, new List<TriGrabMole>(), Dt);
            Assert.AreEqual(TriGrabHookState.Back, h.State);
            Assert.AreEqual(TriGrabHookEnd.Card, h.End);
            Assert.AreEqual(1, h.CardId);
            Assert.AreEqual("a", front.CarriedBy);
            Assert.IsNull(back.CarriedBy);
            Assert.AreEqual(h.Length, h.TurnLength, 1e-5f);
        }

        [Test]
        public void StandingMoleBlocks_HiddenMolePasses()
        {
            var standing = Mole(1, 4f);
            var h = FireRight();
            RunOut(h, new List<TriGrabCard> { Card(9, 8f) }, new List<TriGrabMole> { standing }, Dt);
            Assert.AreEqual(TriGrabHookEnd.Mole, h.End);
            Assert.AreEqual(1, h.MoleId);
            Assert.AreEqual(500, standing.BonkTick);
            Assert.AreEqual(TriGrabHookState.Back, h.State);

            var hidden = Mole(2, 4f, spawn: 1000); // 아직 안 솟았다
            var card = Card(9, 8f);
            var h2 = FireRight();
            RunOut(h2, new List<TriGrabCard> { card }, new List<TriGrabMole> { hidden }, Dt);
            Assert.AreEqual(TriGrabHookEnd.Card, h2.End);
            Assert.AreEqual(-1, hidden.BonkTick);
        }

        [Test]
        public void Range_EndsEmpty()
        {
            var h = FireRight();
            RunOut(h, new List<TriGrabCard>(), new List<TriGrabMole>(), Dt);
            Assert.AreEqual(TriGrabConfig.HookRange, h.Length, 1e-4f);
            Assert.AreEqual(TriGrabHookEnd.Range, h.End);
            Assert.AreEqual(TriGrabHookState.Back, h.State);
        }

        [Test]
        public void FastHook_DoesNotTunnel()
        {
            var c = Card(1, 6f);
            var h = FireRight();
            RunOut(h, new List<TriGrabCard> { c }, new List<TriGrabMole>(), 0.1f);
            Assert.AreEqual(TriGrabHookEnd.Card, h.End);
            Assert.AreEqual("a", c.CarriedBy);
        }

        [Test]
        public void Hook_IgnoresCarriedCard()
        {
            // 호출 쪽이 끌려가는 카드를 onDisc에서 빼 준다는 계약.
            var carried = Card(1, 5f);
            carried.CarriedBy = "b";
            var all = new List<TriGrabCard> { carried };
            var onDisc = all.FindAll(x => x.CarriedBy == null);
            var h = FireRight();
            RunOut(h, onDisc, new List<TriGrabMole>(), Dt);
            Assert.AreEqual(TriGrabHookEnd.Range, h.End);
            Assert.AreEqual("b", carried.CarriedBy);
        }

        [Test]
        public void HalfPoppedCard_NotGrabbed()
        {
            var c = Card(1, 5f);
            c.BornTick = 500; // 막 생김: PopK 0
            var h = FireRight();
            RunOut(h, new List<TriGrabCard> { c }, new List<TriGrabMole>(), Dt, tick: 500);
            Assert.AreEqual(TriGrabHookEnd.Range, h.End);
            Assert.IsNull(c.CarriedBy);

            var ok = Card(2, 5f);
            ok.BornTick = 500 - TriGrabConfig.PopTicks;
            var h2 = FireRight();
            RunOut(h2, new List<TriGrabCard> { ok }, new List<TriGrabMole>(), Dt, tick: 500);
            Assert.AreEqual(TriGrabHookEnd.Card, h2.End);
        }

        [Test]
        public void PopK_Curve()
        {
            var c = new TriGrabCard { BornTick = 100 };
            Assert.AreEqual(0f, TriGrabCards.PopK(c, 100), 1e-5f);
            Assert.AreEqual(1f, TriGrabCards.PopK(c, 100 + TriGrabConfig.PopTicks), 1e-5f);
            Assert.AreEqual(1f, TriGrabCards.PopK(c, 5000), 1e-5f);
            Assert.Greater(TriGrabCards.PopK(c, 100 + TriGrabConfig.PopTicks / 2), 0.5f);
        }

        [Test]
        public void Back_CarriesCardToBody()
        {
            var c = Card(1, 5f);
            var cards = new Dictionary<int, TriGrabCard> { { 1, c } };
            var h = FireRight();
            RunOut(h, new List<TriGrabCard> { c }, new List<TriGrabMole>(), Dt);
            Assert.AreEqual(TriGrabHookState.Back, h.State);

            float lastLen = h.Length;
            bool done = false;
            for (int i = 0; i < 400 && !done; i++)
            {
                done = TriGrabHook.StepBack(h, Vector2.Zero, cards, Dt);
                Assert.LessOrEqual(h.Length, lastLen);
                lastLen = h.Length;
                if (!done) Assert.AreEqual(h.Tip.X, c.Pos.X, 1e-4f);
            }
            Assert.IsTrue(done);
            Assert.AreEqual(TriGrabHookState.Done, h.State);
            Assert.LessOrEqual(h.Length, TriGrabConfig.HookBackSpeed * Dt + TriGrabConfig.BodyRadius * 0.6f + 1e-4f);
        }

        [Test]
        public void Back_TipContinuous_WhenBodyMoves()
        {
            var c = Card(1, 5f);
            var cards = new Dictionary<int, TriGrabCard> { { 1, c } };
            var h = FireRight();
            RunOut(h, new List<TriGrabCard> { c }, new List<TriGrabMole>(), Dt);
            var origin = h.Origin; var dir = h.Dir;
            float turnLen = h.TurnLength; long turnTick = h.TurnTick; long fireTick = h.FireTick;
            float maxStep = TriGrabConfig.HookBackSpeed * Dt + 1e-4f;

            var body = Vector2.Zero;
            bool done = false;
            for (int i = 0; i < 400 && !done; i++)
            {
                if (i < 10) body += new Vector2(0f, 0.3f); // 3 m 옆으로
                var before = h.Tip;
                done = TriGrabHook.StepBack(h, body, cards, Dt);
                Assert.LessOrEqual(Vector2.Distance(before, h.Tip), maxStep);
                Assert.AreEqual(h.Tip.X, c.Pos.X, 1e-4f);
                Assert.AreEqual(h.Tip.Y, c.Pos.Y, 1e-4f);
            }
            Assert.IsTrue(done);
            Assert.AreEqual(origin, h.Origin);
            Assert.AreEqual(dir, h.Dir);
            Assert.AreEqual(turnLen, h.TurnLength);
            Assert.AreEqual(turnTick, h.TurnTick);
            Assert.AreEqual(fireTick, h.FireTick);
        }

        [Test]
        public void Back_EmptyHandReturns()
        {
            var h = FireRight();
            RunOut(h, new List<TriGrabCard>(), new List<TriGrabMole>(), Dt);
            bool done = false;
            for (int i = 0; i < 400 && !done; i++)
                done = TriGrabHook.StepBack(h, Vector2.Zero, new Dictionary<int, TriGrabCard>(), Dt);
            Assert.IsTrue(done);
        }
    }
}
