using System.Collections.Generic;
using GameFramework;
using GameFramework.Physics;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class FlappyWorldTests
    {
        const float Tolerance = 1e-3f;

        // 아무데도 안 부딪히는 빈 하늘 — 맵 충돌 없이 이동 계산만 보고 싶을 때.
        private class EmptySkyQuery : ICollisionQuery
        {
            public CollisionHit CapsuleCast(Vector3 point1, Vector3 point2, float radius,
                Vector3 direction, float distance, int layerMask) => CollisionHit.None;

            public CollisionHit Raycast(Vector3 origin, Vector3 direction, float distance, int layerMask)
                => CollisionHit.None;

            public CollisionHit[] OverlapSphere(Vector3 center, float radius, int layerMask)
                => System.Array.Empty<CollisionHit>();
        }

        // 물리 바디가 아직 없는 단계라 브릿지는 아무 일도 하지 않는다(호출 여부만 세어 둔다).
        private class NoopMotionBridge : GameFramework.World.IMotionBridge
        {
            public int SyncTransformsCalls;
            public int SeparateCalls;

            public void SyncTransforms() => SyncTransformsCalls++;
            public void Depenetrate(Entity entity) { }
            public void Separate(Entity entity) => SeparateCalls++;
            public void PushMotion(Entity entity) { }
        }

        // 수평 방향 sweep에만 고정 거리로 맞는 벽 — 수직(중력) sweep까지 맞히면 낙하가 막혀
        // 계산이 복잡해지니 벽은 수평 전용으로 한정한다. 받은 인자를 기록해 phase ③이 실제로
        // config 값(반지름)·월드에 넘긴 레이어마스크를 쓰는지 검증할 수 있게 한다.
        private class WallAheadQuery : ICollisionQuery
        {
            private readonly float _hitDistance;
            private readonly Vector3 _normal;

            public float LastRadius;
            public int LastLayerMask;
            public int HorizontalCastCount;

            public WallAheadQuery(float hitDistance, Vector3 normal)
            {
                _hitDistance = hitDistance;
                _normal = normal;
            }

            public CollisionHit CapsuleCast(Vector3 point1, Vector3 point2, float radius,
                Vector3 direction, float distance, int layerMask)
            {
                if (!Mathf.Approximately(direction.y, 0f))
                {
                    return CollisionHit.None;   // 수직 sweep(중력) — 벽이 막을 방향이 아니다
                }

                LastRadius = radius;
                LastLayerMask = layerMask;
                HorizontalCastCount++;
                return new CollisionHit(true, _hitDistance, _normal, point1 + direction * _hitDistance, null);
            }

            public CollisionHit Raycast(Vector3 origin, Vector3 direction, float distance, int layerMask)
                => CollisionHit.None;

            public CollisionHit[] OverlapSphere(Vector3 center, float radius, int layerMask)
                => System.Array.Empty<CollisionHit>();
        }

        static FlappyConfig Config()
            => new FlappyConfig(forwardSpeed: 11f, flapImpulse: 23f, gravity: 70f, maxFallSpeed: 30f,
                                bodyRadius: 0.45f, bodyHeight: 0.9f, restitution: 0.35f);

        static Entity Bird(string id, Vector3 position, bool simulated, float radius = 0.45f, float height = 0.9f)
        {
            var entity = new Entity(id);
            entity.Add(new GameFramework.World.Transform { Position = position.ToNumerics() });
            entity.Add(new Velocity());
            entity.Add(new CapsuleShape(radius, height));
            if (simulated)
            {
                entity.Add(new Simulated());
            }
            return entity;
        }

        static FlappyWorld World(EntityRegistry registry, GameFramework.World.IMotionBridge bridge)
            => new FlappyWorld(registry, new WorldEventBuffer(),
                               new FlappyMoveSystem(Config()),
                               new FlappyBodyCollisionSystem(Config()),
                               new EmptySkyQuery(), bridge, layerMask: ~0);

        static Vector3 PositionOf(Entity e) => e.Get<GameFramework.World.Transform>().Position.ToUnity();
        static Vector3 VelocityOf(Entity e) => e.Get<Velocity>().Linear.ToUnity();

        [Test]
        public void 한_틱이면_전진하면서_중력만큼_떨어진다()
        {
            var registry = new EntityRegistry();
            var bird = Bird("bird-1", Vector3.zero, simulated: true);
            registry.Add(bird);

            World(registry, new NoopMotionBridge()).Tick(1, 0.1f);

            Assert.AreEqual(11f, VelocityOf(bird).x, Tolerance);    // 고정 전진
            Assert.AreEqual(-7f, VelocityOf(bird).y, Tolerance);    // 70 × 0.1
            Assert.AreEqual(1.1f, PositionOf(bird).x, Tolerance);   // 11 × 0.1
            Assert.AreEqual(-0.7f, PositionOf(bird).y, Tolerance);  // 7 × 0.1
        }

        [Test]
        public void 시뮬_대상이_아닌_엔티티는_건드리지_않는다()
        {
            var registry = new EntityRegistry();
            var remote = Bird("bird-2", Vector3.zero, simulated: false);
            registry.Add(remote);

            World(registry, new NoopMotionBridge()).Tick(1, 0.1f);

            // 남의 새는 예측하지 않고 서버 스냅샷 보간에 맡긴다
            Assert.AreEqual(Vector3.zero, PositionOf(remote));
            Assert.AreEqual(Vector3.zero, VelocityOf(remote));
        }

        [Test]
        public void 겹쳐_있던_두_새는_이동_전에_갈라진다()
        {
            var registry = new EntityRegistry();
            var lower = Bird("bird-1", Vector3.zero, simulated: true);
            var upper = Bird("bird-2", new Vector3(0f, 0.5f, 0f), simulated: true);
            registry.Add(lower);
            registry.Add(upper);

            World(registry, new NoopMotionBridge()).Tick(1, 0.1f);

            // 몸싸움으로 위아래 0.39만큼 갈라진 뒤 각자 이동한다 — 서로 파고든 채로 남지 않는다
            Assert.Greater(PositionOf(upper).y - PositionOf(lower).y, 0.5f);
        }

        [Test]
        public void 새끼리_밀어내기를_물리엔진에_맡기지_않는다()
        {
            var registry = new EntityRegistry();
            registry.Add(Bird("bird-1", Vector3.zero, simulated: true));
            var bridge = new NoopMotionBridge();

            World(registry, bridge).Tick(1, 0.1f);

            Assert.AreEqual(0, bridge.SeparateCalls);       // 겹침은 우리 계산이 이미 풀었다
            Assert.AreEqual(1, bridge.SyncTransformsCalls); // 옮긴 자리는 물리에 알려 준다
        }

        [Test]
        public void 맵_콜라이더에_막히면_벽까지만_전진하고_수평속도가_깎인다()
        {
            var registry = new EntityRegistry();
            var bird = Bird("bird-1", Vector3.zero, simulated: true);
            registry.Add(bird);

            var wallQuery = new WallAheadQuery(hitDistance: 0.5f, normal: Vector3.left);
            const int layerMask = 1 << 5;   // ~0처럼 아무 값이나 통과하는 마스크가 아니라 실제로 넘겨지는지 구분할 값
            var world = new FlappyWorld(registry, new WorldEventBuffer(),
                                         new FlappyMoveSystem(Config()),
                                         new FlappyBodyCollisionSystem(Config()),
                                         wallQuery, new NoopMotionBridge(), layerMask);

            world.Tick(1, 0.1f);

            // 안 막혔으면 x=1.1(11×0.1)까지 갔을 것. 0.5m 앞 벽에 막혀 0.48(=0.5-SkinWidth)에서 멈춘다.
            Assert.AreEqual(0.48f, PositionOf(bird).x, Tolerance);
            Assert.AreEqual(0f, VelocityOf(bird).x, Tolerance);   // 벽 법선(-x)이 전진 속도를 그대로 깎는다

            Assert.AreEqual(1, wallQuery.HorizontalCastCount);
            Assert.AreEqual(Config().BodyRadius, wallQuery.LastRadius, Tolerance);
            Assert.AreEqual(layerMask, wallQuery.LastLayerMask);
        }

        [Test]
        public void 맵_sweep은_엔티티가_들고_있는_몸_치수를_쓴다()
        {
            var registry = new EntityRegistry();
            // 튜닝값(0.45)과 일부러 다른 몸을 준다 — 어느 쪽을 읽는지 구분되는 값이어야 한다.
            var bird = Bird("bird-1", Vector3.zero, simulated: true, radius: 0.2f, height: 0.4f);
            registry.Add(bird);

            var wallQuery = new WallAheadQuery(hitDistance: 0.5f, normal: Vector3.left);
            var world = new FlappyWorld(registry, new WorldEventBuffer(),
                                        new FlappyMoveSystem(Config()),
                                        new FlappyBodyCollisionSystem(Config()),
                                        wallQuery, new NoopMotionBridge(), layerMask: ~0);

            world.Tick(1, 0.1f);

            Assert.AreEqual(0.2f, wallQuery.LastRadius, Tolerance);
        }

        [Test]
        public void 몸이_없는_엔티티는_맵_이동을_하지_않는다()
        {
            var registry = new EntityRegistry();
            var noBody = new Entity("bird-1");
            noBody.Add(new GameFramework.World.Transform());
            noBody.Add(new Velocity());
            noBody.Add(new Simulated());
            registry.Add(noBody);

            // 속도는 정해지지만(전진·중력) 위치를 옮기는 단계는 몸 없이는 돌 수 없다.
            World(registry, new NoopMotionBridge()).Tick(1, 0.1f);

            Assert.AreEqual(Vector3.zero, PositionOf(noBody));
        }
    }
}
