namespace LOP
{
    /// <summary>
    /// Flappy Race 튜닝값. MasterData <c>TbFlappyConfig</c>에서 사이드 provider가 채워 시뮬에 주입한다.
    /// Shared는 MasterData 패키지를 참조하지 않으므로 순수 struct로 건네받는다(<see cref="CombatConfig"/>와 같은 짝).
    /// </summary>
    public readonly struct FlappyConfig
    {
        /// <summary>고정 전진 속도(+X). 플레이어가 바꿀 수 없다 — 이 게임의 전진은 조작 대상이 아니다.</summary>
        public readonly float ForwardSpeed;

        /// <summary>플랩 순간의 세로 속도. 지금까지의 세로 속도를 덮어쓴다.</summary>
        public readonly float FlapImpulse;

        /// <summary>중력 가속도(아래로 당기는 크기라 양수).</summary>
        public readonly float Gravity;

        /// <summary>낙하 속도 상한(양수). 이보다 빠르게 떨어지지 않는다.</summary>
        public readonly float MaxFallSpeed;

        /// <summary>새 몸 캡슐의 반지름. 맵 충돌과 새끼리 몸싸움이 같은 값을 쓴다.</summary>
        public readonly float BodyRadius;

        /// <summary>새 몸 캡슐의 전체 높이(발밑부터 정수리까지).</summary>
        public readonly float BodyHeight;

        /// <summary>몸싸움 반발계수 — 0이면 부딪힌 자리에 얹히고, 1이면 온전히 튕겨 나간다.</summary>
        public readonly float Restitution;

        /// <summary>맵에 부딪혔을 때 그 자리에 멈춰 있는 시간(초). 이 시간 손실이 페널티다.</summary>
        public readonly float StunTime;

        /// <summary>스턴이 풀린 뒤 다시 걸리지 않는 시간(초). 같은 벽에 연달아 걸리는 것을 막는다.</summary>
        public readonly float InvulnTime;

        /// <summary>대시 중 전진 배수. 이 게임에서 전진 속도가 바뀌는 유일한 경우다.</summary>
        public readonly float DashMult;

        /// <summary>대시가 지속되는 시간(초).</summary>
        public readonly float DashDuration;

        /// <summary>가만히 있어도 차는 초당 충전량.</summary>
        public readonly float DashChargeBase;

        /// <summary>
        /// 최고 속도로 떨어질 때 <see cref="DashChargeBase"/>에 더해지는 초당 충전량.
        /// 낙하 속도에 비례하므로 천천히 떨어지면 그만큼만 붙는다.
        /// </summary>
        public readonly float DashChargeDive;

        public FlappyConfig(float forwardSpeed, float flapImpulse, float gravity, float maxFallSpeed,
                            float bodyRadius, float bodyHeight, float restitution,
                            float stunTime, float invulnTime,
                            float dashMult, float dashDuration,
                            float dashChargeBase, float dashChargeDive)
        {
            ForwardSpeed = forwardSpeed;
            FlapImpulse = flapImpulse;
            Gravity = gravity;
            MaxFallSpeed = maxFallSpeed;
            BodyRadius = bodyRadius;
            BodyHeight = bodyHeight;
            Restitution = restitution;
            StunTime = stunTime;
            InvulnTime = invulnTime;
            DashMult = dashMult;
            DashDuration = dashDuration;
            DashChargeBase = dashChargeBase;
            DashChargeDive = dashChargeDive;
        }
    }
}
