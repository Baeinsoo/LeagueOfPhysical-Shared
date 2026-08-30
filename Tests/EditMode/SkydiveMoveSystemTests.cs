using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class SkydiveMoveSystemTests
    {
        const float Tolerance = 1e-4f;

        static Entity Body(Vector3 position, Vector3 velocity)
        {
            var entity = new Entity("diver-1");
            entity.Add(new GameFramework.World.Transform { Position = position.ToNumerics() });
            entity.Add(new Velocity { Linear = velocity.ToNumerics() });
            return entity;
        }

        static Vector3 PositionOf(Entity entity) => entity.Get<GameFramework.World.Transform>().Position.ToUnity();
        static Vector3 VelocityOf(Entity entity) => entity.Get<Velocity>().Linear.ToUnity();

        [Test]
        public void 중력이_세로_속도를_깎는다()
        {
            var body = Body(new Vector3(0f, 100f, 0f), Vector3.zero);

            new SkydiveMoveSystem().Tick(body, 0.1f);

            Assert.AreEqual(-2f, VelocityOf(body).y, Tolerance);   // 20 × 0.1
        }

        [Test]
        public void 낙하_속도가_상한을_넘지_않는다()
        {
            var body = Body(new Vector3(0f, 100f, 0f), new Vector3(0f, -SkydiveMoveSystem.MaxFallSpeed, 0f));

            new SkydiveMoveSystem().Tick(body, 1f);

            Assert.AreEqual(-SkydiveMoveSystem.MaxFallSpeed, VelocityOf(body).y, Tolerance);
        }

        [Test]
        public void 속도만큼_아래로_내려간다()
        {
            var body = Body(new Vector3(0f, 100f, 0f), new Vector3(0f, -10f, 0f));

            new SkydiveMoveSystem().Tick(body, 0.1f);

            // 속도 갱신이 먼저다: -10 - 20×0.1 = -12 → 100 + (-12 × 0.1) = 98.8
            Assert.AreEqual(98.8f, PositionOf(body).y, Tolerance);
        }

        [Test]
        public void 바닥에_닿으면_멈춘다()
        {
            var body = Body(new Vector3(0f, SkydiveMoveSystem.GroundY + 0.5f, 0f), new Vector3(0f, -30f, 0f));

            new SkydiveMoveSystem().Tick(body, 0.1f);

            Assert.AreEqual(SkydiveMoveSystem.GroundY, PositionOf(body).y, Tolerance);
            Assert.AreEqual(0f, VelocityOf(body).y, Tolerance);
        }

        [Test]
        public void 수평_속도는_건드리지_않는다()
        {
            var body = Body(new Vector3(0f, 100f, 0f), new Vector3(3f, 0f, -4f));

            new SkydiveMoveSystem().Tick(body, 0.1f);

            Assert.AreEqual(3f, VelocityOf(body).x, Tolerance);
            Assert.AreEqual(-4f, VelocityOf(body).z, Tolerance);
            Assert.AreEqual(0.3f, PositionOf(body).x, Tolerance);
            Assert.AreEqual(-0.4f, PositionOf(body).z, Tolerance);
        }
    }
}
