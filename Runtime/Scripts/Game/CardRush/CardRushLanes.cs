namespace LOP
{
    public static class CardRushLanes
    {
        public const int MaxLanes = 8;

        public static UnityEngine.Vector3 Flat(UnityEngine.Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude < 1e-8f ? UnityEngine.Vector3.forward : v.normalized;
        }

        /// <summary>레인 i의 출발점. 늘 8레인 기준이라 인원이 달라도 모양이 같다.</summary>
        public static UnityEngine.Vector3 LaneOrigin(UnityEngine.Vector3 marker, UnityEngine.Vector3 forward, float laneSpacing, int lane)
        {
            var right = UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, Flat(forward));
            return marker + right * ((lane - (MaxLanes - 1) * 0.5f) * laneSpacing);
        }

        /// <summary>칸 단위 진행. 이동 중엔 앞 칸에서 지금 칸으로 고르게 간다.</summary>
        public static float Progress(in CardRushRunnerState s, long tick, int moveTicks)
        {
            if (s.MoveStartTick != 0 && tick >= s.MoveStartTick && tick < s.MoveStartTick + moveTicks)
                return (s.Step - 1) + (float)(tick - s.MoveStartTick) / moveTicks;
            return s.Step;
        }

        public static UnityEngine.Vector3 PositionAt(UnityEngine.Vector3 laneOrigin, UnityEngine.Vector3 forward, float stepSpacing, float progress)
            => laneOrigin + Flat(forward) * (stepSpacing * progress);
    }
}
