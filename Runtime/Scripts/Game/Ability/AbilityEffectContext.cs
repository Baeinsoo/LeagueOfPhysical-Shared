using GameFramework.World;

namespace LOP
{
    /// <summary>
    /// effect 핸들러에 넘기는 발동 맥락(누가/누구에게/언제 + 발동당 히트 결과). 핸들러가 필요한 것만 읽는다.
    /// <see cref="HitContext"/>는 히트 정의자(데미지)가 명중 대상을 기록하고 on-hit 라이더(넉백)가 읽는 공유 채널.
    /// </summary>
    public readonly struct AbilityEffectContext
    {
        public readonly Entity Caster;
        public readonly Entity Target;
        public readonly long CurrentTick;
        public readonly int EffectIndex;   // 효과 리스트 내 위치 — 크리 RNG sub-stream 구분용
        public readonly AttackHitContext HitContext;

        public AbilityEffectContext(Entity caster, Entity target, long currentTick, int effectIndex, AttackHitContext hitContext)
        {
            Caster = caster;
            Target = target;
            CurrentTick = currentTick;
            EffectIndex = effectIndex;
            HitContext = hitContext;
        }
    }
}
