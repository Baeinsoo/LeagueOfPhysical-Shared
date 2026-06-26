using GameFramework.World;

namespace LOP
{
    /// <summary>
    /// 어빌리티 로직(상태 없음). GAS 생명주기(CanActivate→Commit(코스트+쿨다운)→효과 생성)를 anemic으로 구현.
    /// 쿨다운은 절대 end-tick(파생 readiness, per-tick 갱신 없음). 효과는 StatusEffectSystem이 적용(이음새).
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

        /// <summary>발동 가능 여부(GAS CanActivateAbility): 보유 + 쿨다운 ready + 자원 충분. 순수 읽기.</summary>
        public bool CanActivate(Entity caster, in AbilityData data, long currentTick)
        {
            var abilities = caster.Get<Abilities>();
            if (abilities == null || !abilities.Slots.TryGetValue(data.AbilityId, out var slot))
            {
                return false;
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
        /// 어빌리티를 발동한다. CanActivate면 Commit(코스트 차감 + 쿨다운 설정) 후 producedEffects를 타깃에 적용하고 true.
        /// 아니면 부수효과 없이 false. producedEffects = 호출자가 data.ProducesEffectIds로 resolve한 효과 설정.
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

            // 효과 생성(이음새) — 즉발. 캐스트 경로는 후속.
            if (producedEffects != null)
            {
                string casterId = caster.Id;
                foreach (var effect in producedEffects)
                {
                    _statusEffectSystem.Apply(target, effect, casterId, currentTick);
                }
            }
            return true;
        }
    }
}
