namespace LOP
{
    /// <summary>
    /// 낙석 바위가 이 틱에 떨어지는 중인가, 그렇다면 천장에서 얼마나 내려왔나. 주기·속도·위상과 틱만 보고 답한다 — 누적하지 않는다.
    /// 한 주기: 천장 속에서 미끄러져 나와 곧게 떨어져 바닥 밑까지(<paramref name="dropDistance"/>) → 남은 시간은 천장 속에 숨는다.
    /// <c>fallen</c>은 바위 <b>윗면</b>이 떨어지기 시작한 자리(천장선 위 바위 한 변)에서 내려온 거리다 — 0이면 아랫면이 천장선.
    /// 예고는 판정엔 없다(10-13 사용자) — 클라가 그림으로만 보인다.
    /// <see cref="FlappyShutterCurve"/>와 같은 이유로 순수 함수다.
    /// </summary>
    public static class FlappyRockfallCurve
    {
        public static bool FallingAt(float periodSeconds, float fallSpeed, float dropDistance, float phaseDegrees,
                                     long tick, float tickSeconds, out float fallen)
        {
            fallen = 0f;
            if (periodSeconds <= 0f || fallSpeed <= 0f || tickSeconds <= 0f)
            {
                return false;
            }
            //  틱 안에서의 위치를 정수로 구한다 — float 소수부(cycles - floor(cycles))는 tickSeconds가
            //  정확한 0.02가 아니라서(float) 주기 경계(틱 100 등)에서 1보다 살짝 작게 나와, "막 끝난 주기"로
            //  읽히는 바람에 다 떨어진 뒤로 오판정됐다. 정수 틱 나눗셈은 그 경계에서도 항상 정확히 0이다.
            long ticksPerPeriod = System.Math.Max(1L, (long)System.Math.Round(periodSeconds / (double)tickSeconds));
            long phaseTicks = (long)System.Math.Round(phaseDegrees / 360.0 * ticksPerPeriod);
            long u = ((tick + phaseTicks) % ticksPerPeriod + ticksPerPeriod) % ticksPerPeriod;
            double elapsed = (double)u * tickSeconds;
            double fall = elapsed * fallSpeed;
            if (fall >= dropDistance)
            {
                return false;
            }
            fallen = (float)fall;
            return true;
        }
    }
}
