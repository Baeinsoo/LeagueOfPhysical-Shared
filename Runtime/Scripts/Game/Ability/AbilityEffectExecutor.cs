using System;
using System.Collections.Generic;
using GameFramework;
using GameFramework.World;

namespace LOP
{
    /// <summary>
    /// 어빌리티의 effect 리스트를 순회하며 각 effect를 그 타입의 핸들러로 디스패치한다(단일 순회·단일 디스패치).
    /// WoW Spell::EffectXXX 디스패치 테이블 / GAS Execution 대응. 핸들러 없는 타입은 무시
    /// (예: 클라엔 DamageEffectHandler 미등록 = 데미지 서버권위).
    /// </summary>
    public class AbilityEffectExecutor
    {
        private readonly Dictionary<Type, IAbilityEffectHandler> _handlers = new Dictionary<Type, IAbilityEffectHandler>();

        public AbilityEffectExecutor(IEnumerable<IAbilityEffectHandler> handlers)
        {
            if (handlers == null)
            {
                return;
            }
            foreach (var handler in handlers)
            {
                _handlers[handler.EffectType] = handler;
            }
        }

        /// <summary>
        /// 한 엔티티의 진행 중 어빌리티를 보고 Active 창 effect를 디스패치한다(host가 매 틱·엔티티마다 호출).
        /// Active 진입 틱(=StartupEndTick)에 OnActiveEnter 1회 + Active 동안 매 틱 OnActiveTick.
        /// 페이즈 전진은 <see cref="AbilitySystem.Tick"/>(world.Tick)이 먼저 끝낸 상태를 읽는다.
        /// entityManager는 ctx로 전달(핸들러 DI 순환 회피).
        /// </summary>
        public void DriveActiveEntity(Entity caster, IEntityManager entityManager, long currentTick)
        {
            var active = caster?.Get<Abilities>()?.ActiveAbility;
            if (active == null || active.Value.Phase != AbilityPhase.Active)
            {
                return;
            }
            var ctx = new AbilityEffectContext(caster, active.Value.Target, currentTick, entityManager);
            if (currentTick == active.Value.StartupEndTick)
            {
                OnActiveEnter(ctx, active.Value.Effects);   // 진입 1회 — 데미지·상태효과(즉발)
            }
            OnActiveTick(ctx, active.Value.Effects);         // 매 틱 — 대시 push(지속)
        }

        /// <summary>Active 진입 시 1회 — 데미지 판정·상태효과 적용 등 즉발 effect.</summary>
        public void OnActiveEnter(AbilityEffectContext ctx, AbilityEffect[] effects)
        {
            if (effects == null)
            {
                return;
            }
            foreach (var effect in effects)
            {
                if (_handlers.TryGetValue(effect.GetType(), out var handler))
                {
                    handler.OnActiveEnter(ctx, effect);
                }
            }
        }

        /// <summary>Active 동안 매 틱 — 대시 push 등 지속 effect.</summary>
        public void OnActiveTick(AbilityEffectContext ctx, AbilityEffect[] effects)
        {
            if (effects == null)
            {
                return;
            }
            foreach (var effect in effects)
            {
                if (_handlers.TryGetValue(effect.GetType(), out var handler))
                {
                    handler.OnActiveTick(ctx, effect);
                }
            }
        }
    }
}
