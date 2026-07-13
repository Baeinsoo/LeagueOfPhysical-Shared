using System.Numerics;

namespace LOP
{
    /// <summary>
    /// 이동 기여의 프루닝/해소(무상태 시스템 — DI 인스턴스, 다른 *System과 동일 관용). 상태는 컴포넌트에 둔다.
    /// 합성 규칙(CMC/Mover 표준): 최고 우선순위 활성 Override가 base를 대체하고, 활성 Additive는 그 위에 가산.
    /// </summary>
    public class MotionContributionSystem
    {
        public void Prune(MotionContributions contributions, long currentTick)
        {
            contributions?.Items.RemoveAll(c => currentTick >= c.EndTick);
        }

        public Vector3 Resolve(Vector3 baseHorizontal, MotionContributions contributions, long currentTick)
        {
            Vector3 root = baseHorizontal;

            if (contributions != null)
            {
                bool hasOverride = false;
                int bestPriority = int.MinValue;
                Vector3 overrideValue = Vector3.Zero;
                foreach (var c in contributions.Items)
                {
                    if (c.Mode == MotionContributionMode.Override && c.IsActiveAt(currentTick) &&
                        (!hasOverride || c.Priority > bestPriority))
                    {
                        hasOverride = true;
                        bestPriority = c.Priority;
                        overrideValue = c.Horizontal;
                    }
                }
                if (hasOverride)
                {
                    root = overrideValue;
                }
            }

            Vector3 sum = root;
            if (contributions != null)
            {
                foreach (var c in contributions.Items)
                {
                    if (c.Mode == MotionContributionMode.Additive && c.IsActiveAt(currentTick))
                    {
                        float factor = System.MathF.Pow(c.DecayPerTick, currentTick - c.StartTick);
                        sum += c.Horizontal * factor;
                    }
                }
            }
            return sum;
        }

        /// <summary>
        /// 엔티티의 현재 수평 속도를 base로 외부 기여(넉백 등)를 합성해 World.Velocity에 되쓴다(y는 보존).
        /// 입력으로 이동을 계산하지 않는 엔티티(AI 등)용 — 플레이어는 <see cref="MovementSystem.Tick"/>이
        /// 입력 기반 base로 같은 <see cref="Resolve"/>를 태운다. 만료 기여는 프루닝.
        /// </summary>
        public void ApplyToVelocity(GameFramework.World.Entity entity, long currentTick)
        {
            var worldVelocity = entity.Get<GameFramework.World.Velocity>();
            if (worldVelocity == null)
            {
                return;
            }
            var contributions = entity.Get<MotionContributions>();
            Prune(contributions, currentTick);
            Vector3 v = worldVelocity.Linear;
            Vector3 final = Resolve(new Vector3(v.X, 0f, v.Z), contributions, currentTick);
            worldVelocity.Linear = new Vector3(final.X, v.Y, final.Z);
        }

        /// <summary>공격자→대상 방향으로 미는 Additive 넉백 기여 하나(순수 커널 — 서버 핸들러/테스트 공용). y는 무시.</summary>
        public static MotionContribution CreateRadialKnockback(
            Vector3 attackerPos, Vector3 targetPos, float strength, int durationTicks, float decayPerTick, long currentTick)
        {
            Vector3 away = new Vector3(targetPos.X - attackerPos.X, 0f, targetPos.Z - attackerPos.Z);
            Vector3 dir = away.LengthSquared() > 1e-8f ? Vector3.Normalize(away) : Vector3.Zero;
            return new MotionContribution(dir * strength, MotionContributionMode.Additive, 0,
                currentTick, currentTick + durationTicks, decayPerTick);
        }
    }
}
