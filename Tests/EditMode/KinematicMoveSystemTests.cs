using System.Collections.Generic;
using GameFramework;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class KinematicMoveSystemTests
    {
        const float Tolerance = 1e-3f;
        const float Dt = 0.1f;
        const float Gravity = -9.81f * 2f;   // KinematicMoveSystem의 중력 상수와 같은 값

        private class FakeCollisionQuery : ICollisionQuery
        {
            public readonly Queue<CollisionHit> Responses = new Queue<CollisionHit>();
            public CollisionHit CapsuleCast(Vector3 p1, Vector3 p2, float radius,
                Vector3 dir, float dist, int mask)
                => Responses.Count > 0 ? Responses.Dequeue() : CollisionHit.None;
        }

        private static GameFramework.World.Entity Entity(Vector3 pos, Vector3 vel)
        {
            var e = new GameFramework.World.Entity("e1");
            e.Add(new GameFramework.World.Transform { Position = pos.ToNumerics() });
            e.Add(new GameFramework.World.Velocity { Linear = vel.ToNumerics() });
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
    }
}
