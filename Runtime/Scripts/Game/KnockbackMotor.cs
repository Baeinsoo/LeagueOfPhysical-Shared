using UnityEngine;

namespace LOP
{
    /// <summary>넉백 생존전의 걷기: 속도가 목표 속도(입력 방향 × 최고 속도)로 부드럽게 다가간다.</summary>
    public static class KnockbackMotor
    {
        public static Vector2 Step(Vector2 velocity, Vector2 input, float moveSpeed, float gain, float dt)
        {
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }
            var target = input * moveSpeed;
            return velocity + (target - velocity) * Mathf.Min(1f, gain * dt);
        }
    }
}
