using NUnit.Framework;

namespace LOP.Tests
{
    public class MountedArcheryLagCompensationTests
    {
        [Test]
        public void 되감는_폭은_상한에서_잘린다()
        {
            Assert.AreEqual(25, MountedArcheryLagCompensation.RewindTicks(100, 40, 25));
            Assert.AreEqual(10, MountedArcheryLagCompensation.RewindTicks(100, 90, 25));
            Assert.AreEqual(0, MountedArcheryLagCompensation.RewindTicks(100, 120, 25));
        }

        [Test]
        public void 기록보다_앞이면_가장_오래된_기록()
        {
            Assert.AreEqual(90L, MountedArcheryLagCompensation.RewindedTick(100, 25, 90));
            Assert.AreEqual(95L, MountedArcheryLagCompensation.RewindedTick(100, 5, 50));
            Assert.AreEqual(100L, MountedArcheryLagCompensation.RewindedTick(100, 5, null));
        }
    }
}
