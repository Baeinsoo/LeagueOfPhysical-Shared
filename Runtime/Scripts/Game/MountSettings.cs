namespace LOP
{
    /// <summary>탈것 이동 수치. 맵 설정에서 온다.</summary>
    public readonly struct MountSettings
    {
        /// <summary>최고 속도(m/s).</summary>
        public readonly float MaxSpeed;
        /// <summary>빨라지는 힘(m/s²).</summary>
        public readonly float Acceleration;
        /// <summary>느려지는 힘(m/s²). 스틱을 놓아도 이만큼씩만 줄어 미끄러진다.</summary>
        public readonly float Deceleration;
        /// <summary>멈춰 있을 때 도는 빠르기(도/초).</summary>
        public readonly float TurnRateStoppedDeg;
        /// <summary>최고 속도일 때 도는 빠르기(도/초). 빠를수록 덜 돈다.</summary>
        public readonly float TurnRateAtMaxDeg;
        /// <summary>쓰이지 않는다 — 말 기준 조작으로 바뀌며 "가려는 방향"이 없어졌다. 마스터데이터 열만 남아 있다.</summary>
        public readonly float SharpTurnDeg;
        /// <summary>쓰이지 않는다(위와 같음).</summary>
        public readonly float SharpTurnSpeedFactor;
        /// <summary>활을 당기는 동안 목표 속도에 곱하는 값.</summary>
        public readonly float DrawSpeedFactor;
        /// <summary>뒤로 갈 때 최고 속도(m/s, 양수로 적는다).</summary>
        public readonly float ReverseSpeed;

        public MountSettings(float maxSpeed, float acceleration, float deceleration,
                             float turnRateStoppedDeg, float turnRateAtMaxDeg,
                             float sharpTurnDeg, float sharpTurnSpeedFactor, float drawSpeedFactor,
                             float reverseSpeed)
        {
            if (reverseSpeed <= 0f)
            {
                throw new System.ArgumentOutOfRangeException(nameof(reverseSpeed), "후진 최고 속도는 0보다 커야 한다 — mount_reverse_speed 열을 확인");
            }
            MaxSpeed = maxSpeed;
            Acceleration = acceleration;
            Deceleration = deceleration;
            TurnRateStoppedDeg = turnRateStoppedDeg;
            TurnRateAtMaxDeg = turnRateAtMaxDeg;
            SharpTurnDeg = sharpTurnDeg;
            SharpTurnSpeedFactor = sharpTurnSpeedFactor;
            DrawSpeedFactor = drawSpeedFactor;
            ReverseSpeed = reverseSpeed;
        }
    }
}
