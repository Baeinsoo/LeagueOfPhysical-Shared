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

    /// <summary>어빌리티 발동의 시간 페이즈(격투 frame data). null ⇔ Ready; <see cref="ActiveAbility"/>는 항상 Startup/Active/Recovery.</summary>
    public enum AbilityPhase { Ready, Startup, Active, Recovery }

    /// <summary>
    /// 진행 중인 어빌리티 발동 하나(transient). 엔티티당 동시 1. 페이즈 경계는 발동 시 절대 틱으로 확정.
    /// 데이터만 — 전진/적용 로직은 <see cref="AbilitySystem.Tick"/>.
    /// </summary>
    public readonly struct ActiveAbility
    {
        public readonly int AbilityId;
        public readonly AbilityPhase Phase;
        public readonly long StartupEndTick;
        public readonly long ActiveEndTick;
        public readonly long RecoveryEndTick;
        public readonly Entity Target;
        public readonly StatusEffectData[] PendingEffects;

        public ActiveAbility(int abilityId, AbilityPhase phase, long startupEndTick, long activeEndTick,
                             long recoveryEndTick, Entity target, StatusEffectData[] pendingEffects)
        {
            AbilityId = abilityId;
            Phase = phase;
            StartupEndTick = startupEndTick;
            ActiveEndTick = activeEndTick;
            RecoveryEndTick = recoveryEndTick;
            Target = target;
            PendingEffects = pendingEffects;
        }

        public ActiveAbility WithPhase(AbilityPhase phase)
            => new ActiveAbility(AbilityId, phase, StartupEndTick, ActiveEndTick, RecoveryEndTick, Target, PendingEffects);
    }

    /// <summary>엔티티가 보유한 어빌리티 슬롯 집합(데이터 컴포넌트). AbilityId당 1 슬롯(InstancedPerActor).</summary>
    public class Abilities : Component
    {
        public Dictionary<int, AbilitySlot> Slots { get; } = new Dictionary<int, AbilitySlot>();

        /// <summary>진행 중인 발동(없으면 null=Ready). 엔티티당 동시 1 — busy 판정.</summary>
        public ActiveAbility? ActiveAbility { get; set; }
    }
}
