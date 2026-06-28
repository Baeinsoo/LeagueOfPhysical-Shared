using GameFramework;
using GameFramework.World;

namespace LOP
{
    /// <summary>
    /// effect 핸들러에 넘기는 발동 맥락(누가/누구에게/언제 + 엔티티 조회). 핸들러가 필요한 것만 읽는다.
    /// <para><see cref="EntityManager"/>는 host가 채운다(핸들러가 id→side 엔티티/Rigidbody를 잡도록) —
    /// 핸들러에 DI로 주입하면 world-graph ↔ entity-manager 순환이 생겨서, ctx로 전달한다.</para>
    /// </summary>
    public readonly struct AbilityEffectContext
    {
        public readonly Entity Caster;
        public readonly Entity Target;
        public readonly long CurrentTick;
        public readonly IEntityManager EntityManager;

        public AbilityEffectContext(Entity caster, Entity target, long currentTick, IEntityManager entityManager)
        {
            Caster = caster;
            Target = target;
            CurrentTick = currentTick;
            EntityManager = entityManager;
        }
    }
}
