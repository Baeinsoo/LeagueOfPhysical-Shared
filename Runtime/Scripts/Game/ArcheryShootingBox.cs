using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 사수가 움직일 수 있는 <b>사대</b> — 레인 원점을 가운데 둔 납작한 상자. 그 밖으로는 못 나간다.
    ///
    /// <para><b>왜 상자로 묶나</b>: 앞으로 걸어 나갈 수 있으면 90m 과녁 앞으로 30m 걸어가 60m로
    /// 만들 수 있다. 그러면 거리 여섯을 둔 것도, 거리마다 과녁 크기를 다르게 한 것도,
    /// "자리를 고를 수 없으니 거리 배수가 필요 없다"는 채점 규칙도 전부 근거를 잃는다.
    /// 좌우로 움직이며 쏘는 손맛은 주되, <b>거리는 주어진 것으로 남긴다.</b></para>
    ///
    /// <para><b>레인 기준</b>으로 자른다 — 레인마다 보는 쪽이 다르므로 월드 축으로 자르면
    /// 어떤 레인은 좌우가 앞뒤가 된다. 높이는 건드리지 않는다(중력과 충돌이 정한다).</para>
    ///
    /// <para>벽(콜라이더)이 아니라 계산으로 막는 이유는 이것이 <b>규칙</b>이기 때문이다 —
    /// 맵은 자리(레인 원점·방향)라는 값을 주고, 규칙은 여기서 실행한다.</para>
    /// </summary>
    public static class ArcheryShootingBox
    {
        /// <summary>
        /// <paramref name="position"/>을 사대 안으로 자른다.
        /// </summary>
        /// <param name="origin">레인의 사대 한가운데(<see cref="ArcheryRangeLayout.Lane.ShooterPosition"/>).</param>
        /// <param name="forward">레인이 보는 쪽 = 과녁 쪽. 이것이 "앞뒤" 축이다.</param>
        /// <param name="halfWidth">좌우로 갈 수 있는 거리(m). 0이면 제자리에 묶인다.</param>
        /// <param name="halfDepth">앞뒤로 갈 수 있는 거리(m). 0이면 제자리에 묶인다.</param>
        public static Vector3 Clamp(Vector3 position, Vector3 origin, Vector3 forward,
                                    float halfWidth, float halfDepth)
        {
            //  수평 성분만 쓴다. 레인 마커가 살짝 기울어 찍혀도 사대가 기울지 않게.
            Vector3 ahead = new Vector3(forward.x, 0f, forward.z);
            if (ahead.sqrMagnitude < 1e-6f)
            {
                //  앞을 못 세운다(마커가 위아래만 본다) — 축이 없으니 제자리에 둔다.
                return new Vector3(origin.x, position.y, origin.z);
            }
            ahead.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, ahead);

            //  음수 크기가 들어오면 Clamp의 min/max가 뒤집혀 결과가 엉킨다. 0으로 본다.
            float maxSide = Mathf.Max(0f, halfWidth);
            float maxAhead = Mathf.Max(0f, halfDepth);

            Vector3 offset = position - origin;
            offset.y = 0f;
            float side = Mathf.Clamp(Vector3.Dot(offset, right), -maxSide, maxSide);
            float depth = Mathf.Clamp(Vector3.Dot(offset, ahead), -maxAhead, maxAhead);

            Vector3 flat = origin + right * side + ahead * depth;
            return new Vector3(flat.x, position.y, flat.z);
        }
    }
}
