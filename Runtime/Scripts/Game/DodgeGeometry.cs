using UnityEngine;

namespace LOP
{
    /// <summary>피하기 판정에 쓰는 바닥 평면(x, z) 거리 계산. Unity 물리를 쓰지 않는다 — 클·서가 같은 답을 내야 한다.</summary>
    public static class DodgeGeometry
    {
        /// <summary>
        /// 한 틱 동안 A는 a0→a1, B는 b0→b1로 곧게 움직였을 때 둘이 가장 가까웠던 거리.
        /// 끝 위치만 비교하면 빠른 탄이 몸을 건너뛰어도 못 잡는다.
        /// </summary>
        public static float ClosestApproach(Vector2 a0, Vector2 a1, Vector2 b0, Vector2 b1)
        {
            Vector2 d0 = a0 - b0;
            Vector2 dv = (a1 - a0) - (b1 - b0);
            float vv = Vector2.Dot(dv, dv);
            float s = vv < 1e-12f ? 0f : Mathf.Clamp01(-Vector2.Dot(d0, dv) / vv);
            return (d0 + dv * s).magnitude;
        }

        /// <summary>선분 p0p1과 q0q1 사이 최단거리. 교차하면 0.</summary>
        public static float SegmentDistance(Vector2 p0, Vector2 p1, Vector2 q0, Vector2 q1)
        {
            if (Intersects(p0, p1, q0, q1))
            {
                return 0f;
            }
            return Mathf.Min(Mathf.Min(PointSegment(p0, q0, q1), PointSegment(p1, q0, q1)),
                             Mathf.Min(PointSegment(q0, p0, p1), PointSegment(q1, p0, p1)));
        }

        public static float PointSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = Vector2.Dot(ab, ab);
            float t = len2 < 1e-12f ? 0f : Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return (p - (a + ab * t)).magnitude;
        }

        private static float Cross(Vector2 o, Vector2 a, Vector2 b) => (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);

        private static bool Intersects(Vector2 p0, Vector2 p1, Vector2 q0, Vector2 q1)
        {
            float d1 = Cross(q0, q1, p0), d2 = Cross(q0, q1, p1);
            float d3 = Cross(p0, p1, q0), d4 = Cross(p0, p1, q1);
            return ((d1 > 0f && d2 < 0f) || (d1 < 0f && d2 > 0f)) && ((d3 > 0f && d4 < 0f) || (d3 < 0f && d4 > 0f));
        }
    }
}
