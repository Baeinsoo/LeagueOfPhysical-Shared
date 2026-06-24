using UnityEngine;

namespace LOP
{
    /// <summary>이동 커널 입력 — primitives only (엔진 객체·MasterData 비참조).</summary>
    public readonly struct MovementInput
    {
        public readonly Vector3 currentVelocity;
        public readonly float horizontal;
        public readonly float vertical;
        public readonly float speed;

        public MovementInput(Vector3 currentVelocity, float horizontal, float vertical, float speed)
        {
            this.currentVelocity = currentVelocity;
            this.horizontal = horizontal;
            this.vertical = vertical;
            this.speed = speed;
        }
    }

    /// <summary>이동 커널 출력 — host가 적용. hasMove == false면 velocity/rotation 무시.</summary>
    public readonly struct MovementResult
    {
        public readonly bool hasMove;
        public readonly Vector3 velocity;
        public readonly Vector3 rotation;

        public MovementResult(bool hasMove, Vector3 velocity, Vector3 rotation)
        {
            this.hasMove = hasMove;
            this.velocity = velocity;
            this.rotation = rotation;
        }
    }

    /// <summary>
    /// 이동 결정론 커널 — 클·서 공유 1벌. 순수(상태 없음·부수효과 없음).
    /// 입력 → 이동 속도·회전 계산. 점프(PhysX 임펄스)·적용·연출은 host 책임.
    /// </summary>
    public static class MovementSystem
    {
        public static MovementResult ProcessMovement(in MovementInput input)
        {
            Vector3 direction = new Vector3(input.horizontal, 0, input.vertical).normalized;

            if (direction.sqrMagnitude > 0)
            {
                var move = direction * input.speed;
                var velocity = new Vector3(move.x, input.currentVelocity.y, move.z);
                float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                var rotation = new Vector3(0, angle, 0);
                return new MovementResult(true, velocity, rotation);
            }

            return new MovementResult(false, input.currentVelocity, Vector3.zero);
        }
    }
}
