namespace LOP
{
    /// <summary>
    /// 스턴 상태의 진입과 시간 감소. 클·서가 같은 구체 클래스를 돌려 결과가 갈리지 않는다.
    /// </summary>
    public class FlappyStunSystem
    {
        // 매 틱 float를 빼 나가면 정확히 0을 찍지 못하고 아주 살짝(예: 2e-7) 남을 수 있다 —
        // 그 잔여를 0으로 봐서 다음 단계 전환이 한 틱 늦게 걸리는 걸 막는다.
        private const float Epsilon = 1e-5f;

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

        /// <summary>
        /// 남은 시간을 "끝나는 절대 틱"으로 바꾼다. 와이어로 나가는 유일한 표현이다.
        ///
        /// <para><see cref="Epsilon"/>을 먼저 빼는 것이 핵심이다. 매 틱 float를 빼면 0.8초가
        /// 0.780000031처럼 아주 조금 크게 남는데, 시뮬은 그 조각을 "끝"으로 보지만(<see cref="Tick"/>)
        /// 올림은 한 틱으로 세어 버린다. 그러면 받는 쪽만 한 틱 더 얼어 있게 되고, 그 한 틱은
        /// 전진과 중력을 통째로 잃어 같은 틱인데 위치가 다른 상태가 된다(라이브 실측).</para>
        /// </summary>
        public static long EndTick(float remaining, long tick, float deltaTime)
        {
            //  Epsilon 이하는 시뮬이 이번 틱에 이미 0으로 만든다 = 남은 틱 없음.
            if (remaining <= Epsilon || deltaTime <= 0f)
            {
                return 0;
            }
            return tick + (long)System.Math.Ceiling((remaining - Epsilon) / deltaTime);
        }

        /// <summary>끝나는 절대 틱에서 지금 틱을 빼 남은 시간(초)으로. 이미 지났거나 같으면 0.</summary>
        public static float RemainingSeconds(long endTick, long tick, float deltaTime)
        {
            long remainingTicks = endTick - tick;
            return remainingTicks > 0 ? remainingTicks * deltaTime : 0f;
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
                if (stun.StunRemaining <= Epsilon)
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
                if (stun.InvulnRemaining <= Epsilon)
                {
                    stun.InvulnRemaining = 0f;
                }
            }
        }
    }
}
