namespace LOP
{
    /// <summary>
    /// 도는 날개의 자세. <b>레이저와 같은 성질이다</b> — 틱만 넣으면 각도가 나오므로 스냅샷에 실을
    /// 것도, 되감기에서 되돌릴 것도 없다. 되감기 재생이 라이브와 같아지는 근거가 이 성질 하나다.
    ///
    /// <para>⚠️ <b>실험용(스파이크)</b> — "곡면 날개가 사람을 미는가"를 눈으로 보려고 만든 것이다.
    /// 결과에 따라 통째로 버릴 수 있다.</para>
    /// </summary>
    public static class BladeGeometry
    {
        /// <summary>
        /// 이 틱의 회전각(도). <b>[0, 360)으로 접어서</b> 돌려준다 — 판이 길어지면 틱이 수만이 되고
        /// 그때 `속도 × 틱`을 float로 그냥 두면 자릿수가 커져 정밀도가 뭉개진다. 접는 계산만
        /// double로 하고 결과는 작은 값이라 float로 안전하다.
        /// </summary>
        /// <param name="tick">판정은 정수 틱, 그림은 소수 틱(프레임 사이)을 넣는다.
        /// <b>식을 하나로 두는 이유</b>: 둘이 갈라지면 보이는 자세와 맞는 자세가 어긋난다.</param>
        public static float AngleDegreesAt(float startDegrees, float speedDegreesPerTick, double tick)
        {
            double raw = startDegrees + (double)speedDegreesPerTick * tick;
            double folded = raw - System.Math.Floor(raw / 360.0) * 360.0;
            return (float)folded;
        }
    }
}
