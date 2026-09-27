namespace LOP
{
    /// <summary>
    /// 새의 세로 속도 한 틱. 이동 시스템과 맵 검사기(탐색·재생)가 <b>이 한 곳</b>을 부른다 —
    /// 같은 식을 두 벌 적으면 1 ulp 차이로도 검사기가 찾은 경로가 게임에서 깨진다.
    /// 순서: 중력(또는 기류) → 종단속도 클램프 → 날갯짓이면 덮어쓰기.
    /// </summary>
    public static class FlappyVerticalKernel
    {
        public static float Next(float vy, bool flap, FlappyAirflowKind air,
                                 float flapImpulse, float gravity, float maxFallSpeed, float dt,
                                 float upAccel, float riseCap, float shaftGravityMult)
        {
            float next;
            if (air == FlappyAirflowKind.Up)
            {
                //  기류는 상한까지만 민다. 날갯짓으로 상한 위에 있으면 평소처럼 중력만 받는다.
                next = vy < riseCap ? System.Math.Min(vy + upAccel * dt, riseCap) : vy - gravity * dt;
            }
            else if (air == FlappyAirflowKind.Down)
            {
                next = vy - gravity * shaftGravityMult * dt;
            }
            else
            {
                next = vy - gravity * dt;
            }
            if (next < -maxFallSpeed)
            {
                next = -maxFallSpeed;
            }
            if (flap)
            {
                next = flapImpulse;
            }
            return next;
        }
    }
}
