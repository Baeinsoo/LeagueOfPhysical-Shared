using System.Collections.Generic;
using GameFramework.Physics;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 반평면(바닥·벽·경사)으로 맵을 흉내내는 테스트용 충돌 쿼리.
    /// 스크립트된 응답 큐(<c>FakeCollisionQuery</c>)와 달리 실제 지오메트리라서
    /// "이동한 결과가 면 안쪽인가" 같은 위치 기반 검증이 가능하다 — 경사 파묻힘 재현이 그것이다.
    /// </summary>
    internal class HalfSpaceQuery : ICollisionQuery
    {
        internal struct Face
        {
            public Vector3 Point;
            public Vector3 Normal;   // 이 방향이 빈 공간, 반대쪽이 solid
        }

        public readonly List<Face> Faces = new List<Face>();

        public void AddGround(float y)
            => Faces.Add(new Face { Point = new Vector3(0f, y, 0f), Normal = Vector3.up });

        /// <param name="degrees">+x로 갈수록 높아지는 오르막의 각도.</param>
        public void AddSlope(float degrees, Vector3 through)
        {
            float rad = degrees * Mathf.Deg2Rad;
            Faces.Add(new Face { Point = through, Normal = new Vector3(-Mathf.Sin(rad), Mathf.Cos(rad), 0f) });
        }

        /// <summary>면에서 캡슐까지의 여유. 음수면 그만큼 파묻혔다는 뜻.</summary>
        public float Clearance(Face face, Vector3 p1, Vector3 p2, float radius)
            => Mathf.Min(Vector3.Dot(p1 - face.Point, face.Normal),
                         Vector3.Dot(p2 - face.Point, face.Normal)) - radius;

        public CollisionHit CapsuleCast(Vector3 p1, Vector3 p2, float radius,
            Vector3 direction, float distance, int layerMask)
        {
            float best = float.MaxValue;
            Vector3 normal = Vector3.zero;
            foreach (var face in Faces)
            {
                float clear = Clearance(face, p1, p2, radius);
                //  이미 파묻힌 면은 sweep이 못 본다 — PhysX가 시작 겹침을 무시하는 것과 같다.
                //  이 성질이 없으면 파묻힘 버그 자체가 재현되지 않는다.
                if (clear < 0f) continue;
                float closing = Vector3.Dot(direction, face.Normal);
                if (closing >= -1e-6f) continue;   // 멀어지거나 평행
                float t = clear / -closing;
                if (t <= distance && t < best) { best = t; normal = face.Normal; }
            }
            return best == float.MaxValue
                ? CollisionHit.None
                : new CollisionHit(true, best, normal, p1, null);
        }

        public CollisionHit Raycast(Vector3 origin, Vector3 direction, float distance, int layerMask)
            => CollisionHit.None;

        public CollisionHit[] OverlapSphere(Vector3 center, float radius, int layerMask)
            => System.Array.Empty<CollisionHit>();

        /// <summary>지금 캡슐이 면 안쪽이면 밖으로 밀어낼 벡터(겹침 없으면 zero).</summary>
        public Vector3 PushOut(Vector3 p1, Vector3 p2, float radius)
        {
            Vector3 total = Vector3.zero;
            foreach (var face in Faces)
            {
                float clear = Clearance(face, p1, p2, radius);
                if (clear < 0f) total += face.Normal * -clear;
            }
            return total;
        }
    }

    /// <summary>진짜 <c>MotionBridge</c>처럼 파묻힘을 World.Transform에 반영하고 민 값을 돌려준다.</summary>
    internal class HalfSpaceMotionBridge : GameFramework.World.IMotionBridge
    {
        private readonly HalfSpaceQuery _map;

        public HalfSpaceMotionBridge(HalfSpaceQuery map) { _map = map; }

        public void SyncTransforms() { }
        public void Separate(GameFramework.World.Entity entity) { }
        public void PushMotion(GameFramework.World.Entity entity) { }

        public System.Numerics.Vector3 Depenetrate(GameFramework.World.Entity entity)
        {
            var transform = entity.Get<GameFramework.World.Transform>();
            var shape = entity.Get<GameFramework.World.CapsuleShape>();
            Vector3 feet = new Vector3(transform.Position.X, transform.Position.Y, transform.Position.Z);
            Vector3 p1 = feet + Vector3.up * shape.Radius;
            Vector3 p2 = feet + Vector3.up * (shape.Height - shape.Radius);
            Vector3 push = _map.PushOut(p1, p2, shape.Radius);
            if (push == Vector3.zero)
            {
                return System.Numerics.Vector3.Zero;
            }
            transform.Position += new System.Numerics.Vector3(push.x, push.y, push.z);
            return new System.Numerics.Vector3(push.x, push.y, push.z);
        }
    }

    /// <summary>바닥(y=0)에 <c>StepX</c>부터 <c>StepHeight</c>짜리 한 단이 있는 지형. 턱 오르기 검증용.</summary>
    internal class StepQuery : ICollisionQuery
    {
        public float StepX = 1f;
        public float StepHeight = 0.1f;

        private float SurfaceY(float x) => x >= StepX ? StepHeight : 0f;

        public CollisionHit CapsuleCast(Vector3 p1, Vector3 p2, float radius,
            Vector3 direction, float distance, int layerMask)
        {
            float bottom = Mathf.Min(p1.y, p2.y) - radius;
            if (direction.y < -0.5f)
            {
                float gap = bottom - SurfaceY(p1.x);
                return gap >= 0f && gap <= distance
                    ? new CollisionHit(true, gap, Vector3.up, Vector3.zero, null) : CollisionHit.None;
            }
            if (direction.y > 0.5f)
            {
                return CollisionHit.None;   // 천장 없음
            }
            if (bottom >= StepHeight - 1e-4f)
            {
                return CollisionHit.None;   // 턱보다 높이 있으면 안 막힌다
            }
            float ahead = (StepX - radius) - p1.x;
            return ahead >= 0f && ahead <= distance
                ? new CollisionHit(true, ahead, Vector3.left, Vector3.zero, null) : CollisionHit.None;
        }

        public CollisionHit Raycast(Vector3 origin, Vector3 direction, float distance, int layerMask)
            => CollisionHit.None;

        public CollisionHit[] OverlapSphere(Vector3 center, float radius, int layerMask)
            => System.Array.Empty<CollisionHit>();
    }
}
