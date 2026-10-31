using NUnit.Framework;

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
    }
}
