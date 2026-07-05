using System.Numerics;

namespace LOP
{
    /// <summary>이동 시스템이 base velocity 위에 얹는 기여의 합성 방식.</summary>
    public enum MotionContributionMode { Override, Additive }

    /// <summary>
    /// 이동 시스템에 얹히는 수평 velocity 기여 하나(순수 데이터). 활성 창 <c>[StartTick, EndTick)</c> 동안만 적용.
    /// Additive는 <c>DecayPerTick</c>로 매 틱 지수 감쇠(=물리 drag의 순수 함수). 산업 표준: Unreal CMC RootMotionSource.
    /// </summary>
    public readonly struct MotionContribution
    {
        public readonly Vector3 Horizontal;              // 수평(x,z) 초기 임펄스 v0; y 미사용
        public readonly MotionContributionMode Mode;
        public readonly int Priority;                    // Override 경합 시 큰 값 우선
        public readonly long StartTick;
        public readonly long EndTick;                    // 활성 = StartTick <= tick < EndTick
        public readonly float DecayPerTick;              // Additive 감쇠 계수 k(0<k<=1). 1=상수

        public MotionContribution(Vector3 horizontal, MotionContributionMode mode, int priority, long startTick, long endTick)
            : this(horizontal, mode, priority, startTick, endTick, 1f)
        {
        }

        public MotionContribution(Vector3 horizontal, MotionContributionMode mode, int priority, long startTick, long endTick, float decayPerTick)
        {
            Horizontal = horizontal;
            Mode = mode;
            Priority = priority;
            StartTick = startTick;
            EndTick = endTick;
            DecayPerTick = decayPerTick;
        }

        public bool IsActiveAt(long tick) => tick >= StartTick && tick < EndTick;
    }
}
