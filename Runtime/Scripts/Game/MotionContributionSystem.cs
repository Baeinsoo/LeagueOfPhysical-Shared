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
