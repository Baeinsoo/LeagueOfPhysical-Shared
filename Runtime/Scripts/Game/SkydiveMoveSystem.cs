using GameFramework;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// Skydive의 <b>속도</b>를 정한다. 자세가 목표 하강·수평 속도를 정하고, 실제 속도는 그 목표로
    /// 수렴한다 — 자세를 바꿔도 속도가 한 틱에 튀지 않아 남을 예측하는 쪽의 오차가 완만해진다.
    ///
    /// 발판에 서 있는 동안은 자세를 보지 않고 <b>다른 게임과 같은 걷기 커널</b>
    /// (<see cref="MovementMotor.CalcVelocity"/>)을 그대로 부른다 — 상수를 베끼면 한쪽만
    /// 바뀌어 조용히 갈라지지만, 같은 함수를 부르면 걷는 느낌이 구조적으로 같아진다.
    /// 그 커널이 제동과 함께 <b>바라볼 방향</b>도 내주므로 걸을 때 몸이 이동 방향으로 돈다.
    /// 자세는 떨어지는 몸의 개념이라, 서 있는 몸에 그대로 쓰면 얼음 위를 게걸음하듯 보인다.
    ///
    /// 위치는 여기서 정하지 않는다: 맵에 부딪히면 벽까지만 가야 하는데 그 판정은 충돌 쿼리가
    /// 필요하고, 그 쿼리를 든 쪽이 <see cref="SkydiveWorld"/>다(<see cref="FlappyMoveSystem"/>과 같은 짝).
    /// </summary>
    public class SkydiveMoveSystem
    {
        public void Tick(GameFramework.World.Entity entity, float deltaTime, in SkydiveConfig config)
        {
            var velocity = entity.Get<GameFramework.World.Velocity>();
            var posture = entity.Get<Posture>();
            if (velocity == null || posture == null)
            {
                return;
            }

            // 발 딛고 있나는 지난 틱 이동 커널이 적어 둔 값이다(SkydiveWorld가 이동 뒤에 갱신).
            // 한 틱 늦지만 20ms라, 착지 다음 틱부터 걷기 값이 붙는 정도로만 드러난다.
            bool grounded = entity.Get<GameFramework.World.GroundState>()?.IsGrounded ?? false;

            float axis = posture.Axis < 0f ? 0f : (posture.Axis > 1f ? 1f : posture.Axis);

            // 패러세일은 자세 축과 무관한 도구라 축을 덮어쓴다.
            float targetFall = posture.Gliding
                ? config.GlideFallSpeed
                : Lerp(config.SpreadFallSpeed, config.DiveFallSpeed, axis);

            var linear = velocity.Linear;
            var command = entity.Get<InputBuffer>()?.Current;
            float inputX = command == null ? 0f : command.Horizontal;
            float inputZ = command == null ? 0f : command.Vertical;

            // 세로 — 목표 하강 속도로 수렴한다(중력을 직접 적분하지 않는다).
            linear.Y = Approach(linear.Y, -targetFall, config.FallApproach * deltaTime);

            // 점프는 지금까지의 세로 속도를 지우고 새로 준다 — 그래야 누를 때마다 같은 높이로 뜬다.
            // 위 수렴 다음에 와야 한다: 앞에 두면 누른 틱의 하강분만큼 손해를 봐 높이가 흔들린다.
            // 올라가는 동안 그 수렴이 곧 중력 역할이라, 도달 높이는 JumpPower²/(2×FallApproach)다.
            if (grounded && command != null && command.Jump)
            {
                linear.Y = config.JumpPower;
            }

            if (grounded)
            {
                WalkOnGround(entity, ref linear, inputX, inputZ, deltaTime, config);
            }
            else
            {
                Glide(ref linear, posture, axis, inputX, inputZ, deltaTime, config);
            }

            velocity.Linear = linear;
        }

        // 다른 게임과 같은 걷기 커널을 그대로 부른다 — 제동도 회전도 거기서 나온다.
        private static void WalkOnGround(GameFramework.World.Entity entity,
            ref System.Numerics.Vector3 linear, float inputX, float inputZ,
            float deltaTime, in SkydiveConfig config)
        {
            var result = MovementMotor.CalcVelocity(new MovementInput(
                linear.ToUnity(), inputX, inputZ,
                config.GroundMoveSpeed, config.GroundAccel, deltaTime));

            // 커널은 세로 속도를 그대로 돌려주므로 위에서 정한 점프·하강이 안 지워진다.
            linear = result.velocity.ToNumerics();

            // 입력이 있을 때만 방향이 나온다 — 손을 떼면 마지막으로 보던 쪽을 유지한다.
            if (result.hasRotation)
            {
                var transform = entity.Get<GameFramework.World.Transform>();
                if (transform != null)
                {
                    transform.Rotation = Quaternion.Euler(result.rotation).ToNumerics();
                }
            }
        }

        // 공중 — 자세가 목표 속도와 선회력을 정한다. 몸은 돌리지 않는다(기울기가 그 위에 얹힌다).
        private static void Glide(ref System.Numerics.Vector3 linear, Posture posture, float axis,
            float inputX, float inputZ, float deltaTime, in SkydiveConfig config)
        {
            float maxSide = posture.Gliding
                ? config.GlideMoveSpeed
                : Lerp(config.SpreadMoveSpeed, config.DiveMoveSpeed, axis);
            float turnAccel = posture.Gliding
                ? config.GlideTurnAccel
                : Lerp(config.SpreadTurnAccel, config.DiveTurnAccel, axis);

            float inputLen = (float)System.Math.Sqrt(inputX * inputX + inputZ * inputZ);
            if (inputLen > 1f)
            {
                inputX /= inputLen;
                inputZ /= inputLen;
            }

            // 입력 방향 × 최고 속도가 목표. 입력이 없으면 목표가 0이라 저절로 감속한다.
            linear.X = Approach(linear.X, inputX * maxSide, turnAccel * deltaTime);
            linear.Z = Approach(linear.Z, inputZ * maxSide, turnAccel * deltaTime);
        }

        private static float Lerp(float a, float b, float t) => a + (b - a) * t;

        // 현재값을 목표로 step만큼 당긴다. 넘어가면 목표에 딱 맞춘다(진동 방지).
        private static float Approach(float current, float target, float step)
        {
            float diff = target - current;
            if (diff > step) return current + step;
            if (diff < -step) return current - step;
            return target;
        }
    }
}
