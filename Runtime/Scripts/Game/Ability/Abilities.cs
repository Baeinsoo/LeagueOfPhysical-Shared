using System.Collections.Generic;
using GameFramework.World;

namespace LOP
{
    /// <summary>부여된 어빌리티 하나의 런타임 상태(데이터). 로직은 <see cref="AbilitySystem"/>에 둔다(Anemic).</summary>
    public readonly struct AbilitySlot
    {
        public readonly int AbilityId;
        public readonly long CooldownEndTick;   // currentTick >= 이 값이면 ready (초기 0)

        public AbilitySlot(int abilityId, long cooldownEndTick)
        {
            AbilityId = abilityId;
            CooldownEndTick = cooldownEndTick;
        }
    }

    /// <summary>엔티티가 보유한 어빌리티 슬롯 집합(데이터 컴포넌트). AbilityId당 1 슬롯(InstancedPerActor).</summary>
    public class Abilities : Component
    {
        public Dictionary<int, AbilitySlot> Slots { get; } = new Dictionary<int, AbilitySlot>();
    }
}
