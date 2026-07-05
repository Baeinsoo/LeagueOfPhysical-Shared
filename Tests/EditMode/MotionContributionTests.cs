using System.Numerics;
using NUnit.Framework;

namespace LOP.Tests
{
    public class MotionContributionTests
    {
        [Test]
        public void IsActiveAt_WithinWindow_True_BoundariesHalfOpen()
        {
            var c = new MotionContribution(new Vector3(1, 0, 0), MotionContributionMode.Override, 0, 10, 20);
            Assert.IsFalse(c.IsActiveAt(9), "start 이전");
            Assert.IsTrue(c.IsActiveAt(10), "start 포함");
            Assert.IsTrue(c.IsActiveAt(19));
            Assert.IsFalse(c.IsActiveAt(20), "end 제외(half-open)");
        }

        [Test]
        public void Component_HoldsItems()
        {
            var comp = new MotionContributions();
            Assert.AreEqual(0, comp.Items.Count);
            comp.Items.Add(new MotionContribution(new Vector3(2, 0, 0), MotionContributionMode.Additive, 1, 0, 5));
            Assert.AreEqual(1, comp.Items.Count);
            Assert.AreEqual(MotionContributionMode.Additive, comp.Items[0].Mode);
        }
    }
}
