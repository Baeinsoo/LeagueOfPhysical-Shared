using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class KnockbackPillarsTests
    {
        static KnockbackConfig Make(KnockbackPillarLayout pillars = null)
            => new KnockbackConfig(120f, 6f, 10f, 12f, 2f, 0.5f, 5f, 11f, 30f, 18f, 0.3f, 0.55f, 3, 0.95f, 0.26f, 1.2f, 1,
                                   6f, 0.5f, 0.5f, 0.3f, 0.1f, 0.2f, 0.8f, 5f,
                                   new System.Collections.Generic.List<MountedArcheryZoneStageConfig>(), pillars);

        [Test]
        public void 설정을_안_주면_기둥이_없다()
        {
            Assert.AreEqual(0, Make().Pillars.Count);
        }

        [Test]
        public void 기둥_설정을_주면_그대로_읽힌다()
        {
            var c = Make(new KnockbackPillarLayout(4, 7f, 0.9f, 3f));
            Assert.AreEqual(4, c.Pillars.Count);
            Assert.AreEqual(7f, c.Pillars.RingRadius);
            Assert.AreEqual(0.9f, c.Pillars.Radius);
            Assert.AreEqual(3f, c.Pillars.Height);
        }

        static (KnockbackPillars pillars, KnockbackFloor floor) MakePillars(int count = 4)
        {
            var floor = new KnockbackFloor(17f, 1.5f, 0f);
            return (new KnockbackPillars(new KnockbackPillarLayout(count, 7f, 0.9f, 3f), floor), floor);
        }

        [Test]
        public void 기둥은_22도반부터_90도마다_고리_위에_선다()
        {
            var (p, _) = MakePillars();
            Assert.AreEqual(4, p.Count);
            Assert.AreEqual(2.679f, p.Center(0).x, 1e-3f);
            Assert.AreEqual(6.467f, p.Center(0).y, 1e-3f);
            Assert.AreEqual(6.467f, p.Center(1).x, 1e-3f);
            Assert.AreEqual(-2.679f, p.Center(1).y, 1e-3f);
            Assert.AreEqual(0.9f, p.Radius);
            Assert.AreEqual(3f, p.Height);
        }

        [Test]
        public void 기둥이_0개거나_음수면_기둥이_없다()
        {
            Assert.AreEqual(0, MakePillars(0).pillars.Count);
            Assert.AreEqual(0, MakePillars(-3).pillars.Count);
            Assert.AreEqual(0, new KnockbackPillars(KnockbackPillarLayout.None, new KnockbackFloor(17f, 1.5f, 0f)).Count);
        }

        [Test]
        public void 기둥_아래_칸이_무너지면_그_틱부터_쓰러져_있다()
        {
            var (p, floor) = MakePillars();
            floor.SetZoneStages(new[] { new ZoneStage(0, 0, 100, 200, Vector2.zero, floor.ZoneStartRadius, Vector2.zero, 3f, 4f) });
            long collapse = -1;
            for (long t = 100; t <= 200; t++)
            {
                if (!p.IsStanding(0, t)) { collapse = t; break; }
            }
            Assert.Greater(collapse, 100, "축소 도중에 무너져야 한다");
            Assert.IsTrue(p.IsStanding(0, collapse - 1));
            Assert.IsFalse(p.IsStanding(0, collapse));

            var standing = new System.Collections.Generic.List<Vector2> { Vector2.one };
            p.CollectStanding(collapse - 1, standing);
            Assert.AreEqual(4, standing.Count);
            p.CollectStanding(200, standing);
            Assert.AreEqual(0, standing.Count);
        }
    }
}
