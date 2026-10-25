using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class KnockbackMoveSystemTests
    {
        const float Dt = 0.02f;
        const float MoveSpeed = 6.2f;
        const float Gain = 14f;

        static KnockbackMoveSystem NewSystem() => new KnockbackMoveSystem(
            new KnockbackFloor(17f, 1.5f, 0f), new FlappyWorldFixture.NeverHit(), ~0, MoveSpeed, Gain);

        static Entity Body(float x, float y, float z, bool grounded = true)
        {
            var e = new Entity("body-1");
            e.Add(new GameFramework.World.Transform { Position = new System.Numerics.Vector3(x, y, z) });
            e.Add(new GameFramework.World.Velocity());
            e.Add(new CapsuleShape(BodySizes.CharacterRadius, BodySizes.CharacterHeight));
            e.Add(new GroundState { IsGrounded = grounded });
            e.Add(new InputBuffer());
            return e;
        }

        static void Feed(Entity e, float h, float v)
            => e.Get<InputBuffer>().Current = new InputCommand { Horizontal = h, Vertical = v };

        static Vector3 Pos(Entity e) => e.Get<GameFramework.World.Transform>().Position.ToUnity();
        static Vector3 Vel(Entity e) => e.Get<GameFramework.World.Velocity>().Linear.ToUnity();

        [Test]
        public void 모터는_목표_속도로_다가간다()
        {
            var a = KnockbackMotor.Step(Vector2.zero, new Vector2(1f, 0f), MoveSpeed, Gain, Dt);
            Assert.AreEqual(1.736f, a.x, 1e-4f);
            Assert.AreEqual(0f, a.y, 1e-4f);

            var b = KnockbackMotor.Step(Vector2.zero, new Vector2(3f, 0f), MoveSpeed, Gain, Dt);
            Assert.AreEqual(1.736f, b.x, 1e-4f);
        }

        [Test]
        public void 오른쪽으로_밀면_오른쪽으로_간다()
        {
            var sys = NewSystem();
            var e = Body(0f, 0f, 0f);
            Feed(e, 1f, 0f);
            for (int t = 0; t < 50; t++) sys.Tick(e, t, Dt);

            Assert.Greater(Pos(e).x, 0f);
            Assert.AreEqual(0f, Pos(e).y, 1e-4f);
            Assert.AreEqual(MoveSpeed, Vel(e).x, 0.05f);
        }

        [Test]
        public void 원판_밖으로_걸어_나가면_떨어진다()
        {
            var sys = NewSystem();
            var e = Body(16.5f, 0f, 0f);
            Feed(e, 1f, 0f);
            for (int t = 0; t < 30; t++) sys.Tick(e, t, Dt);

            Assert.Less(Pos(e).y, 0f);
            Assert.IsFalse(e.Get<GroundState>().IsGrounded);
        }

        [Test]
        public void 바닥보다_내려가면_칸_위에서도_계속_떨어진다()
        {
            var sys = NewSystem();
            var e = Body(0f, -0.2f, 0f, grounded: false);
            Feed(e, 0f, 0f);
            sys.Tick(e, 0, Dt);

            Assert.Less(Pos(e).y, -0.2f);
        }

        [Test]
        public void 떨어지는_동안은_조작이_안_먹힌다()
        {
            var sys = NewSystem();
            var e = Body(20f, 0f, 0f, grounded: false);
            e.Get<GameFramework.World.Velocity>().Linear = new System.Numerics.Vector3(1f, 0f, 0f);
            Feed(e, -1f, 0f);
            sys.Tick(e, 0, Dt);

            Assert.AreEqual(1f, Vel(e).x, 1e-4f);
        }

        [Test]
        public void 세로_속도_0으로_바닥에_선다()
        {
            var sys = NewSystem();
            var e = Body(0f, 0f, 0f);
            e.Get<GameFramework.World.Velocity>().Linear = new System.Numerics.Vector3(0f, -1f, 0f);
            Feed(e, 0f, 0f);
            sys.Tick(e, 0, Dt);

            Assert.AreEqual(0f, Pos(e).y, 1e-4f);
            Assert.AreEqual(0f, Vel(e).y, 1e-4f);
            Assert.IsTrue(e.Get<GroundState>().IsGrounded);
        }
    }
}
