namespace LOP
{
    /// <summary>
    /// Skydive 튜닝값. MasterData <c>TbSkydiveConfig</c>에서 사이드 provider가 채워 시뮬에 주입한다.
    /// Shared는 MasterData 패키지를 참조하지 않으므로 순수 struct로 건네받는다(<see cref="FlappyConfig"/>와 같은 짝).
    /// </summary>
    public readonly struct SkydiveConfig
    {
        /// <summary>대자로 안정됐을 때의 하강 속도(양수).</summary>
        public readonly float SpreadFallSpeed;
        /// <summary>완전한 다이브의 하강 속도(양수). 가장 크다.</summary>
        public readonly float DiveFallSpeed;
        /// <summary>패러세일의 하강 속도(양수). 가장 작다.</summary>
        public readonly float GlideFallSpeed;

        /// <summary>대자의 수평 최고 속도.</summary>
        public readonly float SpreadMoveSpeed;
        /// <summary>다이브의 수평 최고 속도.</summary>
        public readonly float DiveMoveSpeed;
        /// <summary>패러세일의 수평 최고 속도.</summary>
        public readonly float GlideMoveSpeed;

        /// <summary>대자의 수평 가속 — 방향을 얼마나 빨리 바꾸나. 셋 중 가장 크다(대자가 제일 민첩).</summary>
        public readonly float SpreadTurnAccel;
        /// <summary>다이브의 수평 가속. 가장 작다 — 빠른 대신 못 꺾는다.</summary>
        public readonly float DiveTurnAccel;
        /// <summary>패러세일의 수평 가속.</summary>
        public readonly float GlideTurnAccel;

        /// <summary>실제 하강 속도가 자세의 목표 속도로 다가가는 가속(m/s²). 자세를 바꿔도 속도가 튀지 않게 한다.</summary>
        public readonly float FallApproach;
        /// <summary>자세 축이 1초에 바뀔 수 있는 양. 4면 0↔1 전환에 0.25초가 걸린다.</summary>
        public readonly float PostureRate;

        /// <summary>몸 캡슐 반지름. 클·서가 같은 값을 써야 한다.</summary>
        public readonly float BodyRadius;
        /// <summary>몸 캡슐 전체 높이.</summary>
        public readonly float BodyHeight;
        /// <summary>임시 바닥 높이. 슬라이스 3의 맵 충돌이 이 자리를 대체한다.</summary>
        public readonly float GroundY;

        /// <summary>스태미나 최대치.</summary>
        public readonly float StaminaMax;
        /// <summary>패러세일을 켜 둔 동안 초당 줄어드는 양.</summary>
        public readonly float GlideDrain;
        /// <summary>발 딛고 있을 때 초당 차는 양. 공중에서는 차지 않는다.</summary>
        public readonly float GroundRecover;
        /// <summary>잔고 0에서 허용되는 마지막 펼침의 지속 시간(초).</summary>
        public readonly float EmergencyGlideTime;

        /// <summary>발판 위에서 걸을 때의 최고 속도. 공중의 자세별 속도와 별개다.</summary>
        public readonly float GroundMoveSpeed;
        /// <summary>
        /// 걸을 때 목표 속도로 따라붙는 빠르기(m/s²). 공중값(6~22)보다 훨씬 커야 안 미끄러진다 —
        /// 표준 걷기 모터(<see cref="MovementSystem"/>)가 쓰는 값과 같은 자리다.
        /// </summary>
        public readonly float GroundAccel;
        /// <summary>발판에서 뛸 때의 처음 세로 속도. 도달 높이는 이 값²/(2×FallApproach)다.</summary>
        public readonly float JumpPower;
        /// <summary>
        /// 자세(대자·다이브·패러세일)를 잡으려면 발밑에 있어야 하는 여유. 지면 코앞에서는
        /// 자세를 못 잡는다 — 젤다의 "선 채로 낙하" 상태가 여기에 해당한다.
        /// </summary>
        public readonly float PoseClearance;

        public SkydiveConfig(
            float spreadFallSpeed, float diveFallSpeed, float glideFallSpeed,
            float spreadMoveSpeed, float diveMoveSpeed, float glideMoveSpeed,
            float spreadTurnAccel, float diveTurnAccel, float glideTurnAccel,
            float fallApproach, float postureRate,
            float bodyRadius, float bodyHeight, float groundY,
            float staminaMax, float glideDrain, float groundRecover, float emergencyGlideTime,
            float groundMoveSpeed, float groundAccel, float jumpPower, float poseClearance)
        {
            SpreadFallSpeed = spreadFallSpeed;
            DiveFallSpeed = diveFallSpeed;
            GlideFallSpeed = glideFallSpeed;
            SpreadMoveSpeed = spreadMoveSpeed;
            DiveMoveSpeed = diveMoveSpeed;
            GlideMoveSpeed = glideMoveSpeed;
            SpreadTurnAccel = spreadTurnAccel;
            DiveTurnAccel = diveTurnAccel;
            GlideTurnAccel = glideTurnAccel;
            FallApproach = fallApproach;
            PostureRate = postureRate;
            BodyRadius = bodyRadius;
            BodyHeight = bodyHeight;
            GroundY = groundY;
            StaminaMax = staminaMax;
            GlideDrain = glideDrain;
            GroundRecover = groundRecover;
            EmergencyGlideTime = emergencyGlideTime;
            GroundMoveSpeed = groundMoveSpeed;
            GroundAccel = groundAccel;
            JumpPower = jumpPower;
            PoseClearance = poseClearance;
        }
    }
}
