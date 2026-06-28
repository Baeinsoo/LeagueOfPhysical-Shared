using UnityEngine;

namespace LOP
{
    /// <summary>이동 계산에 넣는 입력값 묶음 (클라이언트·서버 공통).</summary>
    public readonly struct MovementInput
    {
        public readonly Vector3 currentVelocity;
        public readonly float horizontal;
        public readonly float vertical;
        public readonly float speed;            // 최대 이동 속도(목표)
        public readonly float maxAcceleration;  // 목표 속도로 따라붙는 빠르기(클수록 즉각 반응)
        public readonly float deltaTime;

        public MovementInput(Vector3 currentVelocity, float horizontal, float vertical, float speed,
                             float maxAcceleration, float deltaTime)
        {
            this.currentVelocity = currentVelocity;
            this.horizontal = horizontal;
            this.vertical = vertical;
            this.speed = speed;
            this.maxAcceleration = maxAcceleration;
            this.deltaTime = deltaTime;
        }
    }

    /// <summary>이동 계산 결과. velocity=새 속도, rotation=바라볼 방향(방향 입력이 있을 때만).</summary>
    public readonly struct MovementResult
    {
        public readonly Vector3 velocity;
        public readonly bool hasRotation;
        public readonly Vector3 rotation;

        public MovementResult(Vector3 velocity, bool hasRotation, Vector3 rotation)
        {
            this.velocity = velocity;
            this.hasRotation = hasRotation;
            this.rotation = rotation;
        }
    }

    /// <summary>
    /// 플레이어 입력으로 새 이동 속도를 계산한다 (클라이언트·서버가 똑같이 사용).
    /// 방향 입력이 있으면 지금 속도에서 목표 속도로 곧장 당겨서, 가던 방향의 관성이 남지 않게 한다
    /// (예: 오른쪽으로 가다 위를 누르면 오른쪽 속도가 사라지고 위로만 감 → 옆으로 안 미끄러짐).
    /// 방향 입력이 없으면 속도를 그대로 둔다 — 멈추는 건 Rigidbody의 drag(linearDamping)가 처리한다.
    /// </summary>
    public static class MovementSystem
    {
        public static MovementResult ProcessMovement(in MovementInput input)
        {
            // 좌우/앞뒤(수평) 속도만 다룬다. 위아래(y)는 중력·점프 몫이라 그대로 둔다.
            Vector3 horiz = new Vector3(input.currentVelocity.x, 0, input.currentVelocity.z);

            Vector3 dir = new Vector3(input.horizontal, 0, input.vertical);
            bool hasRotation = dir.sqrMagnitude > 0f;
            Vector3 newHoriz = horiz;  // 방향 입력이 없으면 속도 그대로 (멈춤은 drag가 처리)
            Vector3 rotation = Vector3.zero;
            if (hasRotation)
            {
                dir.Normalize();
                Vector3 desired = dir * input.speed;
                // 지금 속도에서 목표 속도로 정해진 양만큼 당긴다 (옆 관성이 남지 않음)
                newHoriz = Vector3.MoveTowards(horiz, desired, input.maxAcceleration * input.deltaTime);
                float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                rotation = new Vector3(0, angle, 0);
            }

            return new MovementResult(new Vector3(newHoriz.x, input.currentVelocity.y, newHoriz.z), hasRotation, rotation);
        }
    }
}
