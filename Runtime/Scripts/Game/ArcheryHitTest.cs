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
    }
}
