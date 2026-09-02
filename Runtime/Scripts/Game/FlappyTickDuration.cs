namespace LOP
{
    /// <summary>
    /// 남은 시간(초)과 "끝나는 절대 틱" 사이의 변환. 와이어로는 남은 시간이 아니라 끝나는 틱을
    /// 보내므로(스냅이 늦게 도착해도 값이 낡지 않는다) 보내는 쪽과 받는 쪽이 이 한 곳을 쓴다.
    ///
    /// <para>같은 식을 두 벌 적으면 <b>올림 때문에 한 틱을 더 세는 실수</b>가 재발한다 — 스턴에서
    /// 실제로 겪었고, 받는 쪽 새만 한 틱 더 얼어 있게 만들었다. 그 한 틱은 전진과 중력을 통째로
    /// 잃어 "같은 틱인데 위치가 다른" 상태가 된다. 그래서 이 변환의 자리는 여기 하나다.</para>
    /// </summary>
    public static class FlappyTickDuration
    {
        /// <summary>
        /// 매 틱 float를 빼 나가면 정확히 0을 못 찍고 아주 조금(예: 2e-7) 남는다. 시뮬은 그 잔여를
        /// 끝으로 보는데 올림은 한 틱으로 세어 버리므로, 세기 전에 이만큼을 먼저 뺀다.
        /// </summary>
        public const float Epsilon = 1e-5f;

        /// <summary>남은 시간을 "끝나는 절대 틱"으로. 남은 것이 없으면 0.</summary>
        public static long EndTick(float remaining, long tick, float deltaTime)
        {
            if (remaining <= Epsilon || deltaTime <= 0f)
            {
                return 0;
            }
            return tick + (long)System.Math.Ceiling((remaining - Epsilon) / deltaTime);
        }

        /// <summary>끝나는 절대 틱에서 지금 틱을 빼 남은 시간(초)으로. 이미 지났거나 같으면 0.</summary>
        public static float RemainingSeconds(long endTick, long tick, float deltaTime)
        {
            long remainingTicks = endTick - tick;
            return remainingTicks > 0 ? remainingTicks * deltaTime : 0f;
        }
    }
}
