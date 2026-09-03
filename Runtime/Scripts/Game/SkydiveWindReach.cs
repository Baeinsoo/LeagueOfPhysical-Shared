namespace LOP
{
    /// <summary>
    /// 배치가 통과 가능한지 재는 산수. <see cref="SkydiveReach"/>가 "구멍 사이가 닿는 거리인가"를
    /// 보듯, 여기는 <b>바람이 낀 채로도 닿는가</b>를 본다.
    ///
    /// <para>역풍은 밀린 거리와 필요 이동이 더해져서, 구간 전체를 덮는 12 m/s 역풍 하나만으로
    /// 아무도 못 지나가는 구간이 만들어진다. 그러면 에러 없이 판이 안 끝나는 것으로만 보이므로
    /// 굽기 전에 여기서 막는다.</para>
    /// </summary>
    public static class SkydiveWindReach
    {
        /// <summary>
        /// 바람이 미는 거리. 몸은 <c>lag</c>초에 걸쳐 일정 속도로 바람에 실리므로, 실린 비율은
        /// <c>min(1, 지난시간/lag)</c>이고 밀린 거리는 그것을 머문 시간만큼 쌓은 값이다.
        /// </summary>
        public static float DriftDistance(float windSpeed, float bandHeight, float fallSpeed, float lag)
        {
            if (fallSpeed <= 0f || bandHeight <= 0f)
            {
                return 0f;
            }

            float time = bandHeight / fallSpeed;
            if (lag <= 0f)
            {
                return windSpeed * time;
            }

            return time >= lag
                ? windSpeed * (time - lag * 0.5f)               // 다 실린 뒤로는 그대로 흐른다
                : windSpeed * time * time / (2f * lag);         // 다 실리기 전에 빠져나간다
        }

        /// <summary>
        /// 자기 힘으로 갈 수 있는 옆 거리. 최고 속도까지 붙는 데 걸리는 시간만큼 손해를 뺀다.
        /// </summary>
        public static float SelfReach(float moveSpeed, float turnAccel, float dropHeight, float fallSpeed)
        {
            if (fallSpeed <= 0f || dropHeight <= 0f)
            {
                return 0f;
            }

            float time = dropHeight / fallSpeed;
            if (turnAccel <= 0f)
            {
                return 0f;
            }

            float rampTime = moveSpeed / turnAccel;
            return rampTime >= time
                ? 0.5f * turnAccel * time * time                // 최고 속도에 닿기 전에 구간이 끝난다
                : moveSpeed * (time - rampTime * 0.5f);
        }

        /// <summary>
        /// 바람에 밀린 자리에서 자기 힘으로 구멍까지 닿나. 순풍이면 밀린 만큼이 이득이고
        /// 역풍이면 그만큼 더 가야 하는데, 그 둘이 이 뺄셈 하나로 같이 나온다.
        /// </summary>
        public static bool CanReach(float requiredX, float requiredZ,
                                    float driftX, float driftZ, float selfReach)
        {
            float dx = requiredX - driftX;
            float dz = requiredZ - driftZ;
            return dx * dx + dz * dz <= selfReach * selfReach;
        }
    }
}
