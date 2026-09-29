namespace LOP
{
    /// <summary>
    /// 대시 동안의 전진 배율. 누른 순간 <c>peak</c>배로 튀어 나가 대부분 그 속도를 유지하다가 끝 30%에서 곧게
    /// 기본 속도(1배)로 돌아온다 — 카트라이더·마리오카트 부스터 모양. 곧게만 줄이면 부드럽게 밀리는 느낌이었다
    /// (2026-09-28 플레이 피드백). 이동과 맵 도구가 같은 곡선을 보도록 여기 한 곳에만 둔다.
    /// </summary>
    public static class FlappyDashCurve
    {
        /// <summary>최고 배율을 유지하는 몫. 남은 시간이 대시 길이의 30% 밑으로 떨어질 때부터 곧게 1배로 내려온다.</summary>
        public const float HoldShare = 0.7f;

        /// <param name="remaining">대시가 남은 시간(초).</param>
        /// <param name="dashDuration">대시 한 번의 길이(초). 곡선은 이 길이에 맞춰 줄어든다 —
        /// 패드가 이보다 길게 붙여도 남은 시간이 이 길이를 넘는 동안은 <c>peak</c>에 머문다.</param>
        public static float Multiplier(float remaining, float dashDuration, float peak)
        {
            if (dashDuration <= 0f)
            {
                return peak;
            }
            float t = remaining / dashDuration;
            if (t > 1f)
            {
                t = 1f;
            }
            else if (t < 0f)
            {
                t = 0f;
            }
            float tail = 1f - HoldShare;
            if (t >= tail)
            {
                return peak;
            }
            return 1f + (peak - 1f) * (t / tail);
        }

        /// <summary>
        /// 남은 시간 <paramref name="boostSeconds"/>로 시작한 대시가 끝날 때까지 나아가는 거리(m).
        /// 월드가 틱마다 남은 시간을 먼저 줄이고 그 값으로 움직이므로, 같은 순서로 틱을 더한다.
        /// </summary>
        public static float Distance(float forwardSpeed, float boostSeconds, float dashDuration, float peak,
                                     float tickSeconds)
        {
            if (tickSeconds <= 0f)
            {
                return 0f;
            }
            float distance = 0f;
            for (float r = boostSeconds; r > FlappyTickDuration.Epsilon; r -= tickSeconds)
            {
                distance += forwardSpeed * Multiplier(r, dashDuration, peak) * tickSeconds;
            }
            return distance;
        }
    }
}
