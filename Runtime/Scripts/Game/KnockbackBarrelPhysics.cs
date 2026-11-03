using UnityEngine;

namespace LOP
{
    /// <summary>드럼통 굴림의 순수 식(평면). 서버 시스템이 이 식들을 틱 순서대로 부른다.</summary>
    public static class KnockbackBarrelPhysics
    {
        //  이보다 느리게 다가오는 접촉은 안 친다 — 거의 선 드럼통 곁을 지나가다 스친 걸로 날리지 않게.
        public const float MinClosingSpeed = 0.5f;

        /// <summary>마찰로 속도를 줄인다. 한 틱에 0을 넘어 반대로 가지 않는다.</summary>
        public static Vector2 ApplyFriction(Vector2 velocity, float friction, float dt)
        {
            float speed = velocity.magnitude;
            if (speed <= 0f) return Vector2.zero;
            float slowed = Mathf.Max(0f, speed - friction * dt);
            return velocity * (slowed / speed);
        }

        /// <summary>움직이지 않는 원(기둥)에 겹치면 밖으로 밀어내고, 다가오던 속도를 반발 계수만큼 되돌린다.</summary>
        public static void BounceOffFixedCircle(ref Vector2 position, ref Vector2 velocity,
                                                Vector2 center, float minDistance, float restitution)
        {
            Vector2 offset = position - center;
            float distance = offset.magnitude;
            if (distance >= minDistance) return;

            Vector2 normal = distance > 1e-6f ? offset / distance : Vector2.right;
            position = center + normal * minDistance;
            float approach = Vector2.Dot(velocity, normal);
            if (approach < 0f)
            {
                velocity -= (1f + restitution) * approach * normal;
            }
        }

        /// <summary>같은 무게 두 드럼통: 겹친 만큼 반씩 밀어내고, 다가오던 속도를 반발 계수로 주고받는다.</summary>
        public static void BouncePair(ref Vector2 positionA, ref Vector2 velocityA,
                                      ref Vector2 positionB, ref Vector2 velocityB,
                                      float minDistance, float restitution)
        {
            Vector2 offset = positionB - positionA;
            float distance = offset.magnitude;
            if (distance >= minDistance) return;

            Vector2 normal = distance > 1e-6f ? offset / distance : Vector2.right;
            float half = (minDistance - distance) * 0.5f;
            positionA -= normal * half;
            positionB += normal * half;

            float a = Vector2.Dot(velocityA, normal);
            float b = Vector2.Dot(velocityB, normal);
            if (a - b <= 0f) return;

            float mean = (a + b) * 0.5f;
            float spread = restitution * (a - b) * 0.5f;
            velocityA += normal * ((mean - spread) - a);
            velocityB += normal * ((mean + spread) - b);
        }

        /// <summary>
        /// 드럼통이 몸을 치는가. 겹치고 서로 다가올 때만 친다.
        /// 방향은 드럼→몸, 세기는 다가오는 상대 속도 × 배율(같은 질량 충돌에서 속도가 넘어가는 만큼).
        /// </summary>
        public static bool TryHitBody(Vector2 barrelPosition, Vector2 barrelVelocity,
                                      Vector2 bodyPosition, Vector2 bodyVelocity,
                                      float reach, float hitScale,
                                      out Vector2 direction, out float strength)
        {
            direction = Vector2.zero;
            strength = 0f;
            Vector2 offset = bodyPosition - barrelPosition;
            if (offset.sqrMagnitude > reach * reach) return false;

            direction = offset.sqrMagnitude > 1e-8f ? offset.normalized : (barrelVelocity.sqrMagnitude > 0f ? barrelVelocity.normalized : Vector2.right);
            float closing = Vector2.Dot(barrelVelocity - bodyVelocity, direction);
            if (closing <= MinClosingSpeed) return false;

            strength = closing * hitScale;
            return true;
        }
    }
}
