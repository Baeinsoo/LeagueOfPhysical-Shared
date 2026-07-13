using GameFramework.World;

namespace LOP
{
    /// <summary>
    /// effect 핸들러에 넘기는 발동 맥락(누가/누구에게/언제). 핸들러가 필요한 것만 읽는다.
    /// id→엔티티 조회가 필요한 핸들러는 <see cref="EntityRegistry"/>/<c>IOverlapQuery</c>를 DI로 받는다
    /// (velocity 권위가 World.Entity로 이전된 뒤 host EntityManager 탈출구는 제거됨).
    /// </summary>
    public readonly struct AbilityEffectContext
    {
        public readonly Entity Caster;
        public readonly Entity Target;
        public readonly long CurrentTick;
        public readonly int EffectIndex;   // 효과 리스트 내 위치 — 결정론 RNG sub-stream 구분용

        public AbilityEffectContext(Entity caster, Entity target, long currentTick, int effectIndex)
        {
            Caster = caster;
            Target = target;
            CurrentTick = currentTick;
            EffectIndex = effectIndex;
        }
    }
}
