namespace LOP
{
    /// <summary>
    /// Skydive의 이동. 슬라이스 1에서는 중력으로 떨어지는 것뿐이다 —
    /// 자세(항력·수평 가속)는 슬라이스 2, 지형 충돌은 슬라이스 3이 얹는다.
    /// </summary>
    public class SkydiveMoveSystem
    {
        // 슬라이스 2에서 TbSkydiveConfig로 옮긴다. 지금 필요한 것은 "떨어지는 게 보인다"뿐이라
        // 값을 데이터로 뺄 이유가 아직 없다.
        public const float Gravity = 20f;
        public const float MaxFallSpeed = 40f;

        // 진짜 지면은 슬라이스 3의 맵 충돌이 정한다. 그때까지 무한 추락을 막는 임시 바닥이다.
        public const float GroundY = 0f;

        public void Tick(GameFramework.World.Entity entity, float deltaTime)
        {
            var velocity = entity.Get<GameFramework.World.Velocity>();
            var transform = entity.Get<GameFramework.World.Transform>();
            if (velocity == null || transform == null)
            {
                return;
            }

            var linear = velocity.Linear;
            linear.Y -= Gravity * deltaTime;
            if (linear.Y < -MaxFallSpeed)
            {
                linear.Y = -MaxFallSpeed;
            }

            var position = transform.Position + linear * deltaTime;
            if (position.Y <= GroundY)
            {
                position.Y = GroundY;
                linear.Y = 0f;
            }

            velocity.Linear = linear;
            transform.Position = position;
        }
    }
}
