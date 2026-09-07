namespace LOP
{
    /// <summary>
    /// 배치가 통과 가능한지 재는 산수 — 구멍 사이가 <b>바람이 낀 채로도</b> 닿는 거리인가.
    /// (<see cref="SelfReach"/>가 무풍 자력 사거리를 맡는다. 같은 계산을 하던 옛 SkydiveReach는
    /// 사본이라 지웠다 — 두 벌이면 한쪽만 바람을 본다.)
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
        ///
        /// <para>실려 있던 바람은 볼륨을 <b>벗어난 뒤에도</b> <c>lag</c>초에 걸쳐 같은 속도로
        /// 빠지면서 그동안 계속 민다 — 그 "꼬리"까지 더해야 진짜 밀린 거리다. <paramref name="tailHeight"/>는
        /// 볼륨 아래로, 같은 구간 안에서 꼬리가 펼쳐질 수 있는 높이(구간 바닥까지)다. 다음
        /// 구간으로 넘어가는 꼬리는 여기서 자른다 — 그 구간의 몫이다.</para>
        /// </summary>
        public static float DriftDistance(float windSpeed, float bandHeight, float fallSpeed, float lag, float tailHeight)
        {
            if (fallSpeed <= 0f || bandHeight <= 0f)
            {
                return 0f;
            }

            float time = bandHeight / fallSpeed;
            if (lag <= 0f)
            {
                return windSpeed * time;   // 실리는 데 시간이 안 걸리면 빠지는 데도 안 걸린다 — 꼬리가 없다
            }

            float inBand = time >= lag
                ? windSpeed * (time - lag * 0.5f)               // 다 실린 뒤로는 그대로 흐른다
                : windSpeed * time * time / (2f * lag);         // 다 실리기 전에 빠져나간다

            if (tailHeight <= 0f)
            {
                return inBand;   // 구간 바닥이 곧 볼륨 바닥이면 꼬리가 펼쳐질 자리가 없다
            }

            float tailTime = tailHeight / fallSpeed;
            float ratioAtExit = time / lag;
            float driftAtExit = windSpeed * (ratioAtExit < 1f ? ratioAtExit : 1f);
            float decayTime = lag < time ? lag : time;   // 덜 실린 채로 나갔으면 빠지는 데도 그만큼만 걸린다

            float tail = tailTime >= decayTime
                ? driftAtExit * decayTime * 0.5f                                              // 꼬리가 다 펼쳐진다
                : driftAtExit * tailTime - (windSpeed / lag) * tailTime * tailTime * 0.5f;     // 다음 구간에서 잘린다

            return inBand + tail;
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
        /// 바람에 밀린 자리에서 구멍까지 자기 힘으로 얼마나 <b>모자라나</b>. 0 이하면 닿는다.
        /// 순풍이면 밀린 만큼이 이득이고 역풍이면 그만큼 더 가야 하는데, 그 둘이 이 뺄셈 하나로
        /// 같이 나온다.
        ///
        /// <para>"닿나/안 닿나"가 아니라 <b>얼마나 모자라나</b>를 돌려주는 이유는, 굽기 검사가
        /// 여유·부족을 리포트에 숫자로 적어야 하기 때문이다. 참/거짓만 주면 부르는 쪽이 같은
        /// 뺄셈을 한 벌 더 갖게 되고, 그렇게 갈라진 사본이 실제로 사고를 냈다.</para>
        /// </summary>
        public static float Shortfall(float requiredX, float requiredZ,
                                      float driftX, float driftZ, float selfReach)
        {
            float dx = requiredX - driftX;
            float dz = requiredZ - driftZ;
            return (float)System.Math.Sqrt(dx * dx + dz * dz) - selfReach;
        }
    }
}
