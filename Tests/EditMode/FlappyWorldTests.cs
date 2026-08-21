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

        static FlappyConfig Config()
            => new FlappyConfig(forwardSpeed: 11f, flapImpulse: 23f, gravity: 70f, maxFallSpeed: 30f,
                                bodyRadius: 0.45f, bodyHeight: 0.9f, restitution: 0.35f);

        static Entity Bird(string id, Vector3 position, bool simulated)
        {
            var entity = new Entity(id);
            entity.Add(new GameFramework.World.Transform { Position = position.ToNumerics() });
            entity.Add(new Velocity());
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
                               new EmptySkyQuery(), bridge, Config(), layerMask: ~0);

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
    }
}
