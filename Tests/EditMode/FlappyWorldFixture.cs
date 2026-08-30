using GameFramework.Physics;
using GameFramework.World;

namespace LOP.Tests
{
    /// <summary>
    /// FlappyWorld를 조립하는 공용 테스트 픽스처. 스턴(Task3)·저장복원(Task4)·원격 새 보간(Task10)
    /// 테스트가 같은 조립 코드를 반복해서 베끼지 않도록 여기 한 곳에 둔다.
    /// </summary>
    internal static class FlappyWorldFixture
    {
        public static FlappyConfig Config()
            => new FlappyConfig(forwardSpeed: 11f, flapImpulse: 23f, gravity: 70f, maxFallSpeed: 30f,
                                bodyRadius: 0.45f, bodyHeight: 0.9f, restitution: 0.35f,
                                stunTime: 0.8f, invulnTime: 0.6f);

        /// <summary>물리 바디가 없는 EditMode 테스트라 아무 일도 하지 않는 빈 구현.</summary>
        public class NoopMotionBridge : IMotionBridge
        {
            public void SyncTransforms() { }
            public System.Numerics.Vector3 Depenetrate(Entity entity) => System.Numerics.Vector3.Zero;
            public void Separate(Entity entity) { }
            public void PushMotion(Entity entity) { }
        }

        /// <summary>새를 맵에 부딪혀 스턴으로 몰아넣고 싶을 때 쓰는 스텁 — 항상 맞았다고 답한다.</summary>
        public class AlwaysHit : ICollisionQuery
        {
            public CollisionHit CapsuleCast(UnityEngine.Vector3 p1, UnityEngine.Vector3 p2, float radius,
                UnityEngine.Vector3 direction, float distance, int layerMask)
                => new CollisionHit(true, 0f, UnityEngine.Vector3.up, p1, null);

            public CollisionHit Raycast(UnityEngine.Vector3 origin, UnityEngine.Vector3 direction, float distance, int layerMask)
                => CollisionHit.None;

            public CollisionHit[] OverlapSphere(UnityEngine.Vector3 center, float radius, int layerMask)
                => System.Array.Empty<CollisionHit>();

        }

        /// <summary>
        /// 새 한 마리를 구성한다. <paramref name="simulated"/>가 false면 원격(남의 새) — 시뮬 대상이
        /// 아니라 스냅샷 보간으로만 움직이는 쪽을 흉내낼 때 쓴다(Task10).
        /// </summary>
        public static Entity Bird(string id, bool simulated = true, bool withInput = true)
        {
            var entity = new Entity(id);
            entity.Add(new GameFramework.World.Transform());
            entity.Add(new Velocity());
            entity.Add(new CapsuleShape(Config().BodyRadius, Config().BodyHeight));
            entity.Add(new EntityKind(EntityType.Character));   // FlappyWorld.CollectBirds가 이걸로 "새"를 가린다
            entity.Add(new FlappyStun());
            if (withInput)
            {
                entity.Add(new InputBuffer());
            }
            if (simulated)
            {
                entity.Add(new Simulated());
            }
            return entity;
        }

        /// <summary>기본 조립 — 새 한 마리(시뮬 대상) + 지정한 맵 충돌 쿼리. 물리 브릿지는 no-op.</summary>
        public static FlappyWorld Create(ICollisionQuery collisionQuery, out Entity bird)
            => Create(collisionQuery, new NoopMotionBridge(), out bird);

        /// <summary>물리 브릿지 호출 여부까지 확인하고 싶을 때(예: Depenetrate 카운트) 쓰는 오버로드.</summary>
        public static FlappyWorld Create(ICollisionQuery collisionQuery, IMotionBridge motionBridge, out Entity bird)
        {
            var registry = new EntityRegistry();
            bird = Bird("bird-1");
            registry.Add(bird);
            return Build(registry, collisionQuery, motionBridge);
        }

        /// <summary>
        /// 내 새(시뮬 대상) 하나 + 원격 새(시뮬 대상 아님) 하나를 함께 등록해 조립한다.
        /// 원격 새는 InputBuffer도 없다 — 서버가 보내주는 스냅샷만 받는 쪽이라 입력을 낼 일이 없다.
        /// </summary>
        public static FlappyWorld CreateWithRemoteBird(
            ICollisionQuery collisionQuery, out Entity localBird, out Entity remoteBird)
        {
            var registry = new EntityRegistry();
            localBird = Bird("bird-1", simulated: true);
            remoteBird = Bird("bird-2", simulated: false, withInput: false);
            registry.Add(localBird);
            registry.Add(remoteBird);
            return Build(registry, collisionQuery, new NoopMotionBridge());
        }

        private static FlappyWorld Build(EntityRegistry registry, ICollisionQuery collisionQuery, IMotionBridge motionBridge)
            => new FlappyWorld(registry, new WorldEventBuffer(),
                new FlappyMoveSystem(Config()),
                new FlappyStunSystem(Config()), collisionQuery, motionBridge, layerMask: ~0);
    }
}
