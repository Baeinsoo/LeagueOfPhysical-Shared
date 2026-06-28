using GameFramework.World;

namespace LOP
{
    /// <summary>
    /// 어빌리티 로직(상태 없음). GAS 생명주기(CanActivate→Commit→페이즈 머신)를 anemic으로 구현.
    /// 발동은 Startup→Active→Recovery 시간 페이즈(격투 frame data, 시뮬 틱 구동). Active 창에서
    /// effect 리스트를 <see cref="AbilityEffectExecutor"/>로 디스패치(진입 1회 + 매 틱).
    /// 쿨다운은 절대 end-tick(파생 readiness). busy = ActiveAbility 진행 중.
    /// </summary>
    public class AbilitySystem
    {
        private readonly ManaSystem _manaSystem;

        public AbilitySystem(ManaSystem manaSystem)
        {
            _manaSystem = manaSystem;
        }

        /// <summary>
        /// 지금 Active 페이즈인 이동 어빌리티(MotionEffect 보유)를 쓰고 있나? — 대시 중 입력/조종 잠금 판정용.
        /// (side의 입력 캡처·이동 매니저가 호출. 순수 읽기 — 핸들러가 모션을 구동하는 동안 플레이어 입력을 막는다.)
        /// </summary>
        public static bool HasActiveMotionEffect(Entity entity)
        {
            var active = entity?.Get<Abilities>()?.ActiveAbility;
            if (active == null || active.Value.Phase != AbilityPhase.Active || active.Value.Effects == null)
            {
                return false;
            }
            foreach (var effect in active.Value.Effects)
            {
                if (effect is MotionEffect)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>어빌리티를 엔티티에 부여한다(ready 슬롯 추가).</summary>
        public void Grant(Entity entity, int abilityId)
        {
            var abilities = entity.Get<Abilities>();
            if (abilities == null)
            {
                return;
            }
            abilities.Slots[abilityId] = new AbilitySlot(abilityId, 0);
        }

        /// <summary>발동 가능 여부(GAS CanActivateAbility): 보유 + not busy + 쿨다운 ready + 자원 충분. 순수 읽기.</summary>
        public bool CanActivate(Entity caster, in AbilityData data, long currentTick)
        {
            var abilities = caster.Get<Abilities>();
            if (abilities == null || !abilities.Slots.TryGetValue(data.AbilityId, out var slot))
            {
                return false;
            }
            if (abilities.ActiveAbility != null)
            {
                return false;   // busy — 다른 발동 진행 중(Startup/Active/Recovery)
            }
            if (currentTick < slot.CooldownEndTick)
            {
                return false;
            }
            if (data.MpCost > 0)
            {
                var mana = caster.Get<Mana>();
                if (mana == null || mana.Current < data.MpCost)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 어빌리티를 발동한다. CanActivate면 Commit(코스트 차감 + 쿨다운 설정) 후 페이즈 머신을 시작하고 true.
        /// effect는 *여기서 적용하지 않고* Active 창에서 <see cref="Tick"/>이 executor로 디스패치한다. 아니면 부수효과 없이 false.
        /// </summary>
        public bool TryActivate(Entity caster, in AbilityData data, Entity target, long currentTick)
        {
            if (!CanActivate(caster, data, currentTick))
            {
                return false;
            }

            // Commit — 코스트 차감 + 쿨다운 설정
            if (data.MpCost > 0)
            {
                _manaSystem.Spend(caster.Get<Mana>(), data.MpCost);
            }
            var abilities = caster.Get<Abilities>();
            abilities.Slots[data.AbilityId] = new AbilitySlot(data.AbilityId, currentTick + data.CooldownTicks);

            // 페이즈 머신 시작 — 경계를 절대 틱으로 확정. effect는 Active 창에서 Tick이 디스패치.
            long startupEnd = currentTick + data.StartupTicks;
            long activeEnd = startupEnd + data.ActiveTicks;
            long recoveryEnd = activeEnd + data.RecoveryTicks;
            abilities.ActiveAbility = new ActiveAbility(data.AbilityId, AbilityPhase.Startup,
                startupEnd, activeEnd, recoveryEnd, target, data.Effects);
            return true;
        }

        /// <summary>
        /// 진행 중인 <see cref="ActiveAbility"/>의 페이즈를 전진(매 틱, world.Tick에서). Startup→Active→Recovery,
        /// Recovery 종료 시 Ready(null). ActiveAbility 없으면 no-op.
        /// <para>effect 적용은 여기서 하지 않는다 — host가 페이즈 전진 후 <see cref="AbilityEffectExecutor.DriveActiveEntity"/>로
        /// 구동한다(핸들러가 entityManager 등 side 자원을 ctx로 받아야 해서, DI 순환을 피하려 host-driven).</para>
        /// </summary>
        public void Tick(Entity entity, long currentTick)
        {
            var abilities = entity.Get<Abilities>();
            if (abilities?.ActiveAbility == null)
            {
                return;
            }

            var active = abilities.ActiveAbility.Value;
            switch (active.Phase)
            {
                case AbilityPhase.Startup:
                    if (currentTick >= active.StartupEndTick)
                    {
                        abilities.ActiveAbility = active.WithPhase(AbilityPhase.Active);
                    }
                    break;
                case AbilityPhase.Active:
                    if (currentTick >= active.ActiveEndTick)
                    {
                        abilities.ActiveAbility = active.WithPhase(AbilityPhase.Recovery);
                    }
                    break;
                case AbilityPhase.Recovery:
                    if (currentTick >= active.RecoveryEndTick)
                    {
                        abilities.ActiveAbility = null;
                    }
                    break;
            }
        }
    }
}
