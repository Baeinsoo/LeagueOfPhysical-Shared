using System;

namespace LOP
{
    /// <summary>
    /// <see cref="StatusEffectApplyEffect"/> 핸들러(코어). Active 진입 시 효과 id를 설정으로 resolve해
    /// <see cref="StatusEffectSystem.Apply"/>. 적용된 효과는 독립 <see cref="StatusEffects"/>로 살아간다(수명 분리).
    /// <para>대상은 effect의 <see cref="TargetType"/>가 정한다 — Self는 시전자, HitTargets는 이번 발동에서
    /// 명중한 대상 전원(넉백과 같은 on-hit 라이더).</para>
    /// <para>resolve(MasterData)는 <c>resolver</c> 델리게이트 심으로 주입 — 코어는 MasterData를 직접 참조하지 않는다.</para>
    /// </summary>
    public class StatusEffectApplyEffectHandler : AbilityEffectHandler<StatusEffectApplyEffect>
    {
        private readonly StatusEffectSystem _statusEffectSystem;
        private readonly Func<int, StatusEffectData?> _resolver;
        private readonly GameFramework.World.EntityRegistry _entityRegistry;

        public StatusEffectApplyEffectHandler(StatusEffectSystem statusEffectSystem,
                                              Func<int, StatusEffectData?> resolver,
                                              GameFramework.World.EntityRegistry entityRegistry)
        {
            _statusEffectSystem = statusEffectSystem;
            _resolver = resolver;
            _entityRegistry = entityRegistry;
        }

        protected override void OnActiveEnter(AbilityEffectContext ctx, StatusEffectApplyEffect effect)
        {
            var data = _resolver(effect.StatusEffectId);
            if (data == null)
            {
                return;
            }

            if (effect.Target == TargetType.Self)
            {
                if (ctx.Caster != null)
                {
                    _statusEffectSystem.Apply(ctx.Caster, data.Value, ctx.Caster.Id, ctx.CurrentTick);
                }
                return;
            }

            if (ctx.HitContext == null || ctx.Caster == null)
            {
                return;
            }
            foreach (string id in ctx.HitContext.LandedTargets)
            {
                GameFramework.World.Entity target = _entityRegistry.Get(id);
                if (target != null)
                {
                    _statusEffectSystem.Apply(target, data.Value, ctx.Caster.Id, ctx.CurrentTick);
                }
            }
        }
    }
}
