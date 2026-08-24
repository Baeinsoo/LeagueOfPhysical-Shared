using GameFramework;

namespace LOP
{
    /// <summary>
    /// <see cref="DamageEffect"/> 핸들러(클·서 공유). Active 진입 시 1회, 시전자 정면 부채꼴 안의 대상을 때린다.
    /// 판정 위치는 World.Transform(진실원본, System.Numerics) — 엔진 좌표 대신. 엔진 물리(범위 검색)는
    /// ICollisionQuery(클·서 공유)에 위임하고, 해소(데미지/크리/회피)는 LOPCombatSystem(공유)이 한다.
    /// 클라 등록·예측 소비는 A2.4 — 지금은 서버만 등록(데미지 서버권위).
    /// </summary>
    public class DamageEffectHandler : AbilityEffectHandler<DamageEffect>
    {
        // 옛 LOPOverlapQuery가 안에 박아 두던 값 — 포트에서 나오면서 부르는 쪽으로 옮겨왔다.
        private static readonly int CharacterLayerMask = UnityEngine.LayerMask.GetMask("Character");

        private readonly LOPCombatSystem combatSystem;
        private readonly GameFramework.Physics.ICollisionQuery collisionQuery;
        private readonly IMatchSeed matchSeed;
        private readonly GameFramework.World.EntityRegistry entityRegistry;

        public DamageEffectHandler(LOPCombatSystem combatSystem,
                                   GameFramework.Physics.ICollisionQuery collisionQuery,
                                   IMatchSeed matchSeed,
                                   GameFramework.World.EntityRegistry entityRegistry)
        {
            this.combatSystem = combatSystem;
            this.collisionQuery = collisionQuery;
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

            GameFramework.Physics.CollisionHit[] hits =
                collisionQuery.OverlapSphere(casterTransform.Position.ToUnity(), effect.Range, CharacterLayerMask);

            // 한 엔티티가 콜라이더를 여럿 가질 수 있어 같은 대상이 여러 번 나온다 — 합치지 않으면 두 번 맞는다.
            var alreadyHit = new System.Collections.Generic.HashSet<string>();
            foreach (GameFramework.Physics.CollisionHit hit in hits)
            {
                string id = hit.GetEntityId();
                if (id == null || id == ctx.Caster.Id || alreadyHit.Add(id) == false)
                {
                    continue;   // 엔티티 아님 / 자기제외 / 이미 맞음
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
