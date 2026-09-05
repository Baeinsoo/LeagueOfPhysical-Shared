namespace LOP
{
    /// <summary>
    /// 추격자(뒤에서 오는 벽)의 위치. 출발 후 지난 시간만 보고 답한다.
    ///
    /// <para>누적하지 않는 것이 핵심이다 — 어느 시점을 물어도 답이 하나라서, 클라와 서버가 각자
    /// 계산해도 같고(서버가 위치를 보낼 필요가 없다) 되돌리기로 과거 틱을 물어도 그때 값이 나온다.</para>
    ///
    /// <para>상태 없는 순수 계산이라 <c>*System</c>이 아니라 static 커널이다
    /// (<c>MovementMotor.CalcVelocity</c>와 같은 짝).</para>
    /// </summary>
    public static class FlappyChaserCurve
    {
        /// <summary>출발 후 <paramref name="elapsedSeconds"/>초 시점의 벽 x. 출발 전이면 시작점.</summary>
        /// <param name="stopAtX">
        /// 벽이 더 가지 않는 자리(결승선). 여기서 멈춰도 잡는 능력은 안 줄어든다 — 벽이 결승선에
        /// 닿은 시점에 아직 못 들어온 사람은 전부 벽 뒤에 있다. 멈추지 않으면 결승선 너머에
        /// 서 있는 완주자를 벽이 통과하는 그림이 나온다(완주자는 판정에서 빠져 안 죽는데도).
        /// </param>
        public static float XAt(in FlappyConfig config, float elapsedSeconds, float stopAtX)
        {
            float x;

            if (elapsedSeconds <= 0f)
            {
                x = config.ChaserStartX;
            }
            else
            {
                float ramp = RampSeconds(config);
                if (elapsedSeconds <= ramp)
                {
                    x = config.ChaserStartX
                      + config.ChaserInitialSpeed * elapsedSeconds
                      + 0.5f * config.ChaserAcceleration * elapsedSeconds * elapsedSeconds;
                }
                else
                {
                    x = config.ChaserStartX
                      + config.ChaserInitialSpeed * ramp
                      + 0.5f * config.ChaserAcceleration * ramp * ramp
                      + config.ChaserMaxSpeed * (elapsedSeconds - ramp);
                }
            }

            return x > stopAtX ? stopAtX : x;
        }

        /// <summary>
        /// 상한 속도에 닿는 시각(초) = 압박 전환점. 가속이 없으면 영영 안 닿으므로 무한대를 준다
        /// (그러면 <see cref="XAt"/>이 계속 가속 구간을 쓴다).
        /// </summary>
        public static float RampSeconds(in FlappyConfig config)
        {
            float gap = config.ChaserMaxSpeed - config.ChaserInitialSpeed;
            if (gap <= 0f)
            {
                return 0f;
            }
            if (config.ChaserAcceleration <= 0f)
            {
                return float.PositiveInfinity;
            }
            return gap / config.ChaserAcceleration;
        }
    }
}
