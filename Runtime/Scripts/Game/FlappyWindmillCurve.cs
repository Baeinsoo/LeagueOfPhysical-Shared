namespace LOP
{
    /// <summary>
    /// 풍차 날개의 각도(도). 시작각과 속도만 보고 답한다 — 누적하지 않는다.
    ///
    /// <para><see cref="FlappyChaserCurve"/>와 같은 성질이 필요해서 같은 모양으로 둔다: 어느 틱을
    /// 물어도 답이 하나라 클라와 서버가 각자 계산해도 같고, 되돌리기로 과거 틱을 물어도 그때 값이
    /// 나온다. 프레임마다 <c>Rotate</c>로 더해 가면 이 셋이 전부 깨진다 — 프레임레이트가 위상을
    /// 가르고, 지나간 틱의 각도를 물을 방법이 없어진다.</para>
    ///
    /// <para>상태 없는 순수 계산이라 <c>*System</c>이 아니라 static 커널이다
    /// (<c>LaserGeometry.Angle</c>·<c>MovementMotor.CalcVelocity</c>와 같은 짝).</para>
    /// </summary>
    public static class FlappyWindmillCurve
    {
        /// <summary>
        /// <paramref name="tick"/> 시점의 날개 각도(도, [0,360)).
        /// </summary>
        /// <param name="tick">
        /// 음수여도 되고(되감기) 아주 커도 된다(긴 판). 각도를 배로 구하고 마지막에 한 번만 감싸므로
        /// 틱이 커져도 오차가 쌓이지 않는다 — 틱마다 더해 가면 그 덧셈 오차가 쌓인다.
        /// </param>
        public static float AngleAt(float startAngleDegrees, float rotSpeedDegreesPerSecond,
                                    long tick, float tickSeconds)
        {
            // double로 셈한다. 한 시간짜리 판이면 각도 누계가 십만 도를 넘는데, float은 그 크기에서
            // 0.01도조차 표현하지 못해 큰 틱에서 각도가 계단처럼 튄다.
            double angle = startAngleDegrees
                         + (double)rotSpeedDegreesPerSecond * ((double)tick * tickSeconds);

            angle %= 360.0;
            if (angle < 0.0)
            {
                angle += 360.0;
            }
            // 음수를 감싼 결과가 반올림으로 정확히 360이 될 수 있다. [0,360)을 약속했으므로 0으로 접는다.
            if (angle >= 360.0)
            {
                angle = 0.0;
            }
            return (float)angle;
        }
    }
}
