namespace LOP
{
    /// <summary>
    /// <see cref="DamageEffect"/> 핸들러(클·서 공유). Active 진입 시 1회, 시전자 정면 부채꼴 안의 대상을 때린다.
    /// 판정 위치는 World.Transform(진실원본, System.Numerics) — 엔진 좌표 대신. 엔진 물리(범위 검색)는
    /// IOverlapQuery(사이드 구체)에 위임하고, 해소(데미지/크리/회피)는 LOPCombatSystem(공유)이 한다.
    /// 클라 등록·예측 소비는 A2.4 — 지금은 서버만 등록(데미지 서버권위).
    /// </summary>
    public class DamageEffectHandler : AbilityEffectHandler<DamageEffect>
    {
        private readonly LOPCombatSystem combatSystem;
        private readonly GameFramework.Physics.IOverlapQuery overlapQuery;
        private readonly IMatchSeed matchSeed;
        private readonly GameFramework.World.EntityRegistry entityRegistry;

        public DamageEffectHandler(LOPCombatSystem combatSystem,
                                   GameFramework.Physics.IOverlapQuery overlapQuery,
                                   IMatchSeed matchSeed,
                                   GameFramework.World.EntityRegistry entityRegistry)
        {
            this.combatSystem = combatSystem;
            this.overlapQuery = overlapQuery;
            this.matchSeed = matchSeed;
            this.entityRegistry = entityRegistry;
        }

        protected override void OnActiveEnter(AbilityEffectContext ctx, DamageEffect effect)
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

                combatSystem.Attack(ctx.Caster, target, effect.Amount, ctx.CurrentTick, ctx.EffectIndex, matchSeed.Value, ctx.HitContext);
            }
        }
    }
}
