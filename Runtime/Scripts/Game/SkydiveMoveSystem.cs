using GameFramework;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// Skydive의 <b>속도</b>를 정한다. 젤다 <i>왕국의 눈물</i>처럼 <b>걷기 / 점프 / 스카이다이빙 /
    /// 패러세일</b> 네 상태로 나눠 다룬다(<see cref="SkydiveMotionState"/>) — 매 틱 상태를 먼저
    /// 정하고 세로·좌우가 그 하나를 따른다.
    ///
    /// <list type="bullet">
    /// <item>걷기: 다른 게임과 같은 커널(<see cref="MovementMotor.CalcVelocity"/>)을
    /// 그대로 부른다. 상수를 베끼면 한쪽만 바뀌어 조용히 갈라지지만, 같은 함수를 부르면 걷는
    /// 느낌이 구조적으로 같아진다. 그 커널이 <b>바라볼 방향</b>도 내주므로 몸이 이동 방향으로 돈다.</item>
    /// <item>점프: 좌우 입력을 받지 않는다 — 이륙할 때의 수평 속도가 그대로 궤적이 된다.</item>
    /// <item>스카이다이빙: 자세 축(대자~다이브)이 종단속도와 좌우 조작을 정한다.</item>
    /// <item>패러세일: 천천히 내려오며 좌우가 가장 잘 든다.</item>
    /// </list>
    ///
    /// 세로는 어느 상태든 <b>일정 가속 + 종단속도 상한</b>이다 — 젤다 낙하 실측과 같은 모양이다.
    ///
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

            var linear = velocity.Linear;
            var command = entity.Get<InputBuffer>()?.Current;
            float inputX = command == null ? 0f : command.Horizontal;
            float inputZ = command == null ? 0f : command.Vertical;

            // 착지하면 점프가 끝난다. 여기서 지워야 다음 점프가 깨끗하게 시작한다.
            var jumpState = entity.Get<JumpState>();
            if (grounded && jumpState != null)
            {
                jumpState.IsJumping = false;
            }

            // 이번 틱을 어떤 상태로 굴릴지 먼저 정한다 — 아래 세로·좌우가 모두 이 하나를 따른다.
            var state = SkydiveMotion.Resolve(grounded, jumpState != null && jumpState.IsJumping, posture.Gliding);

            // 세로 — 상태가 정한 종단속도로 수렴한다(중력을 직접 적분하지 않는다).
            // 걷기 상태에서는 자세를 보지 않는다: 발 딛고 선 몸에 다이브 종단속도를 물리면,
            // 슬라이더를 쥔 채 걷다가 발판을 벗어나는 순간 하강이 튄다.
            float targetFall;
            if (state == SkydiveMotionState.Walking || state == SkydiveMotionState.Jumping)
            {
                targetFall = config.SpreadFallSpeed;
            }
            else if (state == SkydiveMotionState.Gliding)
            {
                targetFall = config.GlideFallSpeed;
            }
            else
            {
                float axis = posture.Axis < 0f ? 0f : (posture.Axis > 1f ? 1f : posture.Axis);
                targetFall = Lerp(config.SpreadFallSpeed, config.DiveFallSpeed, axis);
            }
            linear.Y = Approach(linear.Y, -targetFall, config.FallApproach * deltaTime);

            // 점프는 지금까지의 세로 속도를 지우고 새로 준다 — 그래야 누를 때마다 같은 높이로 뜬다.
            // 위 수렴 다음에 와야 한다: 앞에 두면 누른 틱의 하강분만큼 손해를 봐 높이가 흔들린다.
            // 올라가는 동안 그 수렴이 곧 중력 역할이라, 도달 높이는 JumpPower²/(2×FallApproach)다.
            if (grounded && command != null && command.Jump)
            {
                linear.Y = config.JumpPower;
                if (jumpState != null)
                {
                    // 이 틱은 아직 걷기로 굴러 좌우가 먹는다 — 뛰는 순간의 수평 속도가 곧 궤적이다.
                    // 잠금은 다음 틱부터다.
                    jumpState.IsJumping = true;
                }
            }

            // 좌우 — 상태마다 규칙이 다르다.
            //  걷기: 다른 게임과 같은 커널로(회전까지)
            //  점프: 아무것도 안 한다 — 이륙할 때의 수평 속도가 그대로 남아 궤적이 된다
            //  낙하·패러세일: 상태별 목표 속도로 수렴
            if (state == SkydiveMotionState.Walking)
            {
                Walk(entity, ref linear, inputX, inputZ, deltaTime, config);
            }
            else if (state != SkydiveMotionState.Jumping)
            {
                Drift(ref linear, posture, state, inputX, inputZ, deltaTime, config);
            }

            velocity.Linear = linear;
        }

        // 다른 게임과 같은 걷기 커널을 그대로 부른다 — 제동도 회전도 거기서 나온다.
        private static void Walk(GameFramework.World.Entity entity,
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

        // 공중 — 상태가 목표 속도와 선회력을 정한다. 몸은 돌리지 않는다(자세 기울기가 그 위에 얹힌다).
        private static void Drift(ref System.Numerics.Vector3 linear, Posture posture,
            SkydiveMotionState state, float inputX, float inputZ, float deltaTime, in SkydiveConfig config)
        {
            float maxSide;
            float turnAccel;
            if (state == SkydiveMotionState.Gliding)
            {
                maxSide = config.GlideMoveSpeed;
                turnAccel = config.GlideTurnAccel;
            }
            else
            {
                float axis = posture.Axis < 0f ? 0f : (posture.Axis > 1f ? 1f : posture.Axis);
                maxSide = Lerp(config.SpreadMoveSpeed, config.DiveMoveSpeed, axis);
                turnAccel = Lerp(config.SpreadTurnAccel, config.DiveTurnAccel, axis);
            }

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
