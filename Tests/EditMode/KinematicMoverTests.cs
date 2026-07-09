using System.Collections.Generic;
using GameFramework;
using LOP;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class KinematicMoverTests
    {
        const float Tolerance = 1e-3f;

        // 스크립트된 충돌 응답을 순서대로 돌려주는 테스트용 쿼리(씬 없이 collide-and-slide 로직만 검증).
        private class FakeCollisionQuery : ICollisionQuery
        {
            public readonly Queue<CollisionHit> Responses = new Queue<CollisionHit>();
            public int CallCount;

            public CollisionHit CapsuleCast(Vector3 point1, Vector3 point2, float radius,
                Vector3 direction, float distance, int layerMask)
            {
                CallCount++;
                return Responses.Count > 0 ? Responses.Dequeue() : CollisionHit.None;
            }
        }

        private static KinematicMoveInput Input(Vector3 pos, Vector3 vel, float dt = 0.1f)
            => new KinematicMoveInput(pos, vel, 0.35f, 1.5f, dt, ~0);

        [Test]
        public void NoHit_MovesFullDelta()
        {
            var query = new FakeCollisionQuery();   // 응답 없음 → 항상 None
            var r = KinematicMover.Move(Input(Vector3.zero, new Vector3(10f, 0f, 0f)), query);

            // delta = velocity*dt = (1,0,0)
            Assert.That(r.position.x, Is.EqualTo(1f).Within(Tolerance));
            Assert.That(r.position.y, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(r.position.z, Is.EqualTo(0f).Within(Tolerance));
            Assert.IsFalse(r.grounded);
        }

        [Test]
        public void HeadOnWall_StopsAndZeroesVelocityAlongNormal()
        {
            var query = new FakeCollisionQuery();
            // 정면 벽: 거리 0.5에서 법선이 이동 반대(-x)
            query.Responses.Enqueue(new CollisionHit(true, 0.5f, new Vector3(-1f, 0f, 0f), Vector3.zero));
            var r = KinematicMover.Move(Input(Vector3.zero, new Vector3(10f, 0f, 0f)), query);

            // 접촉 전까지만(≈0.48, skin 여유), 목표(1.0)까지 안 감
            Assert.That(r.position.x, Is.GreaterThan(0.4f));
            Assert.That(r.position.x, Is.LessThan(0.6f));
            // 벽 법선 방향 속도 소멸
            Assert.That(r.velocity.x, Is.EqualTo(0f).Within(Tolerance));
        }
    }
}
