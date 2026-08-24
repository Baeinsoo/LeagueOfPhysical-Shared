namespace LOP
{
    /// <summary>
    /// 유령 상태의 진입과 시간 감소. 클·서가 같은 구체 클래스를 돌려 결과가 갈리지 않는다.
    /// </summary>
    public class FlappyGhostSystem
    {
        // 매 틱 float를 빼 나가면 정확히 0을 찍지 못하고 아주 살짝(예: 2e-7) 남을 수 있다 —
        // 그 잔여를 0으로 봐서 다음 단계 전환이 한 틱 늦게 걸리는 걸 막는다.
        private const float Epsilon = 1e-5f;

        private readonly FlappyConfig config;

        public FlappyGhostSystem(FlappyConfig config)
        {
            this.config = config;
        }

        /// <summary>지금 멈춰 있는가. 멈춰 있으면 이번 틱에 속도를 주지 않는다.</summary>
        public bool IsStopped(GameFramework.World.Entity entity)
        {
            var ghost = entity.Get<FlappyGhost>();
            return ghost != null && ghost.Remaining > 0f;
        }

        /// <summary>맵에 닿았을 때. 이미 멈춰 있거나 무적이면 아무 일도 없다.</summary>
        public void Enter(GameFramework.World.Entity entity)
        {
            var ghost = entity.Get<FlappyGhost>();
            if (ghost == null || ghost.Remaining > 0f || ghost.InvulnRemaining > 0f)
            {
                return;
            }
            ghost.Remaining = config.GhostTime;
        }

        public void Tick(GameFramework.World.Entity entity, float deltaTime)
        {
            var ghost = entity.Get<FlappyGhost>();
            if (ghost == null)
            {
                return;
            }

            if (ghost.Remaining > 0f)
            {
                ghost.Remaining -= deltaTime;
                if (ghost.Remaining <= Epsilon)
                {
                    // 멈춤이 끝나는 그 틱에 무적을 채운다 — 빠져나오는 동안 다시 걸리지 않게.
                    ghost.Remaining = 0f;
                    ghost.InvulnRemaining = config.InvulnTime;
                }
                return;
            }

            if (ghost.InvulnRemaining > 0f)
            {
                ghost.InvulnRemaining -= deltaTime;
                if (ghost.InvulnRemaining <= Epsilon)
                {
                    ghost.InvulnRemaining = 0f;
                }
            }
        }
    }
}
