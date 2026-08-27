using GameFramework.Physics;
using NUnit.Framework;

namespace LOP.Tests
{
    public class FlappyWorldStartGateTests
    {
        //  이웃 테스트 파일들과 같은 모양의 "아무것도 안 맞는" 스텁. 공용 픽스처로 빼지 않은 것은
        //  이미 네 파일이 각자 갖고 있어, 여기만 바꾸면 오히려 어긋나 보이기 때문이다.
        private class EmptySkyQuery : ICollisionQuery
        {
            public CollisionHit CapsuleCast(UnityEngine.Vector3 p1, UnityEngine.Vector3 p2, float radius,
                UnityEngine.Vector3 direction, float distance, int layerMask)
                => CollisionHit.None;

            public CollisionHit Raycast(UnityEngine.Vector3 origin, UnityEngine.Vector3 direction, float distance, int layerMask)
                => CollisionHit.None;

            public CollisionHit[] OverlapSphere(UnityEngine.Vector3 center, float radius, int layerMask)
                => System.Array.Empty<CollisionHit>();

        }

        private const float Dt = 0.02f;

        [Test]
        public void 출발틱이_안_정해졌으면_아무리_굴려도_안_움직인다()
        {
            var world = FlappyWorldFixture.Create(new EmptySkyQuery(), out var bird);

            for (long tick = 0; tick < 200; tick++)
            {
                world.Tick(tick, Dt);
            }

            Assert.AreEqual(System.Numerics.Vector3.Zero, bird.Get<GameFramework.World.Transform>().Position);
            Assert.AreEqual(System.Numerics.Vector3.Zero, bird.Get<GameFramework.World.Velocity>().Linear);
        }

        [Test]
        public void 출발틱_직전까지는_안_움직인다()
        {
            var world = FlappyWorldFixture.Create(new EmptySkyQuery(), out var bird);
            world.GameplayStartTick = 100;

            for (long tick = 0; tick < 100; tick++)
            {
                world.Tick(tick, Dt);
            }

            Assert.AreEqual(System.Numerics.Vector3.Zero, bird.Get<GameFramework.World.Transform>().Position);
        }

        [Test]
        public void 출발틱부터_움직인다()
        {
            var world = FlappyWorldFixture.Create(new EmptySkyQuery(), out var bird);
            world.GameplayStartTick = 100;

            for (long tick = 0; tick <= 100; tick++)
            {
                world.Tick(tick, Dt);
            }

            //  전진 속도가 붙어 x가 커지고, 중력으로 y가 내려간다.
            Assert.Greater(bird.Get<GameFramework.World.Transform>().Position.X, 0f);
            Assert.Less(bird.Get<GameFramework.World.Transform>().Position.Y, 0f);
        }

        [Test]
        public void 출발_경계를_가로질러_두_번_굴려도_결과가_같다()
        {
            System.Numerics.Vector3 Run()
            {
                var world = FlappyWorldFixture.Create(new EmptySkyQuery(), out var bird);
                world.GameplayStartTick = 50;
                for (long tick = 40; tick < 60; tick++)
                {
                    world.Tick(tick, Dt);
                }
                return bird.Get<GameFramework.World.Transform>().Position;
            }

            Assert.AreEqual(Run(), Run());
        }
    }
}
