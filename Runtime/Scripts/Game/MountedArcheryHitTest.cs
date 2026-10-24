using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 말 탄 사람 한 명의 맞는 자리(구 여러 개)와, 화살이 이번 틱에 지나온 선분이 그것에 닿는지 재는 식.
    /// 클·서가 같은 식을 써야 "보이는 대로 맞는다". 구 값은 수치 튜닝 전까지 상수다(슬라이스 4에서 표로 옮긴다).
    /// 방향은 <see cref="MountMotor"/>와 같다 — 0도가 +z, 90도가 +x.
    /// </summary>
    public static class MountedArcheryHitTest
    {
        // 탈것 기준: 앞이 +z, 위가 +y. 클라가 그리는 회색 말(MountedHorseView)과 안장 위 치비 기수 안에 들어가게 잡았다 —
        // 판정이 그림보다 크면 눈에는 빗나간 화살이 맞는다. 말 몸(0.85×0.85×2 m 상자)은 구 넷을 앞뒤로 늘어놓아 채운다.
        private static readonly (Vector3 center, float radius)[] Spheres =
        {
            (new Vector3(0f, 2.1f, -0.1f), 0.35f),   // 기수(안장 위로 보이는 상체와 머리)
            (new Vector3(0f, 1.4f, -0.57f), 0.43f),  // 말 몸
            (new Vector3(0f, 1.4f, -0.19f), 0.43f),
            (new Vector3(0f, 1.4f, 0.19f), 0.43f),
            (new Vector3(0f, 1.4f, 0.57f), 0.43f),
            // 말 목 — 0.95 m 상자가 앞으로 31° 기울어 있다. 축을 따라 구 셋을 놓는다(위 끝이 기수 쪽, 아래 끝이 앞쪽).
            (new Vector3(0f, 2.207f, 0.795f), 0.21f),
            (new Vector3(0f, 1.95f, 0.95f), 0.21f),
            (new Vector3(0f, 1.693f, 1.105f), 0.21f),
            (new Vector3(0f, 2.32f, 1.2f), 0.21f),   // 말 머리
            (new Vector3(0f, 2.32f, 1.55f), 0.21f),
        };

        public static int ShapeCount => Spheres.Length;

        /// <summary>i번째 판정 구의 월드 중심과 반지름. 디버그 그림이 판정과 그림을 겹쳐 보는 데 쓴다.</summary>
        public static (Vector3 center, float radius) WorldShape(int i, Vector3 mountPosition, float headingDeg)
        {
            var (center, radius) = Spheres[i];
            return (ToWorld(center, mountPosition, headingDeg), radius);
        }

        private static Vector3 ToWorld(Vector3 local, Vector3 mountPosition, float headingDeg)
        {
            float rad = headingDeg * Mathf.Deg2Rad;
            var forward = new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
            var right = new Vector3(Mathf.Cos(rad), 0f, -Mathf.Sin(rad));
            return mountPosition + right * local.x + Vector3.up * local.y + forward * local.z;
        }

        /// <summary>
        /// 선분 <paramref name="from"/>→<paramref name="to"/>가 구 중 하나라도 지나면 참.
        /// <paramref name="along"/>은 선분 위 0~1에서 가장 이른 접점 비율(안 맞으면 0).
        /// 빠른 화살이 한 틱에 몸을 건너뛰어도 선분으로 재므로 놓치지 않는다.
        /// </summary>
        public static bool SegmentHits(Vector3 from, Vector3 to, Vector3 mountPosition, float headingDeg, out float along)
        {
            along = 0f;
            bool hit = false;
            float best = float.MaxValue;
            foreach (var (center, radius) in Spheres)
            {
                Vector3 world = ToWorld(center, mountPosition, headingDeg);
                if (EarliestContact(from, to, world, radius, out float t) && t < best)
                {
                    best = t;
                    hit = true;
                }
            }
            if (hit) along = best;
            return hit;
        }

        /// <summary>
        /// 선분이 <b>처음 닿는 구</b>를 지나는 구간(선분 위 0~1 비율). 조준이 구의 가장자리가 아니라 가운데를 겨누는 데 쓴다.
        /// 선분이 그 구 안에서 시작하면 <paramref name="enter"/>는 0이다.
        /// </summary>
        public static bool SegmentChord(Vector3 from, Vector3 to, Vector3 mountPosition, float headingDeg,
                                        out float enter, out float exit)
        {
            enter = exit = 0f;
            bool hit = false;
            float best = float.MaxValue;
            foreach (var (center, radius) in Spheres)
            {
                Vector3 world = ToWorld(center, mountPosition, headingDeg);
                if (EarliestContact(from, to, world, radius, out float t) && t < best)
                {
                    best = t;
                    hit = true;
                    enter = t;
                    exit = Exit(from, to, world, radius);
                }
            }
            return hit;
        }

        // 선분이 구를 빠져나가는 비율(0~1로 자른다).
        private static float Exit(Vector3 from, Vector3 to, Vector3 center, float radius)
        {
            Vector3 d = to - from;
            Vector3 m = from - center;
            float a = d.sqrMagnitude;
            if (a < 1e-12f) return 0f;
            float b = Vector3.Dot(m, d);
            float c = m.sqrMagnitude - radius * radius;
            float disc = Mathf.Max(0f, b * b - a * c);
            return Mathf.Clamp01((-b + Mathf.Sqrt(disc)) / a);
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
