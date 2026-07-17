namespace LOP
{
    /// <summary>
    /// <see cref="KnockbackEffect"/> 핸들러(공유 구현, 서버만 등록) — on-hit 라이더. 히트 정의자(데미지)가
    /// <see cref="AttackHitContext"/>에 기록한 명중 대상만 공격자 반대 방향으로 미는 Additive 기여를 등록한다.
    /// 자체 범위 탐색/닷지 판정 없음(히트 형상·명중 여부는 데미지가 정함). 위치는 World.Transform(진실원본).
    /// 클라 미등록 → 클라는 스냅샷으로 결과 수신(넉백 = 서버권위).
    /// </summary>
    public class KnockbackEffectHandler : AbilityEffectHandler<KnockbackEffect>
    {
        private readonly GameFramework.World.EntityRegistry entityRegistry;

        public KnockbackEffectHandler(GameFramework.World.EntityRegistry entityRegistry)
        {
            this.entityRegistry = entityRegistry;
        }

        protected override void OnActiveEnter(AbilityEffectContext ctx, KnockbackEffect effect)
        {
            AttackHitContext hit = ctx.HitContext;
            if (hit == null)
            {
                return;
            }
            GameFramework.World.Transform casterTransform = ctx.Caster?.Get<GameFramework.World.Transform>();
            if (casterTransform == null)
            {
                return;
            }

            foreach (string id in hit.LandedTargets)
            {
                GameFramework.World.Entity target = entityRegistry.Get(id);
                GameFramework.World.Transform targetTransform = target?.Get<GameFramework.World.Transform>();
                if (targetTransform == null)
                {
                    continue;
                }
                MotionContributions contributions = target.Get<MotionContributions>();
                if (contributions == null)
                {
                    continue;
                }
                contributions.Items.Add(MotionContributionSystem.CreateRadialKnockback(
                    casterTransform.Position, targetTransform.Position,
                    effect.Strength, effect.DurationTicks, effect.DecayPerTick, ctx.CurrentTick));
            }
        }
    }
}
