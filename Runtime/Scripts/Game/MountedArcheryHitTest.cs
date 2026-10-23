using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 말 탄 사람 한 명의 맞는 자리(구 네 개)와, 화살이 이번 틱에 지나온 선분이 그것에 닿는지 재는 식.
    /// 클·서가 같은 식을 써야 "보이는 대로 맞는다". 구 값은 수치 튜닝 전까지 상수다(슬라이스 4에서 표로 옮긴다).
    /// 방향은 <see cref="MountMotor"/>와 같다 — 0도가 +z, 90도가 +x.
    /// </summary>
    public static class MountedArcheryHitTest
    {
        // 탈것 기준: 앞이 +z, 위가 +y
        private static readonly (Vector3 center, float radius)[] Spheres =
        {
            (new Vector3(0f, 2.4f, 0f), 0.62f),     // 기수
            (new Vector3(0f, 1.4f, 0.6f), 0.72f),   // 말 몸 앞
            (new Vector3(0f, 1.4f, -0.6f), 0.72f),  // 말 몸 뒤
            (new Vector3(0f, 2.15f, 1.35f), 0.45f), // 말 머리
        };

        /// <summary>
        /// 선분 <paramref name="from"/>→<paramref name="to"/>가 구 넷 중 하나라도 지나면 참.
        /// <paramref name="along"/>은 선분 위 0~1에서 가장 이른 접점 비율(안 맞으면 0).
        /// 빠른 화살이 한 틱에 몸을 건너뛰어도 선분으로 재므로 놓치지 않는다.
        /// </summary>
        public static bool SegmentHits(Vector3 from, Vector3 to, Vector3 mountPosition, float headingDeg, out float along)
        {
            float rad = headingDeg * Mathf.Deg2Rad;
            var forward = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            var right = new Vector3(Mathf.Cos(rad), 0f, -Mathf.Sin(rad));

            along = 0f;
            bool hit = false;
            float best = float.MaxValue;
            foreach (var (center, radius) in Spheres)
            {
                Vector3 world = mountPosition + right * center.x + Vector3.up * center.y + forward * center.z;
                if (EarliestContact(from, to, world, radius, out float t) && t < best)
                {
                    best = t;
                    hit = true;
                }
            }
            if (hit) along = best;
            return hit;
        }

        // 선분 위 점 from + t*(to-from)이 구 안에 처음 들어가는 t. 이미 안이면 0.
        private static bool EarliestContact(Vector3 from, Vector3 to, Vector3 center, float radius, out float t)
        {
            t = 0f;
            Vector3 d = to - from;
            Vector3 m = from - center;
            float c = m.sqrMagnitude - radius * radius;
            if (c <= 0f) return true;

            float a = d.sqrMagnitude;
            if (a < 1e-12f) return false;

            float b = Vector3.Dot(m, d);
            float disc = b * b - a * c;
            if (disc < 0f) return false;

            float root = (-b - Mathf.Sqrt(disc)) / a;
            if (root < 0f || root > 1f) return false;
            t = root;
            return true;
        }
    }
}
