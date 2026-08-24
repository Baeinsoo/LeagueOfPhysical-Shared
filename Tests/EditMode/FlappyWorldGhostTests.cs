using GameFramework;
using GameFramework.Physics;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    // 전진 속도가 고정이라 "막기"로는 벽에 박힌 새가 수평으로 영영 빠져나오지 못한다(실측 확인).
    // 맵은 이제 막지 않고 통과시키되, 부딪힌 새를 유령정지(FlappyGhostSystem)로 넘긴다.
    public class FlappyWorldGhostTests
    {
        private class AlwaysHit : ICollisionQuery
        {
            public CollisionHit CapsuleCast(Vector3 p1, Vector3 p2, float radius,
                Vector3 direction, float distance, int layerMask)
                => new CollisionHit(true, 0f, Vector3.up, p1);
        }

        private class NeverHit : ICollisionQuery
        {
            public CollisionHit CapsuleCast(Vector3 p1, Vector3 p2, float radius,
                Vector3 direction, float distance, int layerMask)
                => CollisionHit.None;
        }

        [Test]
        public void 맵에_닿으면_멈춘다_그러나_막히지는_않는다()
        {
            var world = FlappyWorldFixture.Create(new AlwaysHit(), out var bird);
            Vector3 before = bird.Get<GameFramework.World.Transform>().Position.ToUnity();

            world.Tick(1, 0.02f);

            // 통과한다 — 위치는 그대로가 아니라 앞으로 나가 있다.
            Vector3 after = bird.Get<GameFramework.World.Transform>().Position.ToUnity();
            Assert.That(after.x, Is.GreaterThan(before.x));
            // 그리고 유령에 걸렸다.
            Assert.That(bird.Get<LOP.FlappyGhost>().Remaining, Is.GreaterThan(0f));
        }

        [Test]
        public void 유령_중에는_속도가_0이다()
        {
            var world = FlappyWorldFixture.Create(new AlwaysHit(), out var bird);
            world.Tick(1, 0.02f);   // 유령 진입

            world.Tick(2, 0.02f);

            Vector3 velocity = bird.Get<GameFramework.World.Velocity>().Linear.ToUnity();
            Assert.That(velocity, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void 맵에_안_닿으면_평소대로_전진한다()
        {
            var world = FlappyWorldFixture.Create(new NeverHit(), out var bird);

            world.Tick(1, 0.02f);

            Assert.That(bird.Get<LOP.FlappyGhost>().Remaining, Is.EqualTo(0f));
            Assert.That(bird.Get<GameFramework.World.Velocity>().Linear.X, Is.GreaterThan(0f));
        }
    }
}
