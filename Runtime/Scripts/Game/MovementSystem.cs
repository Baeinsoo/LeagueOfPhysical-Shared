using GameFramework;
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
    /// 지금 속도에서 목표 속도로 정해진 양만큼 당긴다. 입력이 있으면 목표=입력 방향×moveSpeed,
    /// 없으면 목표=0(정지). 그래서 방향전환 시 옆 관성이 안 남고, 입력을 떼면 0으로 제동해 멈춘다.
    /// 수평(좌우/앞뒤)만 다루고 수직(y)은 중력·점프 몫(drag 미사용).
    /// </summary>
    public class MovementSystem
    {
        private const float MaxAcceleration = 100f;   // 목표 속도로 따라붙는 빠르기(클수록 즉각 반응 — 튜닝값)

        private readonly GameFramework.World.StatsSystem statsSystem;

        public MovementSystem(GameFramework.World.StatsSystem statsSystem)
        {
            this.statsSystem = statsSystem;
        }

        /// <summary>
        /// PlayerInput(이번 틱 입력)을 읽어 이동을 적용한다 — World.Velocity/Transform에 쓴다.
        /// PlayerInput이 없는 엔티티(AI/원격/아이템)는 건드리지 않는다.
        /// </summary>
        public void Tick(GameFramework.World.Entity entity, long currentTick, float deltaTime)
        {
            var buffer = entity.Get<InputBuffer>();
            if (buffer == null)
            {
                return;   // 입력 비조종(AI/원격/아이템) — 버퍼 없음
            }

            var input = buffer.Current;
            if (input == null)
            {
                return;   // 이번 틱 확정된 커맨드 없음
            }

            // 대시 같은 이동 어빌리티가 Active면 입력 이동을 무시한다(대시가 방향·속도를 주도).
            if (AbilitySystem.HasActiveMotionEffect(entity))
            {
                return;
            }

            var stats = entity.Get<GameFramework.World.Stats>();
            float speed = statsSystem.GetValue(stats, (int)GameFramework.World.EntityStatType.MoveSpeed);

            var worldVelocity = entity.Get<GameFramework.World.Velocity>();
            Vector3 velocity = worldVelocity.Linear.ToUnity();

            var result = ProcessMovement(new MovementInput(
                velocity, input.Horizontal, input.Vertical, speed, MaxAcceleration, deltaTime));

            // 계산된 새 속도를 반영한다(좌우/앞뒤만; Y는 중력에 맡겨 보존). 점프면 Y를 점프 속도로 세팅.
            velocity.x = result.velocity.x;
            velocity.z = result.velocity.z;
            if (input.Jump)
            {
                velocity.y = statsSystem.GetValue(stats, (int)GameFramework.World.EntityStatType.JumpPower);
            }
            worldVelocity.Linear = velocity.ToNumerics();

            if (result.hasRotation)
            {
                entity.Get<GameFramework.World.Transform>().Rotation =
                    Quaternion.Euler(result.rotation).ToNumerics();
            }
        }

        public static MovementResult ProcessMovement(in MovementInput input)
        {
            // 좌우/앞뒤(수평) 속도만 다룬다. 위아래(y)는 중력·점프 몫이라 그대로 둔다.
            Vector3 horiz = new Vector3(input.currentVelocity.x, 0, input.currentVelocity.z);

            Vector3 dir = new Vector3(input.horizontal, 0, input.vertical);
            bool hasRotation = dir.sqrMagnitude > 0f;
            Vector3 desired = Vector3.zero;  // 입력이 없으면 목표 0 → 0으로 제동(정지)
            Vector3 rotation = Vector3.zero;
            if (hasRotation)
            {
                dir.Normalize();
                desired = dir * input.speed;
                float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                rotation = new Vector3(0, angle, 0);
            }

            // 지금 속도에서 목표로 정해진 양만큼 당긴다(입력 방향 속도로, 없으면 0으로). 옆 관성이 안 남음.
            Vector3 newHoriz = Vector3.MoveTowards(horiz, desired, input.maxAcceleration * input.deltaTime);

            return new MovementResult(new Vector3(newHoriz.x, input.currentVelocity.y, newHoriz.z), hasRotation, rotation);
        }
    }
}
