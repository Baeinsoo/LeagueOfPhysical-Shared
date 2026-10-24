namespace LOP
{
    /// <summary>
    /// 래그 보상: 쏜 사람 화면에서 맞았다면 맞은 것으로 쳐 주기 위해, 서버가 상대를 얼마나 옛 모습으로 되감을지 정하는 식.
    /// </summary>
    public static class MountedArcheryLagCompensation
    {
        /// <summary>
        /// 쏜 틱과 쏜 사람 화면 틱의 차이를 0~상한으로 자른 되감기 폭.
        /// 화면 틱이 쏜 틱보다 미래면 0(조작된 값을 되감기로 쓰지 못하게), 상한보다 오래되면 상한.
        /// </summary>
        public static int RewindTicks(long fireTick, long viewTick, int maxTicks)
        {
            long diff = fireTick - viewTick;
            if (diff < 0) return 0;
            if (diff > maxTicks) return maxTicks;
            return (int)diff;
        }

        /// <summary>
        /// 되감아 볼 틱. 기록이 그만큼 남아 있지 않으면 가장 오래된 기록으로 자른다.
        /// 기록 범위를 모르면(<c>null</c>) 되감지 않고 그 틱 그대로 쓴다.
        /// </summary>
        public static long RewindedTick(long tick, int rewindTicks, long? firstSavedTick)
        {
            long wanted = tick - rewindTicks;
            long floor = firstSavedTick ?? tick;
            return wanted > floor ? wanted : floor;
        }
    }
}
