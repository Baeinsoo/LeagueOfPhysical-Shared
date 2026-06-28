using System;

namespace LOP
{
    /// <summary>
    /// <see cref="StatusEffectApplyEffect"/> 핸들러(코어). Active 진입 시 효과 id를 설정으로 resolve해
    /// <see cref="StatusEffectSystem.Apply"/>. 적용된 효과는 독립 <see cref="StatusEffects"/>로 살아간다(수명 분리).
    /// <para>resolve(MasterData)는 <c>resolver</c> 델리게이트 심으로 주입 — 코어는 MasterData를 직접 참조하지 않는다.</para>
    /// </summary>
    public class StatusEffectApplyEffectHandler : AbilityEffectHandler<StatusEffectApplyEffect>
    {
        private readonly StatusEffectSystem _statusEffectSystem;
        private readonly Func<int, StatusEffectData?> _resolver;

        public StatusEffectApplyEffectHandler(StatusEffectSystem statusEffectSystem, Func<int, StatusEffectData?> resolver)
        {
            _statusEffectSystem = statusEffectSystem;
            _resolver = resolver;
        }

        protected override void OnActiveEnter(AbilityEffectContext ctx, StatusEffectApplyEffect effect)
        {
            var data = _resolver(effect.StatusEffectId);
            if (data == null)
            {
                return;
            }
            _statusEffectSystem.Apply(ctx.Target, data.Value, ctx.Caster.Id, ctx.CurrentTick);
        }
    }
}
