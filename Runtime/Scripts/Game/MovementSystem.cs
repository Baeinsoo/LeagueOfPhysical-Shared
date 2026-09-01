using GameFramework;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// LOP 캐릭터의 이동. 스탯(MoveSpeed/JumpPower)·어빌리티(대시·이동배율·점프 봉인)·외력(넉백)을
    /// 읽어 <see cref="MovementMotor.CalcVelocity"/>에 넣어 주고, 그 결과를 World에 쓴다.
    /// 걷는 느낌 자체는 그 커널에 있다 — 다른 게임도 같은 커널을 부르므로 여기 상수를 바꾸는 게
    /// 아니라 커널에 넘기는 값을 바꾼다.
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
                    var result = MovementMotor.CalcVelocity(new MovementInput(
                        velocity, input.Horizontal, input.Vertical, speed, MaxAcceleration, deltaTime));
                    baseHorizontal = new Vector3(result.velocity.x, 0f, result.velocity.z);
                    if (input.Jump && !AbilitySystem.IsJumpBlocked(entity, currentTick))
                    {
                        velocity.y = statsSystem.GetValue(stats, (int)GameFramework.World.EntityStatType.JumpPower);
                    }
                    if (result.hasRotation)
                    {
                        entity.Get<GameFramework.World.Transform>().Rotation = Quaternion.Euler(result.rotation).ToNumerics();
                    }
                }
            }

            // 공격 등 진행 중 어빌리티의 현재 페이즈 이동배율을 수평속도에 곱한다(플레이어=모터 결과, AI=잔류속도).
            // 넉백 folding 전이라 외력은 안 깎임. 회전은 위에서 이미 세팅돼 배율 무관.
            baseHorizontal *= AbilitySystem.GetMovementMultiplier(entity, currentTick);

            // 외부 기여(넉백 등) 합성 — 입력 유무 무관, 플레이어·AI 공통. 만료 프루닝.
            var contributions = entity.Get<MotionContributions>();
            motionContributionSystem.Prune(contributions, currentTick);
            Vector3 finalHorizontal = motionContributionSystem
                .Resolve(baseHorizontal.ToNumerics(), contributions, currentTick).ToUnity();

            velocity.x = finalHorizontal.x;
            velocity.z = finalHorizontal.z;
            worldVelocity.Linear = velocity.ToNumerics();
        }
    }
}
