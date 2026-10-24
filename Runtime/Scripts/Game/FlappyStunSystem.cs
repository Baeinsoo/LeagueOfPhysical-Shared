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

        /// <summary>
        /// 기절 또는 무적 중인가 — 이 동안은 치는 장애물(진자·풍차·낙석)을 통과한다(10-24 사용자 결정). 그것들에
        /// 밀리지도, 막히지도, 다시 걸리지도 않는다. 맵(정적 지형)과 문(셔터·승강기)은 여전히 막는다.
        /// </summary>
        public bool IsGhost(GameFramework.World.Entity entity)
        {
            var stun = entity.Get<FlappyStun>();
            return stun != null && (stun.StunRemaining > 0f || stun.InvulnRemaining > 0f);
        }

        /// <summary>
        /// 치는 장애물(진자·풍차·낙석) 속에 있는 동안 무적을 붙들어 둔다 — 적어도 한 틱은 더 남긴다. 장애물 속에서 무적이
        /// 끝나면 그 틱에 바로 다시 기절해, 빠져나올 틈 없이 기절이 이어졌다(10-24 제보). 빠져나온 뒤 한 틱이면
        /// 저절로 끝나므로 "장애물을 나와서 새로 맞아야 다시 기절한다"가 된다.
        /// 기절 중이면 아무 일도 없다 — 기절이 끝나는 틱에 Tick이 무적을 채운다.
        /// </summary>
        public void HoldInvuln(GameFramework.World.Entity entity, float deltaTime)
        {
            var stun = entity.Get<FlappyStun>();
            if (stun == null || stun.StunRemaining > 0f)
            {
                return;
            }
            if (stun.InvulnRemaining < deltaTime)
            {
                stun.InvulnRemaining = deltaTime;
            }
        }

        /// <summary>
        /// 맵에 닿았을 때. 이미 스턴 중이거나 무적이면 아무 일도 없다.
        /// 이번에 새로 스턴을 걸었으면 true — 박았을 때의 페널티(게이지 잃기)는 이때만 준다.
        /// </summary>
        public bool Enter(GameFramework.World.Entity entity)
        {
            var stun = entity.Get<FlappyStun>();
            if (stun == null || stun.StunRemaining > 0f || stun.InvulnRemaining > 0f)
            {
                return false;
            }
            stun.StunRemaining = config.StunTime;
            return true;
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
