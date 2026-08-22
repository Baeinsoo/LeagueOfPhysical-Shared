using GameFramework;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 새 한 마리의 이번 틱 속도를 정한다. 전진은 상수로 고정이고, 세로만 중력과 플랩이 바꾼다.
    /// 클·서가 같은 구체 클래스를 돌려 예측이 권위와 갈리지 않는다.
    /// </summary>
    public class FlappyMoveSystem
    {
        private readonly FlappyConfig config;

        public FlappyMoveSystem(FlappyConfig config)
        {
            this.config = config;
        }

        public void Tick(GameFramework.World.Entity entity, float deltaTime)
        {
            var worldVelocity = entity.Get<GameFramework.World.Velocity>();
            if (worldVelocity == null)
            {
                return;   // 이동 없는 엔티티
            }

            Vector3 velocity = worldVelocity.Linear.ToUnity();

            velocity.y -= config.Gravity * deltaTime;
            if (velocity.y < -config.MaxFallSpeed)
            {
                velocity.y = -config.MaxFallSpeed;
            }

            // 플랩은 지금까지의 세로 속도를 지우고 새로 준다 — 낙하 중에 눌러도 늘 같은 높이로 뜬다.
            // 중력 다음에 오는 것이 중요하다. 앞에 두면 누른 틱의 중력만큼 손해를 봐서 높이가 흔들린다.
            var input = entity.Get<InputBuffer>()?.Current;
            if (input != null && input.Jump)
            {
                velocity.y = config.FlapImpulse;
            }

            // 전진은 플레이어가 바꿀 수 없는 상수다. z를 0으로 붙잡아 코스 밖으로 새지 않게 한다.
            velocity.x = config.ForwardSpeed;
            velocity.z = 0f;

            worldVelocity.Linear = velocity.ToNumerics();
        }
    }
}
