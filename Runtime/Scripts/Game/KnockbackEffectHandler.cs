namespace LOP
{
    /// <summary>
    /// <see cref="KnockbackEffect"/> 핸들러(공유 구현, 서버만 등록). Active 진입 시 1회, 시전자 정면 부채꼴 대상을
    /// 공격자 반대 방향으로 미는 Additive 기여를 대상 <see cref="MotionContributions"/>에 등록한다.
    /// 엔진 물리(범위 검색)는 <see cref="GameFramework.IOverlapQuery"/>, 위치는 World.Transform(진실원본, System.Numerics).
    /// 클라 미등록 → 클라는 스냅샷으로 결과 수신(넉백 = 서버권위). 판정은 <see cref="AttackSector"/>(Damage와 공유).
    /// </summary>
    public class KnockbackEffectHandler : AbilityEffectHandler<KnockbackEffect>
    {
        private readonly GameFramework.IOverlapQuery overlapQuery;
        private readonly GameFramework.World.EntityRegistry entityRegistry;

        public KnockbackEffectHandler(GameFramework.IOverlapQuery overlapQuery,
                                      GameFramework.World.EntityRegistry entityRegistry)
        {
            this.overlapQuery = overlapQuery;
            this.entityRegistry = entityRegistry;
        }

        protected override void OnActiveEnter(AbilityEffectContext ctx, KnockbackEffect effect)
        {
            GameFramework.World.Transform casterTransform = ctx.Caster?.Get<GameFramework.World.Transform>();
            if (casterTransform == null)
            {
                return;
            }

            string[] hitIds = overlapQuery.OverlapSphere(casterTransform.Position, effect.Range);
            foreach (string id in hitIds)
            {
                if (id == ctx.Caster.Id)
                {
                    continue;   // 자기제외
                }

                GameFramework.World.Entity target = entityRegistry.Get(id);
                GameFramework.World.Transform targetTransform = target?.Get<GameFramework.World.Transform>();
                if (targetTransform == null)
                {
                    continue;
                }
                if (!AttackSector.Contains(casterTransform, targetTransform.Position, effect.Range, effect.Angle))
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
