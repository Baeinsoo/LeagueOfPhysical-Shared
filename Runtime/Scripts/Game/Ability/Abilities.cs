using System.Collections.Generic;
using GameFramework.World;

namespace LOP
{
    /// <summary>
    /// 이 엔티티가 부여받은 어빌리티 하나의 런타임 상태(데이터). 기록의 존재 자체가 보유 증명이다.
    /// GAS의 FGameplayAbilitySpec 대응. 로직은 <see cref="AbilitySystem"/>에 둔다(Anemic).
    /// </summary>
    public readonly struct GrantedAbility
    {
        public readonly int AbilityId;
        public readonly long CooldownEndTick;   // currentTick >= 이 값이면 ready (초기 0)

        public GrantedAbility(int abilityId, long cooldownEndTick)
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

        /// <summary>
        /// 발동 전에 미리 지목한 대상. 현재 모든 어빌리티가 self 또는 광역 스윕이라 항상 시전자가
        /// 들어가며 읽는 곳이 없다 — 대상 지목형 스킬이 생길 때를 위한 자리.
        /// 명중해서 정해지는 대상은 여기가 아니라 <see cref="AttackHitContext.LandedTargets"/>에 있다.
        /// </summary>
        public readonly Entity Target;
        public readonly AbilityEffect[] Effects;
        public readonly float StartupMoveScale;
        public readonly float ActiveMoveScale;
        public readonly float RecoveryMoveScale;
        public readonly bool BlockJump;

        public ActiveAbility(int abilityId, AbilityPhase phase, long startupEndTick, long activeEndTick,
                             long recoveryEndTick, Entity target, AbilityEffect[] effects,
                             float startupMoveScale = 1f, float activeMoveScale = 1f,
                             float recoveryMoveScale = 1f, bool blockJump = false)
        {
            AbilityId = abilityId;
            Phase = phase;
            StartupEndTick = startupEndTick;
            ActiveEndTick = activeEndTick;
            RecoveryEndTick = recoveryEndTick;
            Target = target;
            Effects = effects;
            StartupMoveScale = startupMoveScale;
            ActiveMoveScale = activeMoveScale;
            RecoveryMoveScale = recoveryMoveScale;
            BlockJump = blockJump;
        }

        public ActiveAbility WithPhase(AbilityPhase phase)
            => new ActiveAbility(AbilityId, phase, StartupEndTick, ActiveEndTick, RecoveryEndTick, Target, Effects,
                                 StartupMoveScale, ActiveMoveScale, RecoveryMoveScale, BlockJump);

        /// <summary>
        /// 연출용 부분 복원 — 어빌리티 id와 페이즈 경계만 채운다(원격 엔티티 스냅샷 반영용).
        /// 효과 목록·이동 스케일·점프 봉인 같은 시뮬 파라미터는 비운다: 클라는 원격 어빌리티를 실행하지 않는다.
        /// Phase는 뷰가 <see cref="AbilityPlayback.Solve"/>로 매 프레임 다시 구하므로 의미 없는 초기값이다.
        /// </summary>
        public static ActiveAbility ForPresentation(int abilityId, long startupEndTick,
                                                    long activeEndTick, long recoveryEndTick)
        {
            return new ActiveAbility(abilityId, AbilityPhase.Startup,
                startupEndTick, activeEndTick, recoveryEndTick,
                null, System.Array.Empty<AbilityEffect>(), 1f, 1f, 1f, false);
        }
    }

    /// <summary>엔티티가 부여받은 어빌리티 집합(데이터 컴포넌트). AbilityId당 1개.</summary>
    public class Abilities : Component
    {
        public Dictionary<int, GrantedAbility> Granted { get; } = new Dictionary<int, GrantedAbility>();

        /// <summary>진행 중인 발동(없으면 null=Ready). 엔티티당 동시 1 — busy 판정.</summary>
        public ActiveAbility? ActiveAbility { get; set; }
    }
}
