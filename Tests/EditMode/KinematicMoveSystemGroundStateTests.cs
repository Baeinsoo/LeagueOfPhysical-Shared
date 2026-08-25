using NUnit.Framework;
using UnityEngine;
using GameFramework.Physics;
using GameFramework.World;

namespace LOP.Tests
{
    public class KinematicMoveSystemGroundStateTests
    {
        // 바닥에 닿는 상황 — 아래로 쓸면 즉시(거리 0) 막힌다.
        private class GroundedQuery : ICollisionQuery
        {
            public CollisionHit CapsuleCast(Vector3 point1, Vector3 point2, float radius,
                                            Vector3 direction, float distance, int layerMask)
            {
                if (direction.y < 0f)
                {
                    return new CollisionHit(true, 0f, Vector3.up, point1, null);
                }
                return CollisionHit.None;
            }

            public CollisionHit Raycast(Vector3 origin, Vector3 direction, float distance, int layerMask)
                => CollisionHit.None;

            public CollisionHit[] OverlapSphere(Vector3 center, float radius, int layerMask)
                => System.Array.Empty<CollisionHit>();
        }

        // 아무것도 막지 않음 — 공중.
        private class EmptyQuery : ICollisionQuery
        {
            public CollisionHit CapsuleCast(Vector3 point1, Vector3 point2, float radius,
                                            Vector3 direction, float distance, int layerMask)
                => CollisionHit.None;

            public CollisionHit Raycast(Vector3 origin, Vector3 direction, float distance, int layerMask)
                => CollisionHit.None;

            public CollisionHit[] OverlapSphere(Vector3 center, float radius, int layerMask)
                => System.Array.Empty<CollisionHit>();
        }

        private static Entity MakeCharacter()
        {
            var entity = new Entity("c1");
            entity.Add(new GameFramework.World.Transform());
            entity.Add(new Velocity());
            entity.Add(new GroundState());
            entity.Add(new GameFramework.World.CapsuleShape(0.35f, 1.5f));
            return entity;
        }

        [Test]
        public void WritesGroundedTrueWhenDownwardSweepBlocked()
        {
            var entity = MakeCharacter();
            var system = new KinematicMoveSystem(new GroundedQuery(), layerMask: ~0);

            system.Tick(entity, 0.02f);

            Assert.IsTrue(entity.Get<GroundState>().IsGrounded);
        }

        [Test]
        public void WritesGroundedFalseWhenNothingBlocks()
        {
            var entity = MakeCharacter();
            entity.Get<GroundState>().IsGrounded = true;   // 이전 틱 잔재
            var system = new KinematicMoveSystem(new EmptyQuery(), layerMask: ~0);

            system.Tick(entity, 0.02f);

            Assert.IsFalse(entity.Get<GroundState>().IsGrounded);
        }

        [Test]
        public void DoesNotThrowWhenGroundStateAbsent()
        {
            var entity = new Entity("c2");
            entity.Add(new GameFramework.World.Transform());
            entity.Add(new Velocity());
            entity.Add(new GameFramework.World.CapsuleShape(0.35f, 1.5f));
            var system = new KinematicMoveSystem(new EmptyQuery(), layerMask: ~0);

            Assert.DoesNotThrow(() => system.Tick(entity, 0.02f));
        }
    }
}
