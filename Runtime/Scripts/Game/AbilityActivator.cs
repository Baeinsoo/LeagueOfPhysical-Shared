namespace LOP
{
    /// <summary>
    /// 어빌리티 id로 발동을 라우팅한다(런타임 식별=int id).
    /// id가 마스터데이터에 있으면 <see cref="AbilitySystem.TryActivate"/>로 발동하고 true, 아니면 false.
    /// </summary>
    public class AbilityActivator
    {
        private readonly AbilitySystem abilitySystem;
        // 마스터데이터는 클·서가 서로 다른 패키지를 보므로(상호 비참조) 공용 코드가 직접 읽을 수 없다.
        // 그래서 조회만 사이드에서 받아 온다 — StatusEffectApplyEffectHandler가 쓰는 방식과 같다.
        private readonly System.Func<int, AbilityData?> resolveAbility;
        private readonly GameFramework.World.EntityRegistry entityRegistry;
        private readonly GameFramework.World.WorldEventBuffer worldEventBuffer;

        public AbilityActivator(
            AbilitySystem abilitySystem,
            System.Func<int, AbilityData?> resolveAbility,
            GameFramework.World.EntityRegistry entityRegistry,
            GameFramework.World.WorldEventBuffer worldEventBuffer)
        {
            this.abilitySystem = abilitySystem;
            this.resolveAbility = resolveAbility;
            this.entityRegistry = entityRegistry;
            this.worldEventBuffer = worldEventBuffer;
        }

        public bool TryActivate(string casterEntityId, int abilityId, long currentTick)
        {
            var ability = resolveAbility(abilityId);
            if (ability == null)
            {
                return false;
            }

            var caster = entityRegistry.Get(casterEntityId);
            if (caster == null)
            {
                return false;
            }

            // effect는 ability.Effects에 실려 있고, Active 창에서 executor가 타입별 핸들러로 디스패치한다.
            bool activated = abilitySystem.TryActivate(caster, ability.Value, caster, currentTick);
            if (activated)
            {
                // 발동 연출 cue — 플레이어·AI 모든 발동 경로가 여기로 모이므로 발화를 한 곳에서 한다.
                worldEventBuffer.Append(new GameFramework.World.AbilityActivatedEvent(casterEntityId, abilityId));
            }
            return activated;
        }

        /// <summary>슬롯에 장착된 어빌리티 id를 찾는다. 입력 캡처가 슬롯을 id로 풀 때 쓴다.</summary>
        public bool TryGetAbilityIdBySlot(string casterEntityId, int slot, out int abilityId)
        {
            abilityId = 0;
            var caster = entityRegistry.Get(casterEntityId);
            if (caster == null)
            {
                return false;
            }
            return abilitySystem.TryGetAbilityIdBySlot(caster, slot, out abilityId);
        }

        /// <summary>슬롯으로 발동. id를 푼 뒤 기존 <see cref="TryActivate"/> 경로로 합류한다.</summary>
        public bool TryActivateSlot(string casterEntityId, int slot, long currentTick)
        {
            if (TryGetAbilityIdBySlot(casterEntityId, slot, out int abilityId) == false)
            {
                return false;
            }
            return TryActivate(casterEntityId, abilityId, currentTick);
        }
    }
}
