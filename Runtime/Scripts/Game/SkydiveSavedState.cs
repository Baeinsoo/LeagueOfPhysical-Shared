namespace LOP
{
    /// <summary>
    /// 되감기용 Skydive 고유 상태의 한 틱 사진 — 자세·스태미나·이동 상태·실린 바람. 위치·속도는
    /// <see cref="GameFramework.World.WorldBase"/>가 이미 담으므로 여기엔 그 밖의 것만 담는다.
    /// </summary>
    public readonly struct SkydiveSavedState
    {
        public readonly float Axis;
        public readonly bool Gliding;
        public readonly float Stamina;
        public readonly bool EmergencyUsed;
        public readonly float EmergencyRemaining;
        public readonly SkydiveMotionState Motion;
        public readonly System.Numerics.Vector3 Drift;

        // 바람이 수렴해 가는 기준값. Value만 담으면, 볼륨 밖(목표 0)인 틱으로 되감았을 때
        // 살아 있는 Anchor가 그 틱 것과 달라 바람이 빠지는 속도가 어긋난다.
        public readonly System.Numerics.Vector3 DriftAnchor;

        private SkydiveSavedState(float axis, bool gliding, float stamina,
                                  bool emergencyUsed, float emergencyRemaining, SkydiveMotionState motion,
                                  System.Numerics.Vector3 drift, System.Numerics.Vector3 driftAnchor)
        {
            Axis = axis;
            Gliding = gliding;
            Stamina = stamina;
            EmergencyUsed = emergencyUsed;
            EmergencyRemaining = emergencyRemaining;
            Motion = motion;
            Drift = drift;
            DriftAnchor = driftAnchor;
        }

        public static SkydiveSavedState Capture(GameFramework.World.Entity entity)
        {
            var posture = entity.Get<Posture>();
            var stamina = entity.Get<Stamina>();
            var wind = entity.Get<WindDrift>();
            return new SkydiveSavedState(
                posture == null ? 0f : posture.Axis,
                posture != null && posture.Gliding,
                stamina == null ? 0f : stamina.Current,
                stamina != null && stamina.EmergencyUsed,
                stamina == null ? 0f : stamina.EmergencyRemaining,
                entity.Get<MotionState>()?.Value ?? SkydiveMotionState.Walking,
                wind?.Value ?? System.Numerics.Vector3.Zero,
                wind?.Anchor ?? System.Numerics.Vector3.Zero);
        }

        public void RestoreTo(GameFramework.World.Entity entity)
        {
            var posture = entity.Get<Posture>();
            if (posture != null)
            {
                posture.Axis = Axis;
                posture.Gliding = Gliding;
            }

            var stamina = entity.Get<Stamina>();
            if (stamina != null)
            {
                stamina.Current = Stamina;
                stamina.EmergencyUsed = EmergencyUsed;
                stamina.EmergencyRemaining = EmergencyRemaining;
            }

            // 이동 상태도 되돌린다 — 안 되돌리면 재생 중 조작 잠금과 슬라이더 허용이 라이브와
            // 달라져 같은 입력이 다른 궤적을 만든다.
            var motion = entity.Get<MotionState>();
            if (motion != null)
            {
                motion.Value = Motion;
            }

            // 바람도 되돌린다 — 안 되돌리면 재생 중 실린 바람이 라이브와 달라져 같은 입력이
            // 다른 궤적을 만든다. Anchor를 빠뜨리면 볼륨 밖으로 되감았을 때 빠지는 속도가 어긋난다.
            var wind = entity.Get<WindDrift>();
            if (wind != null)
            {
                wind.Value = Drift;
                wind.Anchor = DriftAnchor;
            }
        }
    }
}
