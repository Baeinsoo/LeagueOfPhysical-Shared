namespace LOP
{
    /// <summary>
    /// 스턴 상태의 진입과 시간 감소. 클·서가 같은 구체 클래스를 돌려 결과가 갈리지 않는다.
    /// </summary>
    public class FlappyStunSystem
    {
        private readonly FlappyConfig config;

        public FlappyStunSystem(FlappyConfig config)
        {
            this.config = config;
        }

        /// <summary>지금 스턴 중인가. 스턴 중이면 이번 틱에 속도를 주지 않는다.</summary>
        public bool IsStunned(GameFramework.World.Entity entity)
        {
            var stun = entity.Get<FlappyStun>();
            return stun != null && stun.StunRemaining > 0f;
        }

        /// <summary>맵에 닿았을 때. 이미 스턴 중이거나 무적이면 아무 일도 없다.</summary>
        public void Enter(GameFramework.World.Entity entity)
        {
            var stun = entity.Get<FlappyStun>();
            if (stun == null || stun.StunRemaining > 0f || stun.InvulnRemaining > 0f)
            {
                return;
            }
            stun.StunRemaining = config.StunTime;
        }

        public void Tick(GameFramework.World.Entity entity, float deltaTime)
        {
            var stun = entity.Get<FlappyStun>();
            if (stun == null)
            {
                return;
            }

            if (stun.StunRemaining > 0f)
            {
                stun.StunRemaining -= deltaTime;
                if (stun.StunRemaining <= FlappyTickDuration.Epsilon)
                {
                    // 스턴이 끝나는 그 틱에 무적을 채운다 — 빠져나오는 동안 다시 걸리지 않게.
                    stun.StunRemaining = 0f;
                    stun.InvulnRemaining = config.InvulnTime;
                }
                return;
            }

            if (stun.InvulnRemaining > 0f)
            {
                stun.InvulnRemaining -= deltaTime;
                if (stun.InvulnRemaining <= FlappyTickDuration.Epsilon)
                {
                    stun.InvulnRemaining = 0f;
                }
            }
        }
    }
}
