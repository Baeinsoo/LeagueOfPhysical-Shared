using System.Collections.Generic;
using GameFramework;
using GameFramework.Physics;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class KinematicMoveSystemTests
    {
        const float Tolerance = 1e-3f;
        const float Dt = 0.1f;
        const float Gravity = -9.81f * 2f;   // KinematicMoveSystem의 중력 상수와 같은 값

        //  스크립트된 충돌 응답을 돌려주는 테스트용 쿼리(KinematicMoverTests.cs와 같은 모양).
        //  수평/수직 큐를 나눈 이유도 같다 — 커널이 이동 전에 발밑을 훑는 지면 탐침을 한 번
        //  더 쏘므로, 큐가 하나면 그 탐침이 수직용 응답을 먼저 먹어 버린다.
        private class FakeCollisionQuery : ICollisionQuery
        {
            public readonly Queue<CollisionHit> Horizontal = new Queue<CollisionHit>();
            public readonly Queue<CollisionHit> Vertical = new Queue<CollisionHit>();

            public CollisionHit CapsuleCast(Vector3 p1, Vector3 p2, float radius,
                Vector3 dir, float dist, int mask)
                => Mathf.Abs(dir.y) > 0.5f ? Take(Vertical, dist) : Take(Horizontal, dist);

            //  실제 sweep은 요청한 거리 밖의 것을 못 본다 — 스크립트 응답도 같게 다룬다.
            private static CollisionHit Take(Queue<CollisionHit> queue, float distance)
            {
                if (queue.Count == 0 || queue.Peek().Distance > distance)
                {
                    return CollisionHit.None;
                }
                return queue.Dequeue();
            }

            public CollisionHit Raycast(Vector3 origin, Vector3 direction, float distance, int layerMask)
                => CollisionHit.None;

            public CollisionHit[] OverlapSphere(Vector3 center, float radius, int layerMask)
                => System.Array.Empty<CollisionHit>();
        }

        private static GameFramework.World.Entity Entity(Vector3 pos, Vector3 vel)
        {
            var e = new GameFramework.World.Entity("e1");
            e.Add(new GameFramework.World.Transform { Position = pos.ToNumerics() });
            e.Add(new GameFramework.World.Velocity { Linear = vel.ToNumerics() });
            e.Add(new GameFramework.World.CapsuleShape(0.35f, 1.5f));
            return e;
        }

        [Test]
        public void Gravity_PullsDown_WhenAirborne()
        {
            var sys = new KinematicMoveSystem(new FakeCollisionQuery(), ~0);   // 응답 없음 → 자유낙하
            var e = Entity(new Vector3(0f, 10f, 0f), Vector3.zero);

            sys.Tick(e, Dt);

            var v = e.Get<GameFramework.World.Velocity>().Linear.ToUnity();
            var p = e.Get<GameFramework.World.Transform>().Position.ToUnity();
            Assert.That(v.y, Is.EqualTo(Gravity * Dt).Within(Tolerance), "중력만큼 수직 속도 감소");
            Assert.That(p.y, Is.LessThan(10f), "아래로 이동");
        }

        [Test]
        public void Ground_StopsFall_ZeroesVerticalVelocity()
        {
            var q = new FakeCollisionQuery();
            // 바닥(법선 위). 거리 0.05는 지면 탐침(0.07까지 봄)에도, 실제 수직 스텝에도 잡힌다 —
            // 큐는 한 번 꺼내면 사라지지만 진짜 바닥은 두 번 다 그 자리에 있으므로 같은 응답을 둘 다에 준다.
            q.Vertical.Enqueue(new CollisionHit(true, 0.05f, new Vector3(0f, 1f, 0f), Vector3.zero, null));
            q.Vertical.Enqueue(new CollisionHit(true, 0.05f, new Vector3(0f, 1f, 0f), Vector3.zero, null));
            var sys = new KinematicMoveSystem(q, ~0);
            var e = Entity(new Vector3(0f, 0.1f, 0f), Vector3.zero);

            sys.Tick(e, Dt);

            var v = e.Get<GameFramework.World.Velocity>().Linear.ToUnity();
            Assert.That(v.y, Is.EqualTo(0f).Within(Tolerance), "바닥 접지 시 수직 속도 소멸");
        }

        [Test]
        public void HorizontalVelocity_MovesPosition_WhenClear()
        {
            var sys = new KinematicMoveSystem(new FakeCollisionQuery(), ~0);   // 무충돌
            var e = Entity(Vector3.zero, new Vector3(5f, 0f, 0f));

            sys.Tick(e, Dt);

            var p = e.Get<GameFramework.World.Transform>().Position.ToUnity();
            Assert.That(p.x, Is.EqualTo(0.5f).Within(Tolerance), "수평 5 × dt 0.1 = 0.5 이동");
        }

        [Test]
        public void NoTransformOrVelocity_DoesNotThrow()
        {
            var sys = new KinematicMoveSystem(new FakeCollisionQuery(), ~0);
            var e = new GameFramework.World.Entity("e2");   // Transform/Velocity 없음
            Assert.DoesNotThrow(() => sys.Tick(e, Dt));
        }
    }
}
