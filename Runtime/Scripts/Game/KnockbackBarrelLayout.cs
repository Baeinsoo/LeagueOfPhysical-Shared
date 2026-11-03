using UnityEngine;

namespace LOP
{
    /// <summary>드럼통 설정 한 벌: 몇 개를, 얼마나 먼 고리 위에, 어떤 물리 값으로 굴릴지. 0개면 드럼통 없는 맵이다.</summary>
    public sealed class KnockbackBarrelLayout
    {
        public int Count { get; }
        public float Radius { get; }
        public float RingRadius { get; }
        /// <summary>구르는 드럼통이 초당 줄어드는 속도(m/s²).</summary>
        public float Friction { get; }
        public float Restitution { get; }
        /// <summary>탄 한 발이 드럼통에 더하는 속도(m/s).</summary>
        public float ShotImpulse { get; }
        /// <summary>드럼통이 몸을 칠 때 넉백 세기에 곱하는 배율.</summary>
        public float HitScale { get; }

        //  설정을 안 준 맵(옛 호출부 포함)이 "드럼통 없음"으로 읽도록 하는 기본값. 방어 검사를 거치지 않는다.
        public static readonly KnockbackBarrelLayout None = new KnockbackBarrelLayout();

        private KnockbackBarrelLayout() { }

        public KnockbackBarrelLayout(int count, float radius, float ringRadius, float friction,
                                     float restitution, float shotImpulse, float hitScale)
        {
            Count = Mathf.Max(0, count);
            if (Count == 0) return;
            //  0 이하면 드럼통이 안 보이거나 안 멈추므로 기본값으로 쓴다. 클·서가 같은 생성자를 지나 같은 값이 된다.
            Radius = PositiveOrDefault(radius, 0.55f, "barrel_radius");
            RingRadius = PositiveOrDefault(ringRadius, 9f, "barrel_ring_radius");
            Friction = PositiveOrDefault(friction, 2.5f, "barrel_friction");
            Restitution = NonNegativeOrDefault(restitution, 0.6f, "barrel_restitution");
            ShotImpulse = PositiveOrDefault(shotImpulse, 6f, "barrel_shot_impulse");
            HitScale = NonNegativeOrDefault(hitScale, 1f, "barrel_hit_scale");
        }

        //  0이 의미 있는 값(완전 비탄성, 몸을 안 치는 드럼통)이라 음수만 거른다.
        private static float NonNegativeOrDefault(float value, float fallback, string column)
        {
            if (value >= 0f) return value;
            Debug.LogError($"TbKnockbackConfig {column}는 0 이상이어야 한다: {value} - {fallback}로 처리한다");
            return fallback;
        }

        private static float PositiveOrDefault(float value, float fallback, string column)
        {
            if (value > 0f) return value;
            Debug.LogError($"TbKnockbackConfig {column}는 0보다 커야 한다: {value} - {fallback}로 처리한다");
            return fallback;
        }
    }
}
