using GameFramework;
using GameFramework.Physics;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    // 이 슬라이스가 존재하는 이유 — 클라 예측과 서버 권위가 같은 구체 코드를 돌려 같은 결과를
    // 내야 한다. 같은 입력을 두 월드에 넣고 같은 틱 수만큼 돌리면 새들의 최종 위치·속도가
    // 정확히 같아야 하고, 레지스트리에 등록한 순서가 그 결과를 바꾸면 안 된다
    // (CollectBirds가 id로 다시 정렬하기 때문).
    public class FlappyWorldDeterminismTests
    {
        // 아무데도 안 부딪히는 빈 하늘 — 몸싸움(순서 의존)만 보고 싶을 때 맵 충돌은 빼 둔다.
        private class EmptySkyQuery : ICollisionQuery
        {
            public CollisionHit CapsuleCast(Vector3 point1, Vector3 point2, float radius,
                Vector3 direction, float distance, int layerMask) => CollisionHit.None;

            public CollisionHit Raycast(Vector3 origin, Vector3 direction, float distance, int layerMask)
                => CollisionHit.None;

            public CollisionHit[] OverlapSphere(Vector3 center, float radius, int layerMask)
                => System.Array.Empty<CollisionHit>();
        }

        // 물리 바디가 없는 EditMode 테스트라 브릿지는 아무 일도 하지 않는다.
        private class NoopMotionBridge : IMotionBridge
        {
            public void SyncTransforms() { }
            public void Depenetrate(Entity entity) { }
            public void Separate(Entity entity) { }
            public void PushMotion(Entity entity) { }
        }

        static FlappyConfig Config()
            => new FlappyConfig(forwardSpeed: 11f, flapImpulse: 23f, gravity: 70f, maxFallSpeed: 30f,
                                bodyRadius: 0.45f, bodyHeight: 0.9f, restitution: 0.35f,
                                ghostTime: 0.8f, invulnTime: 0.6f);

        static Entity Bird(string id, Vector3 position, float initialVy)
        {
            var entity = new Entity(id);
            entity.Add(new GameFramework.World.Transform { Position = position.ToNumerics() });
            entity.Add(new Velocity { Linear = new Vector3(0f, initialVy, 0f).ToNumerics() });
            entity.Add(new CapsuleShape(0.45f, 0.9f));
            entity.Add(new Simulated());
            return entity;
        }

        static FlappyWorld World(EntityRegistry registry)
            => new FlappyWorld(registry, new WorldEventBuffer(),
                               new FlappyMoveSystem(Config()),
                               new FlappyBodyCollisionSystem(Config()),
                               new FlappyGhostSystem(Config()),
                               new EmptySkyQuery(), new NoopMotionBridge(), layerMask: ~0);

        // 세 마리가 서로 겹치는 삼각형 배치 + 서로 다른 초기 세로속도.
        // 몸싸움이 세 쌍(1-2, 1-3, 2-3) 전부에서 실제로 걸리게 한 뒤, 쌍을 순서대로 푸는 과정에서
        // 나중 쌍이 앞선 쌍이 이미 바꾼 위치·속도를 보게 만든다 — 그래서 처리 순서가 결과를 가른다.
        static void PopulateOverlappingBirds(EntityRegistry registry, params string[] idsInRegistrationOrder)
        {
            var byId = new System.Collections.Generic.Dictionary<string, (Vector3 pos, float vy)>
            {
                ["bird-1"] = (new Vector3(0f, 0f, 0f), 0f),
                ["bird-2"] = (new Vector3(0.3f, 0.2f, 0f), -1f),
                ["bird-3"] = (new Vector3(0.15f, -0.15f, 0.25f), 2f),
            };
            foreach (var id in idsInRegistrationOrder)
            {
                var (pos, vy) = byId[id];
                registry.Add(Bird(id, pos, vy));
            }
        }

        [Test]
        public void 등록_순서가_달라도_결과는_정확히_같다()
        {
            var forward = new EntityRegistry();
            PopulateOverlappingBirds(forward, "bird-1", "bird-2", "bird-3");

            var reversed = new EntityRegistry();
            PopulateOverlappingBirds(reversed, "bird-3", "bird-2", "bird-1");

            var worldForward = World(forward);
            var worldReversed = World(reversed);
            for (long tick = 1; tick <= 5; tick++)
            {
                worldForward.Tick(tick, 0.1f);
                worldReversed.Tick(tick, 0.1f);
            }

            foreach (var id in new[] { "bird-1", "bird-2", "bird-3" })
            {
                var a = forward.Get(id);
                var b = reversed.Get(id);

                // 부동소수 오차가 아니라 정확히 같아야 한다 — 같은 코드가 같은 입력을 같은
                // 순서로 처리하면 비트까지 같다. 톨러런스를 두면 이 테스트의 요점이 사라진다.
                Assert.AreEqual(a.Get<GameFramework.World.Transform>().Position,
                                 b.Get<GameFramework.World.Transform>().Position,
                                 $"{id}: position diverged by registration order");
                Assert.AreEqual(a.Get<Velocity>().Linear, b.Get<Velocity>().Linear,
                                 $"{id}: velocity diverged by registration order");
            }
        }
    }
}
