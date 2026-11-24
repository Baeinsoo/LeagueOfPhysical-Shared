using System;
using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;

namespace LOP.TriGrab.Tests
{
    public class TriGrabMolesTests
    {
        static TriGrabMole Mole(long spawn = 100, int stand = 60) =>
            new TriGrabMole { SpawnTick = spawn, StandTicks = stand };

        [Test]
        public void Height_RiseStandSink()
        {
            var m = Mole();
            Assert.AreEqual(0f, m.HeightAt(100), 1e-4f);
            Assert.AreEqual(0.5f, m.HeightAt(104), 1e-4f);
            Assert.AreEqual(1f, m.HeightAt(108), 1e-4f);
            Assert.AreEqual(1f, m.HeightAt(159), 1e-4f);
            Assert.AreEqual(1f, m.HeightAt(160), 1e-4f); // 서 있음의 끝에서 막 내려가기 시작
            Assert.AreEqual(0.5f, m.HeightAt(165), 1e-4f);
            Assert.AreEqual(0f, m.HeightAt(170), 1e-4f);
            Assert.IsFalse(m.GoneAt(100));
            Assert.IsFalse(m.GoneAt(169));
            Assert.IsTrue(m.GoneAt(170));
        }

        [Test]
        public void Bonk_ShortensStand()
        {
            var m = Mole(100, 100);
            m.BonkTick = 120;
            Assert.AreEqual(1f, m.HeightAt(142), 1e-4f);
            Assert.IsFalse(m.GoneAt(152));
            Assert.IsTrue(m.GoneAt(120 + 23 + 10));
            Assert.Less(120 + 23 + 10, 100 + 100 + 10);
        }

        [Test]
        public void Blocks_OnlyWhenStandingAndNotBonked()
        {
            var m = Mole();
            Assert.IsFalse(m.Blocks(102));
            Assert.IsTrue(m.Blocks(106));
            Assert.IsTrue(m.Blocks(130));
            Assert.IsFalse(m.Blocks(166));
            m.BonkTick = 130;
            Assert.IsFalse(m.Blocks(131));
        }

        [Test]
        public void Spawner_MaxTwo_AwayFromCards()
        {
            var rng = new Random(7);
            var sp = new TriGrabMoleSpawner();
            var moles = new List<TriGrabMole>();
            var cards = new List<TriGrabCard>();
            for (int i = 0; i < 7; i++)
                cards.Add(new TriGrabCard { Pos = new Vector2((float)(rng.NextDouble() * 6 - 3), (float)(rng.NextDouble() * 6 - 3)) });
            int spawned = 0;
            for (long t = 0; t < 10000; t++)
            {
                int before = moles.Count;
                var known = new HashSet<int>();
                foreach (var m in moles) known.Add(m.Id);
                sp.Step(moles, cards, t, rng);
                Assert.LessOrEqual(moles.Count, 2);
                foreach (var m in moles)
                {
                    if (known.Contains(m.Id)) continue;
                    spawned++;
                    float r = m.Pos.Length();
                    Assert.GreaterOrEqual(r, 1.0f - 1e-4f);
                    Assert.LessOrEqual(r, TriGrabConfig.CardWallRadius - 0.4f + 1e-4f);
                    foreach (var c in cards)
                        Assert.Greater(Vector2.Distance(c.Pos, m.Pos), 0.64f + 0.42f + 0.1f);
                    foreach (var o in moles)
                        if (o != m) Assert.Greater(Vector2.Distance(o.Pos, m.Pos), 1.4f);
                }
            }
            Assert.Greater(spawned, 20);
        }
    }
}
