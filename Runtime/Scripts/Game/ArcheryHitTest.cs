using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 화살이 이번 틱에 지나온 <b>선분</b>이 과녁 구를 스쳤는지 본다. 매 틱의 점만 검사하면 빠른
    /// 화살이 작은 과녁을 통째로 뚫고 지나간다(65m/s면 한 틱에 1.3m — 지름 0.5m 과녁보다 길다).
    /// 상태 없는 순수 계산이다.
    /// </summary>
    public static class ArcheryHitTest
    {
        /// <summary>
        /// 맞았으면 참이고 <paramref name="t"/>에 <b>처음 닿은 지점</b>이 선분 위 어디인지(0~1) 담는다.
        /// 시작점이 이미 구 안이면 0이다.
        /// </summary>
        public static bool SegmentHitsSphere(Vector3 from, Vector3 to, Vector3 center, float radius, out float t)
        {
            t = 0f;

            Vector3 toCenter = from - center;
            //  시작점이 이미 안에 있으면 더 볼 것이 없다 — 바로 닿은 것이다.
            if (toCenter.sqrMagnitude <= radius * radius)
            {
                return true;
            }

            Vector3 segment = to - from;
            float a = Vector3.Dot(segment, segment);
            if (a <= 1e-12f)
            {
                return false;   // 길이가 0인데 위에서 안 걸렸다 = 구 밖의 한 점
            }

            //  |from + segment*t − center|² = radius² 를 t에 대해 푼다.
            float b = 2f * Vector3.Dot(toCenter, segment);
            float c = Vector3.Dot(toCenter, toCenter) - radius * radius;
            float discriminant = b * b - 4f * a * c;
            if (discriminant < 0f)
            {
                return false;   // 아예 안 스친다
            }

            //  두 해 중 작은 쪽이 "들어가는 지점"이다. 위에서 밖이라고 확인했으므로 이 값이 답이다.
            float entry = (-b - Mathf.Sqrt(discriminant)) / (2f * a);
            if (entry < 0f || entry > 1f)
            {
                return false;   // 닿는 지점이 이번 틱의 선분 밖이다
            }

            t = entry;
            return true;
        }

        /// <summary>
        /// 화살 선분이 <b>사수를 향해 선 원판</b>을 맞혔는지 본다.
        /// <paramref name="facing"/> 쪽에서 오는 화살만 맞는다 — 뒤에서 온 것은 통과한다.
        /// 맞았으면 <paramref name="t"/>에 선분 위 어디서 평면을 지났는지(0~1)를 담는다.
        /// </summary>
        public static bool SegmentHitsFace(Vector3 from, Vector3 to, Vector3 center, Vector3 facing,
                                           float radius, out float t)
        {
            t = 0f;

            Vector3 segment = to - from;
            float approach = Vector3.Dot(segment, facing);
            //  화살이 판이 보는 쪽으로 다가가고 있어야 한다. 0이면 판과 나란히 가는 것이고,
            //  양수면 뒤에서 오는 것이다.
            if (approach >= -1e-9f)
            {
                return false;
            }

            //  판이 보는 쪽에서 출발했는지 본다. 이미 판을 지나쳐 있었다면 뒤에서 오는 것이다.
            float startSide = Vector3.Dot(from - center, facing);
            if (startSide <= 0f)
            {
                return false;
            }

            //  선분이 판의 평면을 지나는 지점을 푼다.
            float cross = startSide / -approach;
            if (cross < 0f || cross > 1f)
            {
                return false;   // 이번 틱에는 평면까지 못 간다
            }

            Vector3 impact = from + segment * cross;
            if ((impact - center).sqrMagnitude > radius * radius)
            {
                return false;   // 평면은 지났지만 판 밖이다
            }

            t = cross;
            return true;
        }

        /// <summary>
        /// 과녁 모양에 맞는 판정을 고른다. <b>소비처는 이 함수만 부른다</b> — 모양이 늘어도
        /// 부르는 쪽은 안 바뀐다.
        /// <paramref name="normalizedOffset"/>은 맞은 자리가 중심에서 얼마나 벗어났는지를
        /// 반지름으로 나눈 값이다(0이 정중앙, 1이 가장자리). 채점이 이 값으로 띠를 찾는다.
        /// </summary>
        public static bool SegmentHitsTarget(Vector3 from, Vector3 to, Vector3 center,
                                             in ArcheryTarget target,
                                             out float t, out float normalizedOffset)
        {
            normalizedOffset = 0f;

            bool hit = target.Shape == ArcheryTargetShape.Face
                ? SegmentHitsFace(from, to, center, target.Facing, target.Radius, out t)
                : SegmentHitsSphere(from, to, center, target.Radius, out t);

            if (hit == false)
            {
                return false;
            }

            if (target.Radius > 1e-6f)
            {
                Vector3 impact = from + (to - from) * t;
                normalizedOffset = Mathf.Clamp01((impact - center).magnitude / target.Radius);
            }
            return true;
        }
    }
}
