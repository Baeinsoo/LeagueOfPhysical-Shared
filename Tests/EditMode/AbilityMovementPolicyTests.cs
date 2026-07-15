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

        private static Entity EntityWith(ActiveAbility? active)
        {
            var e = new Entity("e1");
            var ab = new Abilities();
            ab.ActiveAbility = active;
            e.Add(ab);
            return e;
        }

        [Test]
        public void GetMovementMultiplier_NoActiveAbility_ReturnsOne()
        {
            Assert.That(AbilitySystem.GetMovementMultiplier(EntityWith(null), 5), Is.EqualTo(1f));
        }

        [Test]
        public void GetMovementMultiplier_PicksPhaseScale_ByBoundaryTick()
        {
            // 경계: startupEnd=10, activeEnd=100, recoveryEnd=200. 배율 0.5/0/0.3
            var e = EntityWith(new ActiveAbility(3, AbilityPhase.Startup, 10, 100, 200, null,
                                                 new AbilityEffect[0], 0.5f, 0f, 0.3f, false));
            Assert.That(AbilitySystem.GetMovementMultiplier(e, 5),   Is.EqualTo(0.5f), "startup");
            Assert.That(AbilitySystem.GetMovementMultiplier(e, 50),  Is.EqualTo(0f),   "active");
            Assert.That(AbilitySystem.GetMovementMultiplier(e, 150), Is.EqualTo(0.3f), "recovery");
            Assert.That(AbilitySystem.GetMovementMultiplier(e, 200), Is.EqualTo(1f),   "종료 후");
        }

        [Test]
        public void IsJumpBlocked_TrueWithinWindow_FalseAfterOrFlagOff()
        {
            var blocking = EntityWith(new ActiveAbility(3, AbilityPhase.Active, 0, 5, 10, null,
                                                        new AbilityEffect[0], 1f, 1f, 1f, blockJump: true));
            Assert.IsTrue(AbilitySystem.IsJumpBlocked(blocking, 3),  "발동 창 안");
            Assert.IsFalse(AbilitySystem.IsJumpBlocked(blocking, 10), "RecoveryEnd 이후");

            var nonBlocking = EntityWith(new ActiveAbility(3, AbilityPhase.Active, 0, 5, 10, null,
                                                           new AbilityEffect[0]));
            Assert.IsFalse(AbilitySystem.IsJumpBlocked(nonBlocking, 3), "플래그 off");
        }

        [Test]
        public void TryActivate_CopiesMovePolicy_IntoActiveAbility()
        {
            var system = new AbilitySystem(new ManaSystem());
            var e = new Entity("caster");
            e.Add(new Abilities());
            system.Grant(e, 3);
            var data = new AbilityData(3, 10, 0, 2, 3, 2, new AbilityEffect[0],
                                       0.5f, 0f, 0.3f, blockJump: true);

            Assert.IsTrue(system.TryActivate(e, data, e, 0));

            var a = e.Get<Abilities>().ActiveAbility.Value;
            Assert.That(a.ActiveMoveScale, Is.EqualTo(0f));
            Assert.That(a.RecoveryMoveScale, Is.EqualTo(0.3f));
            Assert.That(a.BlockJump, Is.True);
        }
    }
}
