using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    public class ActiveMotionEffectWindowTests
    {
        private static Entity DasherWithWindow(long startupEnd, long activeEnd)
        {
            var e = new Entity("d");
            var abilities = new Abilities();
            // Phase는 무관(창 기반 판정) — 일부러 Startup으로 둬 전이-틱 파리티를 검증.
            abilities.ActiveAbility = new ActiveAbility(2, AbilityPhase.Startup, startupEnd, activeEnd, activeEnd + 5,
                null, new AbilityEffect[] { new MotionEffect(15f) });
            e.Add(abilities);
            return e;
        }

        [Test]
        public void InsideWindow_ReturnsMotionEffect_IgnoringPhase()
        {
            var e = DasherWithWindow(10, 20);
            Assert.IsTrue(AbilitySystem.TryGetActiveMotionEffect(e, 10, out var me), "start 포함(전이 틱)");
            Assert.AreEqual(15f, me.Speed);
            Assert.IsTrue(AbilitySystem.TryGetActiveMotionEffect(e, 19, out _));
        }

        [Test]
        public void OutsideWindow_False()
        {
            var e = DasherWithWindow(10, 20);
            Assert.IsFalse(AbilitySystem.TryGetActiveMotionEffect(e, 9, out _), "start 이전");
            Assert.IsFalse(AbilitySystem.TryGetActiveMotionEffect(e, 20, out _), "end 제외");
        }

        [Test]
        public void NoActiveAbility_False()
        {
            var e = new Entity("x");
            e.Add(new Abilities());
            Assert.IsFalse(AbilitySystem.TryGetActiveMotionEffect(e, 5, out _));
        }
    }
}
