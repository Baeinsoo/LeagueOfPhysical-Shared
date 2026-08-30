namespace LOP
{
    /// <summary>
    /// Skydive의 이동. 자세가 <b>목표</b> 하강·수평 속도를 정하고, 실제 속도는 그 목표로 수렴한다 —
    /// 자세를 바꿔도 속도가 한 틱에 튀지 않아 남을 예측하는 쪽의 오차가 완만해진다.
    /// 지형 충돌은 슬라이스 3이 얹는다(지금은 임시 바닥만).
    /// </summary>
    public class SkydiveMoveSystem
    {
        public void Tick(GameFramework.World.Entity entity, float deltaTime, in SkydiveConfig config)
        {
            var velocity = entity.Get<GameFramework.World.Velocity>();
            var transform = entity.Get<GameFramework.World.Transform>();
            var posture = entity.Get<Posture>();
            if (velocity == null || transform == null || posture == null)
            {
                return;
            }

            float axis = posture.Axis < 0f ? 0f : (posture.Axis > 1f ? 1f : posture.Axis);

            // 패러세일은 자세 축과 무관한 도구라 축을 덮어쓴다.
            float targetFall = posture.Gliding
                ? config.GlideFallSpeed
                : Lerp(config.SpreadFallSpeed, config.DiveFallSpeed, axis);
            float maxSide = posture.Gliding
                ? config.GlideMoveSpeed
                : Lerp(config.SpreadMoveSpeed, config.DiveMoveSpeed, axis);
            float turnAccel = posture.Gliding
                ? config.GlideTurnAccel
                : Lerp(config.SpreadTurnAccel, config.DiveTurnAccel, axis);

            var linear = velocity.Linear;

            // 세로 — 목표 하강 속도로 수렴한다(중력을 직접 적분하지 않는다).
            linear.Y = Approach(linear.Y, -targetFall, config.FallApproach * deltaTime);

            // 가로 — 입력 방향 × 최고 속도가 목표. 입력이 없으면 목표가 0이라 저절로 감속한다.
            var command = entity.Get<InputBuffer>()?.Current;
            float inputX = command == null ? 0f : command.Horizontal;
            float inputZ = command == null ? 0f : command.Vertical;
            float inputLen = (float)System.Math.Sqrt(inputX * inputX + inputZ * inputZ);
            if (inputLen > 1f)
            {
                inputX /= inputLen;
                inputZ /= inputLen;
            }
            linear.X = Approach(linear.X, inputX * maxSide, turnAccel * deltaTime);
            linear.Z = Approach(linear.Z, inputZ * maxSide, turnAccel * deltaTime);

            var position = transform.Position + linear * deltaTime;
            if (position.Y <= config.GroundY)
            {
                position.Y = config.GroundY;
                linear.Y = 0f;
            }

            velocity.Linear = linear;
            transform.Position = position;
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
