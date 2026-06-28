using System;

namespace LOP
{
    /// <summary>
    /// 한 effect 타입을 실제로 처리하는 핸들러(포트). <see cref="AbilityEffectExecutor"/>가 타입으로 골라 호출한다.
    /// 구현은 레이어별(코어 순수 / 모션·데미지=side 엔진)로 두고, DI에서 사이드별로 등록한다.
    /// <para>cadence: <see cref="OnActiveEnter"/>=Active 진입 1회(데미지·상태효과), <see cref="OnActiveTick"/>=Active 매 틱(대시).</para>
    /// </summary>
    public interface IAbilityEffectHandler
    {
        Type EffectType { get; }
        void OnActiveEnter(AbilityEffectContext ctx, AbilityEffect effect);
        void OnActiveTick(AbilityEffectContext ctx, AbilityEffect effect);
    }

    /// <summary>타입 안전 편의 베이스 — 구현은 자기 타입 T만 받는 오버라이드를 채운다(필요한 cadence만).</summary>
    public abstract class AbilityEffectHandler<T> : IAbilityEffectHandler where T : AbilityEffect
    {
        public Type EffectType => typeof(T);

        public void OnActiveEnter(AbilityEffectContext ctx, AbilityEffect effect) => OnActiveEnter(ctx, (T)effect);
        public void OnActiveTick(AbilityEffectContext ctx, AbilityEffect effect) => OnActiveTick(ctx, (T)effect);

        protected virtual void OnActiveEnter(AbilityEffectContext ctx, T effect) { }
        protected virtual void OnActiveTick(AbilityEffectContext ctx, T effect) { }
    }
}
