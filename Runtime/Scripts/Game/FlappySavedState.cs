namespace LOP
{
    /// <summary>
    /// 되감기용 Flappy 고유 상태의 한 틱 사진 — 스턴 타이머. 위치·속도는
    /// <see cref="GameFramework.World.WorldBase"/>가 이미 담으므로 여기엔 그 밖의 것만 담는다.
    /// </summary>
    public readonly struct FlappySavedState
    {
        public readonly float StunRemaining;
        public readonly float InvulnRemaining;

        private FlappySavedState(float stunRemaining, float invulnRemaining)
        {
            StunRemaining = stunRemaining;
            InvulnRemaining = invulnRemaining;
        }

        public static FlappySavedState Capture(GameFramework.World.Entity entity)
        {
            var stun = entity.Get<FlappyStun>();
            return stun == null
                ? new FlappySavedState(0f, 0f)
                : new FlappySavedState(stun.StunRemaining, stun.InvulnRemaining);
        }

        public void RestoreTo(GameFramework.World.Entity entity)
        {
            var stun = entity.Get<FlappyStun>();
            if (stun == null)
            {
                return;
            }
            stun.StunRemaining = StunRemaining;
            stun.InvulnRemaining = InvulnRemaining;
        }
    }
}
