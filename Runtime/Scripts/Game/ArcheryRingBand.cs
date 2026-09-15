namespace LOP
{
    /// <summary>
    /// 과녁 한 장의 띠 하나. 맞은 자리가 중심에서 얼마나 벗어났는지를 <b>반지름으로 나눈 값</b>(0~1)으로
    /// 보고, 그 값이 이 띠의 바깥 경계 안이면 이 점수를 준다.
    ///
    /// <para>지금의 "과녁당 고정 점수"는 바깥 경계가 1인 띠 하나다 — 어디를 맞히든 같은 점수라는 뜻이
    /// 그대로 표현된다.</para>
    /// </summary>
    public readonly struct ArcheryRingBand
    {
        /// <summary>이 띠가 어디까지인가(0~1). 중심이 0, 과녁 가장자리가 1이다.</summary>
        public readonly float OuterRatio;

        /// <summary>이 띠에 맞으면 점수가 이만큼 움직인다. 함정은 음수다.</summary>
        public readonly int Points;

        public ArcheryRingBand(float outerRatio, int points)
        {
            OuterRatio = outerRatio;
            Points = points;
        }
    }
}
