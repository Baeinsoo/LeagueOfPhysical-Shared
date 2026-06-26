using System.Collections.Generic;
using GameFramework.World;

namespace LOP
{
    /// <summary>활성 상태이상 인스턴스(런타임 데이터). 로직은 <see cref="StatusEffectSystem"/>에 둔다(Anemic).</summary>
    public readonly struct ActiveEffect
    {
        public readonly int EffectId;
        public readonly long ExpireTick;        // Duration 한정. Infinite면 -1
        public readonly int StackCount;
        public readonly string SourceEntityId;  // 귀속(instigator)
        public readonly string SourceId;        // "se:{EffectId}" — StatModifier.SourceId 링크

        public ActiveEffect(int effectId, long expireTick, int stackCount, string sourceEntityId, string sourceId)
        {
            EffectId = effectId;
            ExpireTick = expireTick;
            StackCount = stackCount;
            SourceEntityId = sourceEntityId;
            SourceId = sourceId;
        }
    }

    /// <summary>엔티티에 적용 중인 상태이상 컬렉션(데이터 컴포넌트).</summary>
    public class StatusEffects : Component
    {
        public List<ActiveEffect> Effects { get; } = new List<ActiveEffect>();
    }
}
