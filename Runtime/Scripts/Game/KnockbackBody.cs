namespace LOP
{
    /// <summary>넉백 생존전 몸의 피해 쌓임(%)과 "날려지는 중"이 끝나는 틱.</summary>
    public sealed class KnockbackBody : GameFramework.World.Component
    {
        public int DamagePercent;
        // 이 틱보다 앞이면 날려지는 중. 기본값은 "한 번도 안 맞음".
        public long KnockbackEndTick = long.MinValue;
    }
}
