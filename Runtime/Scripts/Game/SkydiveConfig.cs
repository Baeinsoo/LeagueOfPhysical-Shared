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

        public SkydiveConfig(
            float spreadFallSpeed, float diveFallSpeed, float glideFallSpeed,
            float spreadMoveSpeed, float diveMoveSpeed, float glideMoveSpeed,
            float spreadTurnAccel, float diveTurnAccel, float glideTurnAccel,
            float fallApproach, float postureRate,
            float bodyRadius, float bodyHeight, float groundY,
            float staminaMax, float glideDrain, float groundRecover, float emergencyGlideTime)
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
        }
    }
}
