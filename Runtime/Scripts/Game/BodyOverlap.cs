using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 세로로 선 <b>같은 규격</b>의 캡슐 둘이 얼마나 겹쳤는지 구한다(순수 계산 — 물리엔진을
    /// 부르지 않는다). 게임 비종속이다 — 반지름·높이를 인자로 받는다.
    ///
    /// 몸이 전부 같은 캡슐이라 겹침이 "두 심(축) 사이 거리 &lt; 지름"이라는 산수로 끝난다.
    /// (규격이 서로 다른 몸을 섞으려면 이 전제부터 손봐야 한다.)
    /// 물리엔진에 물어보지 않는 이유는, 되감아 다시 돌렸을 때 답이 같아야 클라 예측이 서버와
    /// 맞기 때문이다 — 물리엔진은 그 보장을 하지 않는다. (모양이 제각각인 맵은 그럴 수 없어
    /// 물리엔진 sweep을 쓴다.)
    /// </summary>
    public static class BodyOverlap
    {
        /// <summary>
        /// 겹쳤으면 true와 함께 <paramref name="pushDir"/>(a를 b 밖으로 밀어낼 단위 방향)와
        /// <paramref name="depth"/>(겹친 깊이)를 준다. 위치는 캡슐 발밑 기준 — KinematicMover와 같은 약속이다.
        /// </summary>
        public static bool TryCompute(Vector3 a, Vector3 b, float radius, float height,
                                      out Vector3 pushDir, out float depth)
        {
            pushDir = Vector3.zero;
            depth = 0f;

            // 캡슐의 심 = 아래쪽 구 중심부터 위쪽 구 중심까지의 세로 선분.
            float aLow = a.y + radius, aHigh = a.y + height - radius;
            float bLow = b.y + radius, bHigh = b.y + height - radius;

            // 두 심 사이의 세로 간격. 높이가 서로 겹치면 0 — 그때는 옆거리만으로 판정된다.
            float dy = 0f;
            if (bLow > aHigh)
            {
                dy = bLow - aHigh;
            }
            else if (bHigh < aLow)
            {
                dy = bHigh - aLow;
            }

            Vector3 delta = new Vector3(b.x - a.x, dy, b.z - a.z);
            float touchDistance = radius * 2f;
            float distanceSquared = delta.sqrMagnitude;
            if (distanceSquared >= touchDistance * touchDistance)
            {
                return false;
            }

            float distance = Mathf.Sqrt(distanceSquared);
            depth = touchDistance - distance;
            if (distance < 1e-6f)
            {
                // 두 몸이 정확히 같은 자리 — 밀어낼 방향을 거리에서 구할 수 없다.
                // 아무 방향이나 고르면 클·서가 다르게 갈릴 수 있어 규칙을 하나 박아 둔다.
                // 부르는 쪽이 id 순으로 짝을 세우므로 늘 같은 몸이 아래로 간다.
                pushDir = Vector3.down;
                return true;
            }

            pushDir = -delta / distance;
            return true;
        }
    }
}
