using System.Numerics;
using NUnit.Framework;

namespace LOP.Tests
{
    public class MotionContributionSystemTests
    {
        private MotionContributionSystem system;

        [SetUp]
        public void SetUp() => system = new MotionContributionSystem();

        private static MotionContributions With(params MotionContribution[] items)
        {
            var c = new MotionContributions();
            c.Items.AddRange(items);
            return c;
        }

        [Test]
        public void Resolve_NoContributions_ReturnsBase()
        {
            var v = system.Resolve(new Vector3(3, 0, 4), null, 0);
            Assert.AreEqual(new Vector3(3, 0, 4), v);
        }

        [Test]
        public void Resolve_ActiveOverride_ReplacesBase()
        {
            var c = With(new MotionContribution(new Vector3(9, 0, 0), MotionContributionMode.Override, 0, 0, 10));
            var v = system.Resolve(new Vector3(1, 0, 0), c, 5);
            Assert.AreEqual(new Vector3(9, 0, 0), v);
        }

        [Test]
        public void Resolve_HighestPriorityOverrideWins()
        {
            var c = With(
                new MotionContribution(new Vector3(9, 0, 0), MotionContributionMode.Override, 0, 0, 10),
                new MotionContribution(new Vector3(5, 0, 0), MotionContributionMode.Override, 7, 0, 10));
            var v = system.Resolve(new Vector3(1, 0, 0), c, 5);
            Assert.AreEqual(new Vector3(5, 0, 0), v, "priority 7 > 0");
        }

        [Test]
        public void Resolve_AdditivesSumOnTopOfOverride()
        {
            var c = With(
                new MotionContribution(new Vector3(9, 0, 0), MotionContributionMode.Override, 0, 0, 10),
                new MotionContribution(new Vector3(0, 0, 2), MotionContributionMode.Additive, 0, 0, 10),
                new MotionContribution(new Vector3(0, 0, 3), MotionContributionMode.Additive, 0, 0, 10));
            var v = system.Resolve(new Vector3(1, 0, 0), c, 5);
            Assert.AreEqual(new Vector3(9, 0, 5), v, "override(9,0,0) + additives(0,0,5)");
        }

        [Test]
        public void Resolve_InactiveContributions_Ignored()
        {
            var c = With(new MotionContribution(new Vector3(9, 0, 0), MotionContributionMode.Override, 0, 0, 10));
            var v = system.Resolve(new Vector3(1, 0, 0), c, 20); // 창 밖
            Assert.AreEqual(new Vector3(1, 0, 0), v, "창 밖 기여 무시 → base");
        }

        [Test]
        public void Prune_RemovesExpired_KeepsActive()
        {
            var c = With(
                new MotionContribution(Vector3.Zero, MotionContributionMode.Additive, 0, 0, 10),   // end 10
                new MotionContribution(Vector3.Zero, MotionContributionMode.Additive, 0, 0, 30));   // end 30
            system.Prune(c, 10);   // 10 >= 10 만료 / 10 < 30 유지
            Assert.AreEqual(1, c.Items.Count);
            Assert.AreEqual(30, c.Items[0].EndTick);
        }

        [Test]
        public void Resolve_AdditiveDecaysExponentially()
        {
            // v0=(0,0,10), k=0.5, 창[0,100)
            var c = With(new MotionContribution(new Vector3(0, 0, 10), MotionContributionMode.Additive, 0, 0, 100, 0.5f));
            Assert.Less(Vector3.Distance(system.Resolve(Vector3.Zero, c, 0), new Vector3(0, 0, 10)), 1e-4f, "elapsed 0 → v0");
            Assert.Less(Vector3.Distance(system.Resolve(Vector3.Zero, c, 1), new Vector3(0, 0, 5)), 1e-4f, "elapsed 1 → v0*0.5");
            Assert.Less(Vector3.Distance(system.Resolve(Vector3.Zero, c, 2), new Vector3(0, 0, 2.5f)), 1e-4f, "elapsed 2 → v0*0.25");
        }

        [Test]
        public void Resolve_DecayOne_IsConstant_NoRegression()
        {
            var c = With(new MotionContribution(new Vector3(0, 0, 5), MotionContributionMode.Additive, 0, 0, 100, 1f));
            Assert.Less(Vector3.Distance(system.Resolve(Vector3.Zero, c, 0), new Vector3(0, 0, 5)), 1e-4f);
            Assert.Less(Vector3.Distance(system.Resolve(Vector3.Zero, c, 50), new Vector3(0, 0, 5)), 1e-4f, "k=1 → 상수");
        }

        [Test]
        public void CreateRadialKnockback_PushesAwayFromAttacker()
        {
            // attacker (0,0,0), target (3,0,4) → 방향 (0.6,0,0.8), strength 10 → (6,0,8)
            var c = MotionContributionSystem.CreateRadialKnockback(
                Vector3.Zero, new Vector3(3, 0, 4), strength: 10f, durationTicks: 12, decayPerTick: 0.8f, currentTick: 5);
            Assert.Less(Vector3.Distance(c.Horizontal, new Vector3(6, 0, 8)), 1e-4f, "radial away × strength");
            Assert.AreEqual(MotionContributionMode.Additive, c.Mode);
            Assert.AreEqual(5, c.StartTick);
            Assert.AreEqual(17, c.EndTick, "start + duration");
            Assert.AreEqual(0.8f, c.DecayPerTick, 1e-6f);
        }

        [Test]
        public void CreateRadialKnockback_IgnoresY()
        {
            // 높이 차가 있어도 수평만 — target y는 무시
            var c = MotionContributionSystem.CreateRadialKnockback(
                new Vector3(0, 5, 0), new Vector3(3, 0, 4), 10f, 12, 0.8f, 0);
            Assert.Less(Vector3.Distance(c.Horizontal, new Vector3(6, 0, 8)), 1e-4f);
        }

        [Test]
        public void CreateRadialKnockback_SamePosition_ZeroPush()
        {
            var c = MotionContributionSystem.CreateRadialKnockback(
                new Vector3(2, 0, 2), new Vector3(2, 0, 2), 10f, 12, 0.8f, 0);
            Assert.Less(c.Horizontal.Length(), 1e-4f, "겹친 위치 → 0(NaN 방지)");
        }
    }
}
