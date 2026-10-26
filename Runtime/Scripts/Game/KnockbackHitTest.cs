using UnityEngine;

namespace LOP
{
    public static class KnockbackHitTest
    {
        /// <summary>
        /// 선분 a→b가 원에 처음 닿는 비율 t(0~1). 빠른 탄이 한 틱에 몸을 건너뛰지 않도록
        /// 점이 아니라 이번 틱에 지나온 선분으로 잰다. 시작점이 이미 원 안이면 t=0.
        /// </summary>
        public static bool SegmentHitsCircle(Vector2 a, Vector2 b, Vector2 center, float radius, out float t)
        {
            t = 0f;
            Vector2 d = b - a;
            Vector2 f = a - center;
            float c = f.sqrMagnitude - radius * radius;
            if (c <= 0f) return true;

            float qa = d.sqrMagnitude;
            if (qa <= 0f) return false;
            float qb = Vector2.Dot(f, d);
            float disc = qb * qb - qa * c;
            if (disc < 0f) return false;

            float root = (-qb - Mathf.Sqrt(disc)) / qa;
            if (root < 0f || root > 1f) return false;
            t = root;
            return true;
        }
    }
}
