using System;
using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;

namespace LOP.TriGrab.Tests
{
    public class TriGrabCardSimTests
    {
        static readonly TriGrabMole[] NoMoles = new TriGrabMole[0];

        static List<TriGrabCard> Scatter(Random rng, int n)
        {
            var list = new List<TriGrabCard>();
            for (int i = 0; i < n; i++)
            {
                Assert.IsTrue(TriGrabCardSim.TryFreeSpot(list, rng, out var p));
                float a = (float)(rng.NextDouble() * Math.PI * 2);
                list.Add(new TriGrabCard
                {
                    Id = i, Pos = p,
                    Vel = new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * TriGrabConfig.CardSpeed,
                });
            }
            return list;
        }

        [Test]
        public void NoOverlap_NoEscape_1000Ticks()
        {
            var rng = new Random(11);
            var cards = Scatter(rng, 8);
            float minDist = 2 * TriGrabConfig.CardBumpRadius - 0.01f;
            float worstPair = float.MaxValue, worstR = 0f;
            for (int t = 0; t < 1000; t++)
            {
                TriGrabCardSim.Step(cards, NoMoles, t, TriGrabConfig.TickSeconds, rng);
                for (int i = 0; i < cards.Count; i++)
                {
                    worstR = Math.Max(worstR, cards[i].Pos.Length());
                    for (int j = i + 1; j < cards.Count; j++)
                        worstPair = Math.Min(worstPair, Vector2.Distance(cards[i].Pos, cards[j].Pos));
                }
            }
            Assert.GreaterOrEqual(worstPair, minDist);
            Assert.LessOrEqual(worstR, TriGrabConfig.CardWallRadius + 0.001f);
        }

        [Test]
        public void FiveCardsBurstFromCenter_NoOverlap_NoEscape_1000Ticks()
        {
            var rng = new Random(3);
            int n = TriGrabConfig.CardsOnDisc;
            var cards = new List<TriGrabCard>();
            float ringR = (TriGrabConfig.CardBumpRadius * 2f + 0.02f) / (2f * (float)Math.Sin(Math.PI / n));
            for (int i = 0; i < n; i++)
            {
                float a = i * (float)(Math.PI * 2 / n);
                var pos = new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * ringR;
                cards.Add(new TriGrabCard { Id = i, Pos = pos, Vel = TriGrabCardSim.OutwardDirection(pos, rng) * TriGrabConfig.CardBurstSpeed });
            }
            float minDist = 2 * TriGrabConfig.CardBumpRadius - 0.01f, worstPair = float.MaxValue, worstR = 0f;
            for (int t = 0; t < 1000; t++)
            {
                TriGrabCardSim.Step(cards, NoMoles, t, TriGrabConfig.TickSeconds, rng);
                for (int i = 0; i < n; i++)
                {
                    worstR = Math.Max(worstR, cards[i].Pos.Length());
                    for (int j = i + 1; j < n; j++) worstPair = Math.Min(worstPair, Vector2.Distance(cards[i].Pos, cards[j].Pos));
                }
            }
            Assert.GreaterOrEqual(worstPair, minDist);
            Assert.LessOrEqual(worstR, TriGrabConfig.CardWallRadius + 0.001f);
        }

        [Test]
        public void CenterSpot_KeepsClearOfStandingMole()
        {
            var mole = new TriGrabMole { Pos = new Vector2(0.2f, 0f), SpawnTick = 0, StandTicks = 1000 };
            Assert.IsTrue(TriGrabCardSim.TryCenterSpot(new List<TriGrabCard>(), new[] { mole }, 50, new Random(1), out var p));
            float need = TriGrabConfig.CardBumpRadius + TriGrabConfig.MoleRadius;
            Assert.GreaterOrEqual(Vector2.Distance(p, mole.Pos), need, "두더지 위에 태어나지 않는다");
            var rising = new TriGrabMole { Pos = new Vector2(0f, 0f), SpawnTick = 48, StandTicks = 1000 };
            Assert.IsTrue(TriGrabCardSim.TryCenterSpot(new List<TriGrabCard>(), new[] { rising }, 50, new Random(1), out var q));
            Assert.GreaterOrEqual(Vector2.Distance(q, rising.Pos), need, "솟는 중인 두더지도 피한다");
        }

        [Test]
        public void SpeedRelaxesTo1_4()
        {
            var c = new TriGrabCard { Pos = Vector2.Zero, Vel = new Vector2(0.2f, 0f) };
            var list = new List<TriGrabCard> { c };
            var rng = new Random(1);
            for (int t = 0; t < 150; t++) TriGrabCardSim.Step(list, NoMoles, t, TriGrabConfig.TickSeconds, rng);
            Assert.AreEqual(1.4f, c.Vel.Length(), 0.1f);
        }

        [Test]
        public void SameSeed_SameTrajectory()
        {
            Vector2[] Run()
            {
                var rng = new Random(5);
                var cards = Scatter(rng, 6);
                for (int t = 0; t < 300; t++) TriGrabCardSim.Step(cards, NoMoles, t, TriGrabConfig.TickSeconds, rng);
                var r = new Vector2[cards.Count];
                for (int i = 0; i < r.Length; i++) r[i] = cards[i].Pos;
                return r;
            }
            var a = Run();
            var b = Run();
            Assert.AreEqual(a.Length, b.Length);
            for (int i = 0; i < a.Length; i++) Assert.AreEqual(a[i], b[i]);
        }

        [Test]
        public void HeadOnSwap()
        {
            var a = new TriGrabCard { Pos = new Vector2(-0.65f, 0f), Vel = new Vector2(1.4f, 0f) };
            var b = new TriGrabCard { Pos = new Vector2(0.65f, 0f), Vel = new Vector2(-1.4f, 0f) };
            var list = new List<TriGrabCard> { a, b };
            TriGrabCardSim.Step(list, NoMoles, 7, TriGrabConfig.TickSeconds, new Random(1));
            Assert.AreEqual(-1.4f, a.Vel.X, 0.05f);
            Assert.AreEqual(1.4f, b.Vel.X, 0.05f);
            Assert.AreEqual(7, a.BumpTick);
            Assert.AreEqual(7, b.BumpTick);
        }

        [Test]
        public void StandingMoleBouncesCard()
        {
            var mole = new TriGrabMole { Pos = Vector2.Zero, SpawnTick = 0, StandTicks = 100 };
            var c = new TriGrabCard { Pos = new Vector2(1.2f, 0f), Vel = new Vector2(-1.4f, 0f) };
            var rng = new Random(1);
            for (int t = 20; t < 40; t++)
                TriGrabCardSim.Step(new List<TriGrabCard> { c }, new[] { mole }, t, TriGrabConfig.TickSeconds, rng);
            Assert.Greater(c.Vel.X, 0f);
        }
    }
}
