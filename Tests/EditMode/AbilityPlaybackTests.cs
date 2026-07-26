using NUnit.Framework;

namespace LOP.Tests
{
    public class AbilityPlaybackTests
    {
        // startup 10틱, active 20틱, recovery 10틱 = 총 40틱. 발동 100틱 → 종료 140틱.
        private const long Total = 40;
        private static ActiveAbility Make() =>
            ActiveAbility.ForPresentation(abilityId: 7, startupEndTick: 110, activeEndTick: 130, recoveryEndTick: 140);

        [Test]
        public void StartOfCastIsStartupAtZero()
        {
            Assert.IsTrue(AbilityPlayback.Solve(Make(), 100, Total, out var phase, out float t));
            Assert.AreEqual(AbilityPhase.Startup, phase);
            Assert.AreEqual(0f, t, 1e-4f);
        }

        [Test]
        public void MidStartup()
        {
            Assert.IsTrue(AbilityPlayback.Solve(Make(), 105, Total, out var phase, out float t));
            Assert.AreEqual(AbilityPhase.Startup, phase);
            Assert.AreEqual(0.125f, t, 1e-4f);   // (105-100)/40
        }

        [Test]
        public void StartupEndTickBelongsToActive()
        {
            Assert.IsTrue(AbilityPlayback.Solve(Make(), 110, Total, out var phase, out _));
            Assert.AreEqual(AbilityPhase.Active, phase);
        }

        [Test]
        public void ActiveEndTickBelongsToRecovery()
        {
            Assert.IsTrue(AbilityPlayback.Solve(Make(), 130, Total, out var phase, out _));
            Assert.AreEqual(AbilityPhase.Recovery, phase);
        }

        [Test]
        public void LastTickIsAlmostOne()
        {
            Assert.IsTrue(AbilityPlayback.Solve(Make(), 139, Total, out _, out float t));
            Assert.AreEqual(0.975f, t, 1e-4f);   // (139-100)/40
        }

        [Test]
        public void AtOrAfterEndTickIsNotPlaying()
        {
            Assert.IsFalse(AbilityPlayback.Solve(Make(), 140, Total, out var phase, out float t));
            Assert.AreEqual(AbilityPhase.Ready, phase);
            Assert.AreEqual(0f, t);
        }

        [Test]
        public void BeforeActivationIsNotPlaying()
        {
            Assert.IsFalse(AbilityPlayback.Solve(Make(), 99, Total, out _, out _));
        }

        [Test]
        public void NonPositiveTotalIsNotPlaying()
        {
            Assert.IsFalse(AbilityPlayback.Solve(Make(), 105, 0, out _, out _));
        }
    }
}
