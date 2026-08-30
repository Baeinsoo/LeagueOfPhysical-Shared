namespace LOP
{
    /// <summary>
    /// 되감기용 Skydive 고유 상태의 한 틱 사진 — 자세와 스태미나. 위치·속도는
    /// <see cref="GameFramework.World.WorldBase"/>가 이미 담으므로 여기엔 그 밖의 것만 담는다.
    /// </summary>
    public readonly struct SkydiveSavedState
    {
        public readonly float Axis;
        public readonly bool Gliding;
        public readonly float Stamina;
        public readonly bool EmergencyUsed;
        public readonly float EmergencyRemaining;

        private SkydiveSavedState(float axis, bool gliding, float stamina,
                                  bool emergencyUsed, float emergencyRemaining)
        {
            Axis = axis;
            Gliding = gliding;
            Stamina = stamina;
            EmergencyUsed = emergencyUsed;
            EmergencyRemaining = emergencyRemaining;
        }

        public static SkydiveSavedState Capture(GameFramework.World.Entity entity)
        {
            var posture = entity.Get<Posture>();
            var stamina = entity.Get<Stamina>();
            return new SkydiveSavedState(
                posture == null ? 0f : posture.Axis,
                posture != null && posture.Gliding,
                stamina == null ? 0f : stamina.Current,
                stamina != null && stamina.EmergencyUsed,
                stamina == null ? 0f : stamina.EmergencyRemaining);
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
        }
    }
}
