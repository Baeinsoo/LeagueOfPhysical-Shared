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
        /// <summary>가려는 방향과 이 각도보다 벌어지면 급회전으로 보고 목표 속도를 줄인다.</summary>
        public readonly float SharpTurnDeg;
        /// <summary>급회전일 때 목표 속도에 곱하는 값.</summary>
        public readonly float SharpTurnSpeedFactor;
        /// <summary>활을 당기는 동안 목표 속도에 곱하는 값.</summary>
        public readonly float DrawSpeedFactor;

        public MountSettings(float maxSpeed, float acceleration, float deceleration,
                             float turnRateStoppedDeg, float turnRateAtMaxDeg,
                             float sharpTurnDeg, float sharpTurnSpeedFactor, float drawSpeedFactor)
        {
            MaxSpeed = maxSpeed;
            Acceleration = acceleration;
            Deceleration = deceleration;
            TurnRateStoppedDeg = turnRateStoppedDeg;
            TurnRateAtMaxDeg = turnRateAtMaxDeg;
            SharpTurnDeg = sharpTurnDeg;
            SharpTurnSpeedFactor = sharpTurnSpeedFactor;
            DrawSpeedFactor = drawSpeedFactor;
        }
    }
}
