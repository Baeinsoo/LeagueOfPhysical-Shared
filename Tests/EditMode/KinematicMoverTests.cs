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

        // 평평한 지면(Plane)을 흉내내는 쿼리 — 캐스트 위치에 따라 응답한다(스크립트 큐와 달리 지오메트리 반영).
        // 수평 캐스트: 캡슐 바닥이 지면(GroundY)에 닿아있으면 grazing 히트(버그 트리거), 띄워졌으면 None.
        // 아래 캐스트: 지면까지 거리를 돌려줌.
        private class GroundPlaneQuery : ICollisionQuery
        {
            public float GroundY = 0f;

            public CollisionHit CapsuleCast(Vector3 p1, Vector3 p2, float radius,
                Vector3 direction, float distance, int layerMask)
            {
                float bottom = Mathf.Min(p1.y, p2.y) - radius;   // 캡슐 최하단
                if (direction.y < -0.5f)   // 아래로 캐스트 → 지면까지
                {
                    float d = bottom - GroundY;
                    return d <= distance ? new CollisionHit(true, Mathf.Max(d, 0f), Vector3.up, Vector3.zero)
                                         : CollisionHit.None;
                }
                // 수평 캐스트: 바닥에 붙어있으면 지면과 grazing → 히트, 띄워졌으면 안 맞음
                return bottom <= GroundY + 1e-4f
                    ? new CollisionHit(true, 0f, Vector3.up, Vector3.zero)
                    : CollisionHit.None;
            }
        }

        private static KinematicMoveInput Input(Vector3 pos, Vector3 vel, float dt = 0.1f)
            => new KinematicMoveInput(pos, vel, 0.35f, 1.5f, dt, ~0);

        [Test]
        public void GroundedHorizontalMove_MovesAlongGround_NotBlockedByFloor()
        {
            // 지면(y=0) 위, 수평 속도 + 중력. 바닥에 발이 붙어 있어도 앞으로 걸어야 한다(현재 버그: 못 감).
            var query = new GroundPlaneQuery { GroundY = 0f };
            var r = KinematicMover.Move(Input(Vector3.zero, new Vector3(10f, -20f, 0f)), query);

            Assert.That(r.position.x, Is.GreaterThan(0.5f), "바닥 위에서 수평 이동이 막히면 안 됨");
            Assert.IsTrue(r.grounded, "바닥 접촉이면 grounded");
            Assert.That(r.velocity.y, Is.EqualTo(0f).Within(Tolerance), "바닥에서 수직 속도 소멸");
        }

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

        [Test]
        public void AngledWall_SlidesAlong_RedirectsVelocity()
        {
            var query = new FakeCollisionQuery();
            // 45도 벽: 법선=(-0.7071,0,-0.7071). 접촉 후 남은 이동이 벽면을 따라 미끄러짐.
            query.Responses.Enqueue(new CollisionHit(true, 0.3f,
                new Vector3(-0.7071f, 0f, -0.7071f), Vector3.zero));
            // 두 번째 sweep은 열림(None) → 미끄러진 나머지를 그대로 이동
            var r = KinematicMover.Move(Input(Vector3.zero, new Vector3(10f, 0f, 0f)), query);

            // x로 진행하다 벽을 만나 z로도 미끄러짐(수직축 이동 발생)
            Assert.That(r.position.z, Is.LessThan(-0.01f), "벽면을 따라 옆으로 미끄러져야 함");
            Assert.That(r.position.x, Is.GreaterThan(0.01f));
            // 정면 벽처럼 소멸이 아니라 벽면 방향으로 꺾임(속도 크기 유지)
            Assert.That(new Vector3(r.velocity.x, 0f, r.velocity.z).magnitude, Is.GreaterThan(0.1f),
                "각진 벽에서는 속도가 소멸이 아니라 재방향되어야 함");
        }

        [Test]
        public void GroundHit_SetsGrounded_AndZeroesVerticalVelocity()
        {
            var query = new FakeCollisionQuery();
            // 아래로 낙하 중 바닥(법선 위) 접촉
            query.Responses.Enqueue(new CollisionHit(true, 0.1f, new Vector3(0f, 1f, 0f), Vector3.zero));
            var r = KinematicMover.Move(Input(Vector3.zero, new Vector3(0f, -20f, 0f)), query);

            Assert.IsTrue(r.grounded, "바닥 법선(위쪽) 접촉 시 grounded");
            Assert.That(r.velocity.y, Is.EqualTo(0f).Within(Tolerance), "바닥에 닿으면 수직 속도 소멸");
        }

        [Test]
        public void AlwaysBlocked_TerminatesWithinMaxSlides()
        {
            var query = new FakeCollisionQuery();
            // 매 sweep마다 같은 각진 벽 → 잔여가 계속 남아도 상한(MaxSlides) 내에서 종료해야 함
            for (int i = 0; i < 20; i++)
            {
                query.Responses.Enqueue(new CollisionHit(true, 0.1f,
                    new Vector3(-0.7071f, 0f, -0.7071f), Vector3.zero));
            }
            var r = KinematicMover.Move(Input(Vector3.zero, new Vector3(10f, 0f, 0f)), query);

            Assert.That(query.CallCount, Is.LessThanOrEqualTo(4), "MaxSlides 상한 내에서 종료(무한루프 방지)");
        }
    }
}
