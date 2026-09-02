namespace LOP
{
    /// <summary>
    /// 대시의 충전·발동·소진. 클·서가 같은 구체 클래스를 돌려 결과가 갈리지 않는다.
    ///
    /// <para>게이지는 <b>떨어질수록 빨리 찬다</b> — 이 게임에서 빨라지는 유일한 방법이 대시이므로,
    /// 그 결과 "위험하게 낮게 나는 것"이 곧 보상이 된다.</para>
    /// </summary>
    public class FlappyDashSystem
    {
        private readonly FlappyConfig config;

        public FlappyDashSystem(FlappyConfig config)
        {
            this.config = config;
        }

        /// <summary>지금 대시 중인가. 이동이 이 값을 보고 수평 직선으로 갈지 정한다.</summary>
        public bool IsDashing(GameFramework.World.Entity entity)
        {
            var dash = entity.Get<FlappyDash>();
            return dash != null && dash.DashRemaining > 0f;
        }

        /// <summary>
        /// 게이지가 가득이고 대시 중이 아닐 때만 발동한다. 게이지는 전부 쓴다 — 부분 사용이 없어야
        /// "지금 쓸까 아낄까"가 매번 온전한 선택이 된다.
        /// </summary>
        public bool TryActivate(GameFramework.World.Entity entity)
        {
            var dash = entity.Get<FlappyDash>();
            if (dash == null || dash.DashRemaining > 0f || dash.Charge < 1f)
            {
                return false;
            }
            dash.Charge = 0f;
            dash.DashRemaining = config.DashDuration;
            return true;
        }

        /// <summary>
        /// 대시를 그 자리에서 끝낸다. 스턴에 들어갈 때 부른다 — 멈춰 있는 동안 타이머만 계속 흐르면
        /// 스턴이 풀렸을 때 남은 대시가 되살아나 "맞고 나서 갑자기 튀어나가는" 그림이 된다.
        /// </summary>
        public void Cancel(GameFramework.World.Entity entity)
        {
            var dash = entity.Get<FlappyDash>();
            if (dash != null)
            {
                dash.DashRemaining = 0f;
            }
        }

        public void Tick(GameFramework.World.Entity entity, float deltaTime)
        {
            var dash = entity.Get<FlappyDash>();
            if (dash == null)
            {
                return;
            }

            if (dash.DashRemaining > 0f)
            {
                dash.DashRemaining -= deltaTime;
                if (dash.DashRemaining <= FlappyTickDuration.Epsilon)
                {
                    dash.DashRemaining = 0f;
                }
            }

            if (dash.Charge >= 1f)
            {
                return;
            }

            //  떨어지는 중일 때만 다이브 몫이 붙고, 그 크기는 낙하 속도에 비례한다. 최대낙하로 나눠
            //  정규화하는 것이 핵심이다 — 중력·최대낙하를 튜닝해도 "최고 속도로 떨어지면 최대 충전"
            //  이라는 감각이 그대로 유지된다(이 값들은 실제로 프로토타입과 다르다).
            float fallSpeed = -(entity.Get<GameFramework.World.Velocity>()?.Linear.Y ?? 0f);
            float dive = fallSpeed > 0f && config.MaxFallSpeed > 0f
                ? config.DashChargeDive * System.Math.Min(fallSpeed, config.MaxFallSpeed) / config.MaxFallSpeed
                : 0f;

            dash.Charge = System.Math.Min(1f, dash.Charge + (config.DashChargeBase + dive) * deltaTime);
        }
    }
}
