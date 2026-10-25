using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class MountSystemTests
    {
        const float Dt = 0.02f;

        static readonly MountSettings S = new MountSettings(
            maxSpeed: 13f, acceleration: 9f, deceleration: 16f,
            turnRateStoppedDeg: 172f, turnRateAtMaxDeg: 92f,
            sharpTurnDeg: 100f, sharpTurnSpeedFactor: 0.35f, drawSpeedFactor: 0.5f, reverseSpeed: 3f);

        static Entity Rider(bool mounted = true)
        {
            var entity = new Entity("rider-1");
            entity.Add(new GameFramework.World.Transform { Rotation = Quaternion.identity.ToNumerics() });
            entity.Add(new GameFramework.World.Velocity());
            entity.Add(new InputBuffer());
            if (mounted)
            {
                entity.Add(new Mount());
            }
            return entity;
        }

        static void Feed(Entity entity, float h, float v, bool drawing = false)
        {
            entity.Get<InputBuffer>().Current = new InputCommand { Horizontal = h, Vertical = v, Drawing = drawing };
        }

        static Vector3 Linear(Entity e) => e.Get<GameFramework.World.Velocity>().Linear.ToUnity();

        [Test]
        public void 후진_속도가_0_이하면_설정을_거부한다()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new MountSettings(13f, 9f, 16f, 172f, 92f, 100f, 0.35f, 0.5f, 0f));
        }

        [Test]
        public void 앞으로_밀면_머리_쪽으로_빨라진다()
        {
            var rider = Rider();
            Feed(rider, 0f, 1f);
            new MountSystem(S).Tick(rider, Dt);

            var v = Linear(rider);
            Assert.AreEqual(0f, v.x, 1e-4f);
            Assert.AreEqual(0f, v.y, 1e-4f);
            Assert.AreEqual(0.18f, v.z, 1e-4f);
        }

        [Test]
        public void 속도는_속도_컴포넌트에서_읽는다()
        {
            var rider = Rider();
            rider.Get<GameFramework.World.Velocity>().Linear = new Vector3(0f, 0f, 10f).ToNumerics();
            Feed(rider, 0f, 0f);
            new MountSystem(S).Tick(rider, Dt);

            Assert.AreEqual(10f - 0.32f, Linear(rider).z, 1e-4f);
        }

        [Test]
        public void 뒤로_가는_속도는_부호를_읽어_계속_후진한다()
        {
            var rider = Rider();
            rider.Get<GameFramework.World.Velocity>().Linear = new Vector3(0f, 0f, -2f).ToNumerics();
            Feed(rider, 0f, -1f);
            new MountSystem(S).Tick(rider, Dt);

            //  크기로만 읽으면 +2로 착각해 브레이크를 걸어 -1.68이 된다. 부호를 읽으면 -2.18.
            Assert.AreEqual(-2.18f, Linear(rider).z, 1e-4f);
        }

        [Test]
        public void 세로_속도는_건드리지_않는다()
        {
            var rider = Rider();
            rider.Get<GameFramework.World.Velocity>().Linear = new Vector3(0f, -3f, 0f).ToNumerics();
            Feed(rider, 0f, 1f);
            new MountSystem(S).Tick(rider, Dt);

            Assert.AreEqual(-3f, Linear(rider).y, 1e-4f);
        }

        [Test]
        public void 탈것이_없으면_안_건드린다()
        {
            var rider = Rider(mounted: false);
            rider.Get<GameFramework.World.Velocity>().Linear = new Vector3(0f, 0f, 10f).ToNumerics();
            rider.Get<GameFramework.World.Transform>().Rotation = Quaternion.Euler(0f, 40f, 0f).ToNumerics();
            Feed(rider, 1f, 0f);
            new MountSystem(S).Tick(rider, Dt);

            Assert.AreEqual(new Vector3(0f, 0f, 10f), Linear(rider));
            Assert.AreEqual(40f, rider.Get<GameFramework.World.Transform>().Rotation.ToUnity().eulerAngles.y, 1e-3f);
        }

        [Test]
        public void 당김_입력이면_느려진다()
        {
            var rider = Rider();
            var system = new MountSystem(S);
            for (int i = 0; i < 1000; i++)
            {
                Feed(rider, 0f, 1f, drawing: true);
                system.Tick(rider, Dt);
            }

            var v = Linear(rider);
            Assert.AreEqual(6.5f, new Vector2(v.x, v.z).magnitude, 1e-3f);
        }
    }
}
