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

            var linear = velocity.Linear;
            var command = entity.Get<InputBuffer>()?.Current;
            float inputX = command == null ? 0f : command.Horizontal;
            float inputZ = command == null ? 0f : command.Vertical;

            // 상태 전이는 월드가 이미 밀어 놨다(발밑 여유를 재야 해서 쿼리가 필요하다). 여기선 읽기만.
            var state = entity.Get<MotionState>()?.Value ?? SkydiveMotionState.Falling;

            float targetFall;
            if (state != SkydiveMotionState.Skydiving)
            {
                targetFall = config.SpreadFallSpeed;   // 자세가 없으면 그냥 떨어진다
            }
            else if (posture.Gliding)
            {
                targetFall = config.GlideFallSpeed;
            }
            else
            {
                float axis = posture.Axis < 0f ? 0f : (posture.Axis > 1f ? 1f : posture.Axis);
                targetFall = Lerp(config.SpreadFallSpeed, config.DiveFallSpeed, axis);
            }
            // 빨라질 때는 중력(FallApproach), 느려질 때는 훨씬 큰 감속(FallBrake)을 쓴다.
            // 대칭으로 두면 패러세일을 펴도 속도가 중력과 같은 비율로만 줄어 낙하산이 아니게 된다
            // — 60에서 6까지 1.9초가 걸린다. 공기 저항은 면적이 커지면 급격히 커지므로
            // 커지는 쪽과 줄어드는 쪽이 원래 대칭이 아니다.
            float fallStep = linear.Y < -targetFall ? config.FallBrake : config.FallApproach;
            linear.Y = Approach(linear.Y, -targetFall, fallStep * deltaTime);

            // 점프는 지금까지의 세로 속도를 지우고 새로 준다 — 그래야 누를 때마다 같은 높이로 뜬다.
            // 위 수렴 다음에 와야 한다: 앞에 두면 누른 틱의 하강분만큼 손해를 봐 높이가 흔들린다.
            // 올라가는 동안 그 수렴이 곧 중력 역할이라, 도달 높이는 JumpPower²/(2×FallApproach)다.
            if (state == SkydiveMotionState.Walking && command != null && command.Jump)
            {
                linear.Y = config.JumpPower;
            }

            // 좌우는 화이트리스트다 — 여기 적힌 상태에서만 입력이 먹는다. "나머지 전부 허용"으로
            // 두면 상태를 새로 만들 때 조용히 이동이 붙는다(점프가 정확히 그랬다).
            //   걷기   : 다른 게임과 같은 커널로(회전까지)
            //   활공   : 자세별 목표 속도로 수렴
            //   낙하   : 없음 — 이륙할 때의 수평 속도가 그대로 남아 궤적이 된다
            if (state == SkydiveMotionState.Walking)
            {
                Walk(entity, ref linear, inputX, inputZ, deltaTime, config);
            }
            else if (state == SkydiveMotionState.Skydiving)
            {
                Drift(ref linear, posture, inputX, inputZ, deltaTime, config);
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
            float inputX, float inputZ, float deltaTime, in SkydiveConfig config)
        {
            float maxSide;
            float turnAccel;
            if (posture.Gliding)
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
