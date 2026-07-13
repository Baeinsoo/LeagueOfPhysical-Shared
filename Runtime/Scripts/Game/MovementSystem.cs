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
        private readonly MotionContributionSystem motionContributionSystem;

        public MovementSystem(GameFramework.World.StatsSystem statsSystem, MotionContributionSystem motionContributionSystem)
        {
            this.statsSystem = statsSystem;
            this.motionContributionSystem = motionContributionSystem;
        }

        /// <summary>
        /// PlayerInput(이번 틱 입력)을 읽어 이동을 적용한다 — World.Velocity/Transform에 쓴다.
        /// PlayerInput이 없는 엔티티(AI/원격/아이템)는 건드리지 않는다.
        /// </summary>
        public void Tick(GameFramework.World.Entity entity, long currentTick, float deltaTime)
        {
            var worldVelocity = entity.Get<GameFramework.World.Velocity>();
            if (worldVelocity == null)
            {
                return;   // 이동 없는 엔티티
            }
            Vector3 velocity = worldVelocity.Linear.ToUnity();   // Y 보존용
            Vector3 baseHorizontal = new Vector3(velocity.x, 0f, velocity.z);   // 기본 = 현재 수평(입력 없으면 유지)

            var input = entity.Get<InputBuffer>()?.Current;
            if (input != null)
            {
                if (AbilitySystem.TryGetActiveMotionEffect(entity, currentTick, out var motion))
                {
                    // 대시(파생 Override): 바라보는 방향으로 speed. 입력 무시(락) + 회전 미변경 + 점프 무시.
                    Vector3 forward = entity.Get<GameFramework.World.Transform>().Rotation.ToUnity() * Vector3.forward;
                    baseHorizontal = new Vector3(forward.x, 0f, forward.z).normalized * motion.Speed;
                }
                else
                {
                    var stats = entity.Get<GameFramework.World.Stats>();
                    float speed = statsSystem.GetValue(stats, (int)GameFramework.World.EntityStatType.MoveSpeed);
                    var result = ProcessMovement(new MovementInput(
                        velocity, input.Horizontal, input.Vertical, speed, MaxAcceleration, deltaTime));
                    baseHorizontal = new Vector3(result.velocity.x, 0f, result.velocity.z);
                    if (input.Jump)
                    {
                        velocity.y = statsSystem.GetValue(stats, (int)GameFramework.World.EntityStatType.JumpPower);
                    }
                    if (result.hasRotation)
                    {
                        entity.Get<GameFramework.World.Transform>().Rotation = Quaternion.Euler(result.rotation).ToNumerics();
                    }
                }
            }

            // 외부 기여(넉백 등) 합성 — 입력 유무 무관, 플레이어·AI 공통. 만료 프루닝.
            var contributions = entity.Get<MotionContributions>();
            motionContributionSystem.Prune(contributions, currentTick);
            Vector3 finalHorizontal = motionContributionSystem
                .Resolve(baseHorizontal.ToNumerics(), contributions, currentTick).ToUnity();

            velocity.x = finalHorizontal.x;
            velocity.z = finalHorizontal.z;
            worldVelocity.Linear = velocity.ToNumerics();
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
