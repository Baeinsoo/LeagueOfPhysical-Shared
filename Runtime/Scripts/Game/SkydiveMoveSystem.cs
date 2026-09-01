namespace LOP
{
    /// <summary>
    /// Skydive의 <b>속도</b>를 정한다. 자세가 목표 하강·수평 속도를 정하고, 실제 속도는 그 목표로
    /// 수렴한다 — 자세를 바꿔도 속도가 한 틱에 튀지 않아 남을 예측하는 쪽의 오차가 완만해진다.
    ///
    /// 발판에 서 있는 동안은 자세를 보지 않고 <b>걷기</b> 값을 쓴다(빠른 제동 + 점프). 자세는
    /// 떨어지는 몸의 개념이라, 서 있는 몸에 그대로 쓰면 얼음 위처럼 미끄러진다.
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

            // 땅에서는 자세와 무관하게 걷기 값을 쓴다. 공중 값(가속 6~22)을 그대로 쓰면 손을 떼고도
            // 몇 미터를 미끄러져 "얼음 위" 같아진다 — 자세는 떨어지는 몸을 다루는 개념이지
            // 서 있는 몸을 다루는 개념이 아니다.
            float maxSide;
            float sideAccel;
            if (grounded)
            {
                maxSide = config.GroundMoveSpeed;
                sideAccel = config.GroundAccel;
            }
            else
            {
                maxSide = posture.Gliding
                    ? config.GlideMoveSpeed
                    : Lerp(config.SpreadMoveSpeed, config.DiveMoveSpeed, axis);
                sideAccel = posture.Gliding
                    ? config.GlideTurnAccel
                    : Lerp(config.SpreadTurnAccel, config.DiveTurnAccel, axis);
            }

            var linear = velocity.Linear;
            var command = entity.Get<InputBuffer>()?.Current;

            // 세로 — 목표 하강 속도로 수렴한다(중력을 직접 적분하지 않는다).
            linear.Y = Approach(linear.Y, -targetFall, config.FallApproach * deltaTime);

            // 점프는 지금까지의 세로 속도를 지우고 새로 준다 — 그래야 누를 때마다 같은 높이로 뜬다.
            // 위 수렴 다음에 와야 한다: 앞에 두면 누른 틱의 하강분만큼 손해를 봐 높이가 흔들린다.
            // 올라가는 동안 그 수렴이 곧 중력 역할이라, 도달 높이는 JumpPower²/(2×FallApproach)다.
            if (grounded && command != null && command.Jump)
            {
                linear.Y = config.JumpPower;
            }

            // 가로 — 입력 방향 × 최고 속도가 목표. 입력이 없으면 목표가 0이라 저절로 감속한다.
            float inputX = command == null ? 0f : command.Horizontal;
            float inputZ = command == null ? 0f : command.Vertical;
            float inputLen = (float)System.Math.Sqrt(inputX * inputX + inputZ * inputZ);
            if (inputLen > 1f)
            {
                inputX /= inputLen;
                inputZ /= inputLen;
            }
            linear.X = Approach(linear.X, inputX * maxSide, sideAccel * deltaTime);
            linear.Z = Approach(linear.Z, inputZ * maxSide, sideAccel * deltaTime);

            velocity.Linear = linear;
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
