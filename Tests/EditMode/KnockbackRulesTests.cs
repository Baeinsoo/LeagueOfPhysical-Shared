using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class KnockbackRulesTests
    {
        static readonly KnockbackConfig Config = new KnockbackConfig(120f, 6.2f, 24f, 17f, 1.5f, 0f, 5f, 11f,
            30f, 18f, 0.3f, 0.55f, 3, 0.95f, 0.26f, 1.2f,
            12, 7f, 0.12f, 0.2f, 0.18f, 0.006f, 1.6f, 0.8f, 5f,
            new System.Collections.Generic.List<MountedArcheryZoneStageConfig>());

        static GameFramework.World.Velocity Still() => new GameFramework.World.Velocity();

        [Test]
        public void 첫_방은_8_44()
        {
            var body = new KnockbackBody();
            var vel = Still();
            KnockbackRules.ApplyHit(body, vel, new Vector2(1f, 0f), 100, Config, 0.02f);

            Assert.AreEqual(12, body.DamagePercent);
            Assert.AreEqual(8.44f, vel.Linear.X, 1e-3f);
            Assert.AreEqual(100 + 13, body.KnockbackEndTick);
        }

        [Test]
        public void 피해가_쌓일수록_세진다()
        {
            var body = new KnockbackBody { DamagePercent = 84 };
            var vel = Still();
            KnockbackRules.ApplyHit(body, vel, new Vector2(1f, 0f), 0, Config, 0.02f);

            Assert.AreEqual(96, body.DamagePercent);
            Assert.AreEqual(18.52f, vel.Linear.X, 1e-3f);
        }

        [Test]
        public void 지금_속도의_0_2를_더한다()
        {
            var body = new KnockbackBody();
            var vel = Still();
            vel.Linear = new System.Numerics.Vector3(0f, 3f, 5f);
            KnockbackRules.ApplyHit(body, vel, new Vector2(1f, 0f), 0, Config, 0.02f);

            Assert.AreEqual(1.0f, vel.Linear.Z, 1e-3f);
            Assert.AreEqual(3f, vel.Linear.Y, 1e-4f);
        }
    }
}
