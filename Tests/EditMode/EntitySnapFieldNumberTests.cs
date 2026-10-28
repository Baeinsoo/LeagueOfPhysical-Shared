using NUnit.Framework;

namespace LOP.Tests
{
    public class EntitySnapFieldNumberTests
    {
        [Test]
        public void 스냅_번호는_flap_combo와_겹치지_않는다()
        {
            Assert.AreEqual(25, EntitySnap.FlapComboFieldNumber);
            Assert.AreEqual(26, EntitySnap.KnockbackEndTickFieldNumber);
            Assert.AreEqual(27, EntitySnap.DamagePercentFieldNumber);
        }
    }
}
