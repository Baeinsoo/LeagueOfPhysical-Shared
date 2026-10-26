using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class KnockbackWorldTests
    {
        const float Dt = 0.02f;

        static readonly KnockbackConfig Config = new KnockbackConfig(120f, 6.2f, 14f, 17f, 1.5f, 0f, 5f, 11f);

        static (KnockbackWorld world, Entity body) Make(float z)
        {
            var registry = new EntityRegistry();
            var body = new Entity("body-1");
            body.Add(new GameFramework.World.Transform { Position = new System.Numerics.Vector3(0f, 0f, z) });
            body.Add(new GameFramework.World.Velocity());
            body.Add(new CapsuleShape(BodySizes.CharacterRadius, BodySizes.CharacterHeight));
            body.Add(new GroundState { IsGrounded = true });
            body.Add(new InputBuffer());
            body.Add(new Simulated());
            registry.Add(body);

            var floor = Config.CreateFloor();
            var world = new KnockbackWorld(registry, new WorldEventBuffer(),
                Config.CreateMoveSystem(floor, new FlappyWorldFixture.NeverHit(), ~0),
                new FlappyWorldFixture.NoopMotionBridge(), floor)
            { GameplayStartTick = 0 };
            return (world, body);
        }

        static void Step(KnockbackWorld world, Entity body, long t, float h, float v)
        {
            body.Get<InputBuffer>().Current = new InputCommand { Horizontal = h, Vertical = v };
            world.Tick(t, Dt);
        }

        [Test]
        public void 걸으면_움직인다()
        {
            var (world, body) = Make(0f);
            for (long t = 1; t <= 50; t++) { Step(world, body, t, 0f, 1f); }
            Assert.Greater(body.Get<GameFramework.World.Transform>().Position.Z, 0f);
        }

        [Test]
        public void 가장자리_밖에서_떨어진다()
        {
            //  16.5는 이미 칸 바깥(가장자리 끝)이라 15.9에서 시작해 걸어 나간다
            var (world, body) = Make(15.9f);
            for (long t = 1; t <= 60; t++) { Step(world, body, t, 0f, 1f); }
            Assert.Less(body.Get<GameFramework.World.Transform>().Position.Y, -1f);
        }

        [Test]
        public void 되감아_다시_돌리면_같은_자리()
        {
            var (world, body) = Make(15.9f);
            var ground = body.Get<GroundState>();
            long fallTick = 0;
            for (long t = 1; t <= 60 && fallTick == 0; t++)
            {
                Step(world, body, t, 0f, 1f);
                if (ground.IsGrounded == false) { fallTick = t; }
                else { world.SaveState(t); }
            }
            Assert.Greater(fallTick, 1, "떨어지기 시작해야 한다");
            long saveTick = fallTick - 1;   // 떨어지기 직전, 아직 접지 상태

            void Run()
            {
                for (long t = saveTick + 1; t <= saveTick + 40; t++) { Step(world, body, t, 0f, 1f); }
            }
            world.LoadState(saveTick);   // 떨어지기 시작한 틱까지 이미 진행했으니 저장 지점에서 다시 시작
            Run();
            var pos1 = body.Get<GameFramework.World.Transform>().Position;
            var vel1 = body.Get<GameFramework.World.Velocity>().Linear;
            bool grounded1 = ground.IsGrounded;

            world.LoadState(saveTick);
            Assert.IsTrue(ground.IsGrounded, "저장 시점은 접지 상태");
            Run();

            var pos2 = body.Get<GameFramework.World.Transform>().Position;
            var vel2 = body.Get<GameFramework.World.Velocity>().Linear;
            Assert.AreEqual(pos1.X, pos2.X, 1e-5f);
            Assert.AreEqual(pos1.Y, pos2.Y, 1e-5f);
            Assert.AreEqual(pos1.Z, pos2.Z, 1e-5f);
            Assert.AreEqual(vel1.Y, vel2.Y, 1e-5f);
            Assert.AreEqual(vel1.Z, vel2.Z, 1e-5f);
            Assert.AreEqual(grounded1, ground.IsGrounded);
            Assert.Less(pos2.Y, -1f, "재생에서도 떨어졌어야 한다");
        }

        [Test]
        public void 출발_전에는_입력을_줘도_안_움직인다()
        {
            var (world, body) = Make(0f);
            world.GameplayStartTick = 100;
            for (long t = 1; t <= 50; t++) { Step(world, body, t, 1f, 0f); }
            var before = body.Get<GameFramework.World.Transform>().Position;
            Assert.AreEqual(0f, before.X, 1e-6f);
            Assert.AreEqual(0f, body.Get<GameFramework.World.Velocity>().Linear.X, 1e-6f);

            for (long t = 100; t <= 150; t++) { Step(world, body, t, 1f, 0f); }
            Assert.Greater(body.Get<GameFramework.World.Transform>().Position.X, 0f, "출발 뒤에는 움직여야 한다");
        }

        [Test]
        public void 설정이_바닥을_만든다()
        {
            Assert.IsTrue(Config.CreateFloor().IsSolid(0f, 0f, 0));
            Assert.AreEqual(6000, Config.MatchDurationTicks(0.02f));
        }
    }
}
