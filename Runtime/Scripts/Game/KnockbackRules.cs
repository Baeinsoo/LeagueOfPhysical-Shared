using UnityEngine;

namespace LOP
{
    /// <summary>탄에 맞았을 때의 넉백 규칙. 피해%가 쌓일수록 더 세게, 더 오래 날아간다.</summary>
    public static class KnockbackRules
    {
        public static void ApplyHit(KnockbackBody body, GameFramework.World.Velocity velocity, Vector2 shotDirection,
                                    long tick, KnockbackConfig config, float dt)
        {
            body.DamagePercent += config.DamagePerHit;
            float strength = config.KnockbackBase + body.DamagePercent * config.KnockbackScale;

            var dir = shotDirection.sqrMagnitude > 0f ? shotDirection.normalized : Vector2.zero;
            var v = velocity.Linear;
            // 원래 달리던 속도도 조금 남겨, 달려가며 맞은 쪽이 자연스럽게 이어 밀린다.
            velocity.Linear = new System.Numerics.Vector3(
                dir.x * strength + v.X * config.KnockbackCarry,
                v.Y,
                dir.y * strength + v.Z * config.KnockbackCarry);

            body.KnockbackEndTick = StunEndTick(body, tick, config);
        }

        /// <summary>
        /// 위험도를 올리지 않고 속도와 날림만 넣는다(드럼통이 몸을 칠 때). 방향은 단위 벡터, 세기는 m/s.
        /// </summary>
        public static void ApplyPush(KnockbackBody body, GameFramework.World.Velocity velocity, Vector2 direction,
                                     float strength, long tick, KnockbackConfig config)
        {
            var dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.zero;
            var v = velocity.Linear;
            velocity.Linear = new System.Numerics.Vector3(
                dir.x * strength + v.X * config.KnockbackCarry,
                v.Y,
                dir.y * strength + v.Z * config.KnockbackCarry);
            body.KnockbackEndTick = StunEndTick(body, tick, config);
        }

        /// <summary>지금 위험도로 날리면 언제까지 날려지는지. 탄에 맞을 때와 몸에 부딪힐 때가 같은 식을 쓴다.</summary>
        public static long StunEndTick(KnockbackBody body, long tick, KnockbackConfig config)
            => tick + config.Ticks(config.StunBase + body.DamagePercent * config.StunScale);
    }
}
