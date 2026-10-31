using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class KnockbackFireSystemTests
    {
        static readonly KnockbackConfig Config = new KnockbackConfig(120f, 6.2f, 14f, 17f, 1.5f, 0f, 5f, 11f,
            30f, 18f, 0.3f, 0.55f, 3, 0.95f, 0.26f, 1.2f, 1, 6f, 0.5f, 0.5f, 0.3f, 0.1f, 0.2f, 0.8f, 5f,
            new System.Collections.Generic.List<MountedArcheryZoneStageConfig>());

        static Entity Body(int ammo = 3)
        {
            var body = new Entity("body-1");
            body.Add(new GameFramework.World.Transform());
            body.Add(new GameFramework.World.Velocity());
            body.Add(new InputBuffer());
            body.Add(new Simulated());
            body.Add(new KnockbackGun(Config.AmmoMax) { Ammo = ammo });
            return body;
        }

        static void Aim(Entity body, bool fire, float yaw = 0f)
            => body.Get<InputBuffer>().Current = new InputCommand { Fire = fire, AimYaw = yaw };

        [Test]
        public void 쏘면_탄이_나가고_한_발_준다()
        {
            var body = Body();
            Aim(body, true, 90f);
            Assert.IsTrue(new KnockbackFireSystem(Config).TryFire(body, 10, out var shot));
            Assert.AreEqual(2, body.Get<KnockbackGun>().Ammo);
            Assert.AreEqual(1f, shot.Direction.x, 1e-4f);
            Assert.AreEqual(0f, shot.Direction.y, 1e-4f);
            Assert.AreEqual(0.7f, shot.Origin.x, 1e-4f);
        }

        [Test]
        public void 탄이_없으면_안_나간다()
        {
            var body = Body(0);
            Aim(body, true);
            Assert.IsFalse(new KnockbackFireSystem(Config).TryFire(body, 10, out _));
        }

        [Test]
        public void 연사_간격_안이면_안_나간다()
        {
            var body = Body();
            var sys = new KnockbackFireSystem(Config);
            Aim(body, true);
            Assert.IsTrue(sys.TryFire(body, 10, out _));
            Assert.IsFalse(sys.TryFire(body, 22, out _));
            Assert.IsTrue(sys.TryFire(body, 23, out _));
        }

        [Test]
        public void 충전은_48틱마다_한_발()
        {
            var body = Body();
            var sys = new KnockbackFireSystem(Config);
            Aim(body, true);
            sys.TryFire(body, 0, out _);
            var gun = body.Get<KnockbackGun>();
            for (long t = 0; t <= 47; t++) { sys.Reload(body, t); }
            Assert.AreEqual(2, gun.Ammo);
            sys.Reload(body, 48);
            Assert.AreEqual(3, gun.Ammo);
            Assert.AreEqual(long.MaxValue, gun.NextReloadTick);
        }

        [Test]
        public void 반동은_반대로_1_2()
        {
            var body = Body();
            Aim(body, true, 90f);
            new KnockbackFireSystem(Config).TryFire(body, 10, out _);
            Assert.AreEqual(-1.2f, body.Get<GameFramework.World.Velocity>().Linear.X, 1e-4f);
        }

        [Test]
        public void 발사가_아니면_아무것도_안_한다()
        {
            var body = Body();
            Aim(body, false);
            Assert.IsFalse(new KnockbackFireSystem(Config).TryFire(body, 10, out _));
            Assert.AreEqual(3, body.Get<KnockbackGun>().Ammo);
        }
    }
}
