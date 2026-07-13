using GameFramework.World;
using LOP;
using NUnit.Framework;

namespace LOP.Tests
{
    public class AbilityMovementPolicyTests
    {
        private static ActiveAbility MakeActive(float su, float ac, float re, bool jump)
            => new ActiveAbility(3, AbilityPhase.Startup, 10, 100, 200, null,
                                 new AbilityEffect[0], su, ac, re, jump);

        [Test]
        public void WithPhase_PreservesMoveScalesAndBlockJump()
        {
            var a = MakeActive(0.5f, 0f, 0.3f, true).WithPhase(AbilityPhase.Active);

            Assert.That(a.Phase, Is.EqualTo(AbilityPhase.Active));
            Assert.That(a.StartupMoveScale, Is.EqualTo(0.5f));
            Assert.That(a.ActiveMoveScale, Is.EqualTo(0f));
            Assert.That(a.RecoveryMoveScale, Is.EqualTo(0.3f));
            Assert.That(a.BlockJump, Is.True);
        }

        [Test]
        public void AbilityData_DefaultMovePolicy_IsUnrestricted()
        {
            var d = new AbilityData(3, 0, 0, 0, 1, 0, new AbilityEffect[0]);

            Assert.That(d.StartupMoveScale, Is.EqualTo(1f));
            Assert.That(d.ActiveMoveScale, Is.EqualTo(1f));
            Assert.That(d.RecoveryMoveScale, Is.EqualTo(1f));
            Assert.That(d.BlockJump, Is.False);
        }
    }
}
