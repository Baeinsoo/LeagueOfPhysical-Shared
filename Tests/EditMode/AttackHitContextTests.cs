using System.Linq;
using NUnit.Framework;

namespace LOP.Tests
{
    public class AttackHitContextTests
    {
        [Test]
        public void Landed_false_until_marked()
        {
            var hit = new AttackHitContext();
            Assert.IsFalse(hit.Landed("B"));
        }

        [Test]
        public void MarkLanded_records_target()
        {
            var hit = new AttackHitContext();
            hit.MarkLanded("B");
            Assert.IsTrue(hit.Landed("B"));
            Assert.IsFalse(hit.Landed("C"));
        }

        [Test]
        public void LandedTargets_enumerates_marked_unique()
        {
            var hit = new AttackHitContext();
            hit.MarkLanded("B");
            hit.MarkLanded("B");
            hit.MarkLanded("C");
            Assert.AreEqual(new[] { "B", "C" }, hit.LandedTargets.OrderBy(x => x).ToArray());
        }
    }
}
