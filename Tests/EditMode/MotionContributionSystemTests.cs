using System.Numerics;
using NUnit.Framework;

namespace LOP.Tests
{
    public class MotionContributionSystemTests
    {
        private static MotionContributions With(params MotionContribution[] items)
        {
            var c = new MotionContributions();
            c.Items.AddRange(items);
            return c;
        }

        [Test]
        public void Resolve_NoContributions_ReturnsBase()
        {
            var v = MotionContributionSystem.Resolve(new Vector3(3, 0, 4), null, 0);
            Assert.AreEqual(new Vector3(3, 0, 4), v);
        }

        [Test]
        public void Resolve_ActiveOverride_ReplacesBase()
        {
            var c = With(new MotionContribution(new Vector3(9, 0, 0), MotionContributionMode.Override, 0, 0, 10));
            var v = MotionContributionSystem.Resolve(new Vector3(1, 0, 0), c, 5);
            Assert.AreEqual(new Vector3(9, 0, 0), v);
        }

        [Test]
        public void Resolve_HighestPriorityOverrideWins()
        {
            var c = With(
                new MotionContribution(new Vector3(9, 0, 0), MotionContributionMode.Override, 0, 0, 10),
                new MotionContribution(new Vector3(5, 0, 0), MotionContributionMode.Override, 7, 0, 10));
            var v = MotionContributionSystem.Resolve(new Vector3(1, 0, 0), c, 5);
            Assert.AreEqual(new Vector3(5, 0, 0), v, "priority 7 > 0");
        }

        [Test]
        public void Resolve_AdditivesSumOnTopOfOverride()
        {
            var c = With(
                new MotionContribution(new Vector3(9, 0, 0), MotionContributionMode.Override, 0, 0, 10),
                new MotionContribution(new Vector3(0, 0, 2), MotionContributionMode.Additive, 0, 0, 10),
                new MotionContribution(new Vector3(0, 0, 3), MotionContributionMode.Additive, 0, 0, 10));
            var v = MotionContributionSystem.Resolve(new Vector3(1, 0, 0), c, 5);
            Assert.AreEqual(new Vector3(9, 0, 5), v, "override(9,0,0) + additives(0,0,5)");
        }

        [Test]
        public void Resolve_InactiveContributions_Ignored()
        {
            var c = With(new MotionContribution(new Vector3(9, 0, 0), MotionContributionMode.Override, 0, 0, 10));
            var v = MotionContributionSystem.Resolve(new Vector3(1, 0, 0), c, 20); // 창 밖
            Assert.AreEqual(new Vector3(1, 0, 0), v, "창 밖 기여 무시 → base");
        }

        [Test]
        public void Prune_RemovesExpired_KeepsActive()
        {
            var c = With(
                new MotionContribution(Vector3.Zero, MotionContributionMode.Additive, 0, 0, 10),   // end 10
                new MotionContribution(Vector3.Zero, MotionContributionMode.Additive, 0, 0, 30));   // end 30
            MotionContributionSystem.Prune(c, 10);   // 10 >= 10 만료 / 10 < 30 유지
            Assert.AreEqual(1, c.Items.Count);
            Assert.AreEqual(30, c.Items[0].EndTick);
        }
    }
}
