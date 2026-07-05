using System.Numerics;

namespace LOP
{
    /// <summary>
    /// 이동 기여의 프루닝/해소(순수 static — 상태 없음, <see cref="MovementSystem.ProcessMovement"/> 커널과 같은 결).
    /// 합성 규칙(CMC/Mover 표준): 최고 우선순위 활성 Override가 base를 대체하고, 활성 Additive는 그 위에 가산.
    /// </summary>
    public static class MotionContributionSystem
    {
        public static void Prune(MotionContributions contributions, long currentTick)
        {
            contributions?.Items.RemoveAll(c => currentTick >= c.EndTick);
        }

        public static Vector3 Resolve(Vector3 baseHorizontal, MotionContributions contributions, long currentTick)
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
                        sum += c.Horizontal;
                    }
                }
            }
            return sum;
        }
    }
}
