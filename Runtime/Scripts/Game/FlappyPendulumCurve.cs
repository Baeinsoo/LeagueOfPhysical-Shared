namespace LOP
{
    /// <summary>
    /// 철골 진자의 각도(도). 진폭·주기·위상과 틱만 보고 답한다 — 누적하지 않는다.
    /// <see cref="FlappyWindmillCurve"/>와 같은 이유로 순수 함수다: 클·서가 각자 계산해도 같고,
    /// 롤백으로 지난 틱을 물어도 그때 값이 나온다.
    /// </summary>
    public static class FlappyPendulumCurve
    {
        /// <returns>축에서 수직 아래 기준 각도(도). +면 끝이 +x 쪽으로 간다(z축 반시계).</returns>
        public static float AngleAt(float amplitudeDegrees, float periodSeconds, float phaseDegrees,
                                    long tick, float tickSeconds)
        {
            if (periodSeconds <= 0f)
            {
                return 0f;
            }
            //  double로 주기 수를 세고 소수부만 남긴다. 큰 틱에서 float 시간은 0.02초도 못 담는다.
            double cycles = (double)tick * tickSeconds / periodSeconds;
            double fraction = cycles - System.Math.Floor(cycles);
            double radians = 2.0 * System.Math.PI * fraction + phaseDegrees * System.Math.PI / 180.0;
            return (float)(amplitudeDegrees * System.Math.Sin(radians));
        }
    }
}
