using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 서버가 공개한 원 한 단계. 공개 -> 쉼 -> 축소 순으로 흐르고, 축소 구간에서만 from -> to로 줄어든다.
    /// "틱을 넣으면 반지름이 나오는" 식에 필요한 값만 담는다.
    /// </summary>
    public readonly struct ZoneStage
    {
        public readonly int index;
        public readonly long revealTick;
        public readonly long shrinkStartTick;
        public readonly long shrinkEndTick;
        public readonly Vector2 fromCenter;
        public readonly float fromRadius;
        public readonly Vector2 toCenter;
        public readonly float toRadius;
        public readonly float damagePerSecond;

        public ZoneStage(int index, long revealTick, long shrinkStartTick, long shrinkEndTick,
                         Vector2 fromCenter, float fromRadius, Vector2 toCenter, float toRadius,
                         float damagePerSecond)
        {
            this.index = index;
            this.revealTick = revealTick;
            this.shrinkStartTick = shrinkStartTick;
            this.shrinkEndTick = shrinkEndTick;
            this.fromCenter = fromCenter;
            this.fromRadius = fromRadius;
            this.toCenter = toCenter;
            this.toRadius = toRadius;
            this.damagePerSecond = damagePerSecond;
        }
    }

    /// <summary>
    /// 줄어드는 원의 모양 식. 클·서가 같은 값을 내야 해서 상태를 두지 않는다.
    /// 다음 원의 자리는 미리 계산하지 않고 서버가 공개할 때 알려 준다(미래가 새지 않게).
    /// </summary>
    public static class MountedArcheryZone
    {
        public static (Vector2 center, float radius) CircleAt(
            IReadOnlyList<ZoneStage> stages, long tick, Vector2 initialCenter, float initialRadius)
        {
            if (!TryLatest(stages, tick, out var s)) { return (initialCenter, initialRadius); }
            if (tick <= s.shrinkStartTick) { return (s.fromCenter, s.fromRadius); }
            if (tick >= s.shrinkEndTick) { return (s.toCenter, s.toRadius); }

            float t = (float)(tick - s.shrinkStartTick) / (s.shrinkEndTick - s.shrinkStartTick);
            return (Vector2.Lerp(s.fromCenter, s.toCenter, t), Mathf.Lerp(s.fromRadius, s.toRadius, t));
        }

        /// <summary>
        /// 줍기 시간엔 0, 그 뒤엔 가장 최근에 공개된 단계의 값. 공개되는 순간부터 원 밖이면 아프다.
        /// 마지막 원이 다 줄어든 뒤엔 초마다 growth씩 더 아프다 — 끝까지 버티는 사람이 있어도 판이 끝나게 한다.
        /// </summary>
        public static float DamagePerSecondAt(IReadOnlyList<ZoneStage> stages, long tick, float growthPerSecond, float tickInterval)
        {
            if (!TryLatest(stages, tick, out var s)) { return 0f; }
            if (stages.Count == 0 || s.index != MaxIndex(stages) || tick <= s.shrinkEndTick) { return s.damagePerSecond; }
            return s.damagePerSecond + growthPerSecond * (tick - s.shrinkEndTick) * tickInterval;
        }

        static int MaxIndex(IReadOnlyList<ZoneStage> stages)
        {
            int max = int.MinValue;
            for (int i = 0; i < stages.Count; i++) { if (stages[i].index > max) { max = stages[i].index; } }
            return max;
        }

        /// <summary>반지름이 0 이하면 한가운데 한 점도 안전하지 않다.</summary>
        public static bool IsOutside(Vector2 position, (Vector2 center, float radius) circle)
            => circle.radius <= 0f || Vector2.Distance(position, circle.center) > circle.radius;

        /// <summary>
        /// 새 원이 지금 원 안에 통째로 들어가도록 중심을 고른다(중심 거리 &lt;= 반지름 차).
        /// 면적으로 고르게 퍼지도록 거리에 제곱근을 쓴다. u1은 거리, u2는 각도(0~1).
        /// </summary>
        public static Vector2 PickNextCenter(Vector2 center, float radius, float nextRadius, float u1, float u2)
        {
            float reach = Mathf.Max(0f, radius - nextRadius);
            float dist = reach * Mathf.Sqrt(Mathf.Clamp01(u1));
            float angle = Mathf.Clamp01(u2) * Mathf.PI * 2f;
            return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
        }

        static bool TryLatest(IReadOnlyList<ZoneStage> stages, long tick, out ZoneStage found)
        {
            found = default;
            if (stages == null) { return false; }
            bool any = false;
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i].revealTick > tick) { continue; }
                if (!any || stages[i].revealTick >= found.revealTick) { found = stages[i]; any = true; }
            }
            return any;
        }
    }
}
