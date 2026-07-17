using System;
using System.Collections.Generic;
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
        /// </summary>
        public void DriveActiveEntity(Entity caster, long currentTick)
        {
            var active = caster?.Get<Abilities>()?.ActiveAbility;
            if (active == null || active.Value.Phase != AbilityPhase.Active)
            {
                return;
            }
            var hit = new AttackHitContext();   // 이 발동의 명중 대상 공유 채널
            var ctx = new AbilityEffectContext(caster, active.Value.Target, currentTick, 0, hit);
            if (currentTick == active.Value.StartupEndTick)
            {
                OnActiveEnter(ctx, active.Value.Effects);   // 진입 1회 — 데미지(히트 정의) → 넉백(라이더)
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
            for (int i = 0; i < effects.Length; i++)
            {
                if (_handlers.TryGetValue(effects[i].GetType(), out var handler))
                {
                    var effectCtx = new AbilityEffectContext(
                        ctx.Caster, ctx.Target, ctx.CurrentTick, i, ctx.HitContext);
                    handler.OnActiveEnter(effectCtx, effects[i]);
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
            for (int i = 0; i < effects.Length; i++)
            {
                if (_handlers.TryGetValue(effects[i].GetType(), out var handler))
                {
                    var effectCtx = new AbilityEffectContext(
                        ctx.Caster, ctx.Target, ctx.CurrentTick, i, ctx.HitContext);
                    handler.OnActiveTick(effectCtx, effects[i]);
                }
            }
        }
    }
}
