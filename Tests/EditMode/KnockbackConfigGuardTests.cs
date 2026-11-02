using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LOP.Tests
{
    public class KnockbackConfigGuardTests
    {
        static KnockbackConfig Make(float crateInterval = 20f, float crateWarn = 1.5f, float pickup = 0.9f, float blast = 2.5f)
            => new KnockbackConfig(120f, 6f, 10f, 12f, 2f, 0.5f, 5f, 11f, 30f, 18f, 0.3f, 0.55f, 3, 0.95f, 0.26f, 1.2f, 12, 6f, 0.2f, 0.5f, 0.3f, 0.1f, 0.2f, 0.8f, 8f,
                new List<MountedArcheryZoneStageConfig>(), null, 0, 0f, 1f, 20f, crateInterval, crateWarn, pickup, blast);

        [Test]
        public void 정상_값은_그대로_쓰고_로그가_없다()
        {
            var c = Make(10f, 2f, 1.2f, 3f);
            Assert.AreEqual(10f, c.CrateIntervalSeconds);
            Assert.AreEqual(2f, c.CrateWarnSeconds);
            Assert.AreEqual(1.2f, c.CratePickupRadius);
            Assert.AreEqual(3f, c.BlastRadius);
        }

        [Test]
        public void 줍기_반경이_0_이하면_기본값과_오류_로그()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("crate_pickup_radius"));
            Assert.AreEqual(0.9f, Make(pickup: 0f).CratePickupRadius);
        }

        [Test]
        public void 폭발_반경이_음수면_기본값과_오류_로그()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("blast_radius"));
            Assert.AreEqual(2.5f, Make(blast: -1f).BlastRadius);
        }

        [Test]
        public void 간격이_0_이하면_기본값과_오류_로그()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("crate_interval_seconds"));
            Assert.AreEqual(20f, Make(crateInterval: 0f).CrateIntervalSeconds);
        }

        [Test]
        public void 예고가_0_이하면_기본값과_오류_로그()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("crate_warn_seconds"));
            Assert.AreEqual(1.5f, Make(crateWarn: -0.5f).CrateWarnSeconds);
        }
    }
}
