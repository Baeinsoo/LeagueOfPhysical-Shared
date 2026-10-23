namespace LOP
{
    /// <summary>
    /// 대시의 충전·발동·소진. 클·서가 같은 구체 클래스를 돌려 결과가 갈리지 않는다.
    ///
    /// <para>게이지는 <b>떨어질수록 빨리 찬다</b> — 이 게임에서 빨라지는 유일한 방법이 대시이므로,
    /// 그 결과 "위험하게 낮게 나는 것"이 곧 보상이 된다. 칸을 <see cref="FlappyDash.MaxCharge"/>개까지
    /// 쌓아 두는 것도 같은 이유다: 상한이 한 칸이면 다 찬 순간부터 낮게 날 이유가 없어진다.</para>
    /// </summary>
    public class FlappyDashSystem
    {
        private readonly FlappyConfig config;
        private readonly FlappyMapRulesField rules;

        /// <param name="rules">
        /// 맵이 다이브 충전을 켜 뒀나. <c>null</c>은 규칙 없이 만든 테스트·도구용이라 켜진 것처럼 동작한다 —
        /// 실제 판은 DI가 늘 필드를 넣는다. 필드가 있으면 <see cref="FlappyMapRulesField"/> 그대로,
        /// 마커 없음 = 꺼짐이다.
        /// </param>
        public FlappyDashSystem(FlappyConfig config, FlappyMapRulesField rules = null)
        {
            this.config = config;
            this.rules = rules;
        }

        /// <summary>지금 대시 중인가. 이동이 이 값을 보고 수평 직선으로 갈지 정한다.</summary>
        public bool IsDashing(GameFramework.World.Entity entity)
        {
            var dash = entity.Get<FlappyDash>();
            return dash != null && dash.DashRemaining > 0f;
        }

        /// <summary>
        /// 한 칸(1.0)이 차 있고 대시 중이 아닐 때만 발동한다. 쓰는 것은 <b>딱 한 칸</b>이라
        /// 남은 칸과 덜 찬 몫은 그대로 남는다 — 그래야 두 칸을 모아 뒀다가 연달아 쓸 수 있다.
        /// (예전엔 게이지를 0으로 밀어서, 1.5칸일 때 쓰면 0.5칸이 증발했다.)
        /// </summary>
        public bool TryActivate(GameFramework.World.Entity entity)
        {
            var dash = entity.Get<FlappyDash>();
            if (dash == null || dash.DashRemaining > 0f || dash.Charge < 1f)
            {
                return false;
            }
            dash.Charge -= 1f;
            dash.DashRemaining = config.DashDuration;
            return true;
        }

        /// <summary>
        /// 부스트 패드를 밟았을 때. <b>게이지를 쓰지 않는 공짜 대시</b>다 — 남은 시간만 채운다.
        ///
        /// <para>새 가속 상태를 만들지 않고 대시를 재사용하는 이유: 그러면 와이어·예측·롤백·
        /// 스택 규칙이 전부 새로 생기는데, 대시를 쓰면 그게 전부 0이고 감각도 같다(수평 직선).</para>
        ///
        /// <para>더 긴 쪽으로만 갱신한다. 패드 위를 지나는 동안 매 틱 다시 밟히므로, 덮어쓰면
        /// 패드를 나가는 순간 항상 <c>duration</c>이 남아 "긴 패드가 더 오래 간다"가 깨진다.</para>
        /// </summary>
        public void Boost(GameFramework.World.Entity entity, float duration)
        {
            var dash = entity.Get<FlappyDash>();
            if (dash == null || duration <= 0f)
            {
                return;
            }
            dash.DashRemaining = System.Math.Max(dash.DashRemaining, duration);
        }

        /// <summary>이 맵이 대시 게이지를 빛 알갱이로 채우나(<see cref="FlappyMapRulesField.MoteCharge"/>).</summary>
        public bool MoteCharge => rules != null && rules.MoteCharge;

        /// <summary>몸 표면에서 이만큼 안의 알갱이를 먹는다(m).</summary>
        public float MoteReach => rules?.MoteReach ?? FlappyMapRulesField.DefaultMoteReach;

        /// <summary>알갱이 <paramref name="count"/>개를 먹었다. 알갱이 맵이 아니면 아무 일도 없다.</summary>
        public void AddMotes(GameFramework.World.Entity entity, int count)
        {
            var dash = entity.Get<FlappyDash>();
            if (dash == null || count <= 0 || MoteCharge == false)
            {
                return;
            }
            dash.Charge = System.Math.Min(FlappyDash.MaxCharge, dash.Charge + count * rules.MoteValue);
        }

        /// <summary>
        /// 스턴이 실제로 걸렸을 때. 알갱이 맵이면 <b>채우던 칸만</b> 잃고 이미 찬 칸은 지킨다. 다이브 맵은 예전처럼 안 잃는다.
        /// <para>10-23 사람 판: 전체 절반을 잃게 했더니 도전적으로 붙을수록 자주 박아 게이지가 한 번도 안 찼다.
        /// 모은 칸을 지키면 "칸이 찼으니 쓸까, 아껴서 하나 더 모을까"가 살아난다.</para>
        /// </summary>
        public void LoseOnStun(GameFramework.World.Entity entity)
        {
            var dash = entity.Get<FlappyDash>();
            if (dash != null && MoteCharge)
            {
                dash.Charge = (float)System.Math.Floor(dash.Charge);
            }
        }

        /// <summary>
        /// 출발선에서 기다리는 동안. 알갱이 맵이면 게이지를 0으로 둔다 — 처음부터 차 있으면 첫 무더기의
        /// 맛이 줄어든다. 새를 만들 때가 아니라 여기서 하는 이유: 새를 만드는 순간엔 맵 규칙이 아직 안
        /// 올라왔을 수 있고, 시뮬 안에서 하면 클·서가 같은 틱에 같은 값을 갖는다.
        /// </summary>
        public void ResetForStart(GameFramework.World.Entity entity)
        {
            var dash = entity.Get<FlappyDash>();
            if (dash != null && MoteCharge)
            {
                dash.Charge = 0f;
            }
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

            //  맵이 수동 대시를 꺼 두면(FlappyMapRules.ManualDash == false) 다이브로는 한 칸도
            //  못 채운다 — 대시는 부스트 패드(Boost)로만 받는다. 마커가 없는 맵도 같다(필드 기본값이 꺼짐).
            //  rules == null은 규칙 없이 만든 테스트·도구뿐이고, 그때만 예전처럼 채운다.
            //  알갱이 맵이면 다이브도 쉰다 — 채우는 길이 둘이면 "붙을까 말까"의 무게가 흐려진다.
            if (rules != null && (rules.ManualDash == false || rules.MoteCharge))
            {
                return;
            }

            if (dash.Charge >= FlappyDash.MaxCharge)
            {
                return;
            }

            //  떨어지는 중일 때만 다이브 몫이 붙는다. 최대낙하로 나눠 정규화하는 것이 핵심이다 —
            //  중력·최대낙하를 튜닝해도 "최고 속도로 떨어지면 최대 충전"이라는 감각이 유지된다.
            //
            //  <b>세제곱인 것이 그다음으로 핵심이다.</b> 날갯짓은 튀었다 떨어지는 반복이라
            //  평범하게 날아도 시간의 절반이 낙하다. 속도에 단순 비례하면 그 평범한 비행
            //  (평균 9.3 = 최대의 31%)이 31%를 그대로 받아가서, "과감하게 내려간다"와
            //  "그냥 난다"를 구분하지 못한다 — 게이지가 늘 가득 차 리스크·리워드가 꺼진다.
            //  세제곱이면 31% → 3%로 33배 벌어진다.
            //
            //  지수는 컨피그로 빼지 않는다. 튜닝 손잡이가 아니라 <i>곡선의 모양</i>이고, 바꾸면
            //  "한 칸 = 깊은 다이브 두 번"이라는 경제가 통째로 다시 계산돼야 한다
            //  (<see cref="FlappyDash.MaxCharge"/>를 상수로 둔 것과 같은 이유다).
            //  문턱 아래 낙하는 다이브로 치지 않는다 — 세제곱만으로는 얕은 하강도 조금씩 쌓여,
            //  깊이 내리꽂지 않아도 게이지가 찼다. 문턱을 넘은 뒤에는 원래 곡선 그대로 받는다.
            float fallSpeed = -(entity.Get<GameFramework.World.Velocity>()?.Linear.Y ?? 0f);
            bool deepEnough = fallSpeed >= config.DashChargeMinFall;
            float normalized = deepEnough && fallSpeed > 0f && config.MaxFallSpeed > 0f
                ? System.Math.Min(fallSpeed, config.MaxFallSpeed) / config.MaxFallSpeed
                : 0f;
            float dive = config.DashChargeDive * normalized * normalized * normalized;

            dash.Charge = System.Math.Min(FlappyDash.MaxCharge,
                                          dash.Charge + (config.DashChargeBase + dive) * deltaTime);
        }
    }
}
