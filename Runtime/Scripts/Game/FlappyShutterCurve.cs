namespace LOP
{
    /// <summary>
    /// 주기 셔터가 열린 정도(0 닫힘 ~ 1 다 열림). 주기·몫·위상과 틱만 보고 답한다 — 누적하지 않는다.
    /// <see cref="FlappyPendulumCurve"/>와 같은 이유로 순수 함수다: 클·서가 각자 계산해도 같고,
    /// 롤백으로 지난 틱을 물어도 그때 값이 나온다.
    /// 한 주기: 닫힘 → 올라감(곧게) → 열림 → 내려옴(곧게).
    /// </summary>
    public static class FlappyShutterCurve
    {
        public static float OpenAt(float periodSeconds, float openShare, float moveShare, float phaseDegrees,
                                   long tick, float tickSeconds)
        {
            if (periodSeconds <= 0f)
            {
                return 0f;
            }
            //  몫이 넘치면 닫힘이 음수가 된다 — 움직임을 먼저 지키고 열림을 남는 만큼으로 깎는다.
            double move = System.Math.Min(System.Math.Max((double)moveShare, 0.0), 0.5);
            double open = System.Math.Min(System.Math.Max((double)openShare, 0.0), 1.0 - 2.0 * move);
            double closed = 1.0 - open - 2.0 * move;

            //  double로 주기 수를 세고 소수부만 남긴다. 큰 틱에서 float 시간은 0.02초도 못 담는다.
            double cycles = (double)tick * tickSeconds / periodSeconds + phaseDegrees / 360.0;
            double u = cycles - System.Math.Floor(cycles);

            if (u < closed)
            {
                return 0f;
            }
            u -= closed;
            if (u < move)
            {
                return (float)(u / move);
            }
            u -= move;
            if (u < open)
            {
                return 1f;
            }
            u -= open;
            return move > 0.0 ? (float)System.Math.Max(0.0, 1.0 - u / move) : 0f;
        }
    }
}
