using GameFramework.World;

namespace LOP
{
    /// <summary>
    /// 어빌리티 로직(상태 없음). GAS 생명주기(CanActivate→Commit→페이즈 머신)를 anemic으로 구현.
    /// 발동은 Startup→Active→Recovery 시간 페이즈(격투 frame data, 시뮬 틱 구동). 효과는 Active 진입 시 적용.
    /// 쿨다운은 절대 end-tick(파생 readiness). busy = ActiveAbility 진행 중.
    /// </summary>
    public class AbilitySystem
    {
        private readonly ManaSystem _manaSystem;
        private readonly StatusEffectSystem _statusEffectSystem;

        public AbilitySystem(ManaSystem manaSystem, StatusEffectSystem statusEffectSystem)
        {
            _manaSystem = manaSystem;
            _statusEffectSystem = statusEffectSystem;
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
        /// 효과는 *여기서 적용하지 않고* Active 진입 시 <see cref="Tick"/>이 적용한다. 아니면 부수효과 없이 false.
        /// producedEffects = 호출자가 data.ProducesEffectIds로 resolve한 효과 설정.
        /// </summary>
        public bool TryActivate(Entity caster, in AbilityData data, Entity target,
                                StatusEffectData[] producedEffects, long currentTick)
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

            // 페이즈 머신 시작 — 경계를 절대 틱으로 확정. 효과는 Active 진입 시 Tick이 적용.
            long startupEnd = currentTick + data.StartupTicks;
            long activeEnd = startupEnd + data.ActiveTicks;
            long recoveryEnd = activeEnd + data.RecoveryTicks;
            abilities.ActiveAbility = new ActiveAbility(data.AbilityId, AbilityPhase.Startup,
                startupEnd, activeEnd, recoveryEnd, target, producedEffects);
            return true;
        }

        /// <summary>
        /// 진행 중인 <see cref="ActiveAbility"/>의 페이즈를 전진(매 틱). Startup→Active 전이 시 PendingEffects를
        /// 적용, Active→Recovery, Recovery 종료 시 Ready(null). ActiveAbility 없으면 no-op.
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
                        if (active.PendingEffects != null)
                        {
                            string casterId = entity.Id;
                            foreach (var effect in active.PendingEffects)
                            {
                                _statusEffectSystem.Apply(active.Target, effect, casterId, currentTick);
                            }
                        }
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
