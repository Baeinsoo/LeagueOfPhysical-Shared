using System.Numerics;

namespace LOP
{
    /// <summary>모터가 base velocity 위에 얹는 기여의 합성 방식.</summary>
    public enum MotionContributionMode { Override, Additive }

    /// <summary>
    /// 이동 모터에 얹히는 수평 velocity 기여 하나(순수 데이터). 활성 창 <c>[StartTick, EndTick)</c> 동안만 적용.
    /// 산업 표준: Unreal CMC RootMotionSource(AccumulateMode Override/Additive + Priority + Duration).
    /// </summary>
    public readonly struct MotionContribution
    {
        public readonly Vector3 Horizontal;              // 수평(x,z); y 미사용
        public readonly MotionContributionMode Mode;
        public readonly int Priority;                    // Override 경합 시 큰 값 우선
        public readonly long StartTick;
        public readonly long EndTick;                    // 활성 = StartTick <= tick < EndTick

        public MotionContribution(Vector3 horizontal, MotionContributionMode mode, int priority, long startTick, long endTick)
        {
            Horizontal = horizontal;
            Mode = mode;
            Priority = priority;
            StartTick = startTick;
            EndTick = endTick;
        }

        public bool IsActiveAt(long tick) => tick >= StartTick && tick < EndTick;
    }
}
