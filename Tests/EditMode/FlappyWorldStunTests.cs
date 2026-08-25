using GameFramework;
using GameFramework.Physics;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    // 맵은 막는다(FlappyWorldSolidMapTests 참고) — 여기서는 부딪힌 새가 스턴(FlappyStunSystem)에
    // 걸리는지, 스턴 중 속도가 0인지처럼 "막기"와 무관한 스턴 자체의 동작만 다룬다.
    public class FlappyWorldStunTests
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
        public void 맵에_닿으면_막히고_스턴에_걸린다()
        {
            var world = FlappyWorldFixture.Create(new AlwaysHit(), out var bird);
            world.GameplayStartTick = 0;   // 이 파일은 스턴을 다룬다, 출발 게이트가 아니다
            Vector3 before = bird.Get<GameFramework.World.Transform>().Position.ToUnity();

            world.Tick(1, 0.02f);

            // 막힌다 — 거리 0인 벽이라 위치는 그대로다(FlappyWorldSolidMapTests가 이 회귀를 더 깊이 다룬다).
            Vector3 after = bird.Get<GameFramework.World.Transform>().Position.ToUnity();
            Assert.That(after.x, Is.EqualTo(before.x));
            // 그리고 스턴에 걸렸다.
            Assert.That(bird.Get<LOP.FlappyStun>().StunRemaining, Is.GreaterThan(0f));
        }

        [Test]
        public void 스턴_중에는_속도가_0이다()
        {
            var world = FlappyWorldFixture.Create(new AlwaysHit(), out var bird);
            world.GameplayStartTick = 0;   // 이 파일은 스턴을 다룬다, 출발 게이트가 아니다
            // 스턴 진입 전에 눈에 띄는 0 아닌 속도를 심어 둔다 — 새가 아예 수집조차 안 됐다면
            // (예: EntityKind 누락) 이 값이 그대로 남아 아래 "0이어야 한다" 단언이 그 경우를
            // 잡아낸다. 실제 경로에서는 tick1의 MoveSystem이 스턴 진입보다 먼저 이 값을 자기
            // 계산값으로 덮어쓰므로(진입은 MoveBlockedByMap에서 그 다음에 일어난다) 최종 결과에는
            // 영향이 없다 — tick2에서 IsStunned 가드가 무조건 0으로 만든다.
            bird.Get<GameFramework.World.Velocity>().Linear = new Vector3(5f, 5f, 5f).ToNumerics();

            world.Tick(1, 0.02f);   // 스턴 진입

            world.Tick(2, 0.02f);

            Vector3 velocity = bird.Get<GameFramework.World.Velocity>().Linear.ToUnity();
            Assert.That(velocity, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void 맵에_안_닿으면_평소대로_전진한다()
        {
            var world = FlappyWorldFixture.Create(new NeverHit(), out var bird);
            world.GameplayStartTick = 0;   // 이 파일은 스턴을 다룬다, 출발 게이트가 아니다

            world.Tick(1, 0.02f);

            Assert.That(bird.Get<LOP.FlappyStun>().StunRemaining, Is.EqualTo(0f));
            Assert.That(bird.Get<GameFramework.World.Velocity>().Linear.X, Is.GreaterThan(0f));
        }
    }
}
