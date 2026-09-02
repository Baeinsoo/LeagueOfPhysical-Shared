namespace LOP
{
    /// <summary>
    /// 되감기용 Flappy 고유 상태의 한 틱 사진 — 스턴 타이머와 대시(게이지·남은시간). 위치·속도는
    /// <see cref="GameFramework.World.WorldBase"/>가 이미 담으므로 여기엔 그 밖의 것만 담는다.
    /// </summary>
    public readonly struct FlappySavedState
    {
        public readonly float StunRemaining;
        public readonly float InvulnRemaining;
        public readonly float DashCharge;
        public readonly float DashRemaining;

        private FlappySavedState(float stunRemaining, float invulnRemaining,
                                 float dashCharge, float dashRemaining)
        {
            StunRemaining = stunRemaining;
            InvulnRemaining = invulnRemaining;
            DashCharge = dashCharge;
            DashRemaining = dashRemaining;
        }

        //  스턴과 대시를 따로 확인한다 — 둘 중 하나만 달린 엔티티가 있어도 나머지는 담긴다.
        public static FlappySavedState Capture(GameFramework.World.Entity entity)
        {
            var stun = entity.Get<FlappyStun>();
            var dash = entity.Get<FlappyDash>();
            return new FlappySavedState(
                stun?.StunRemaining ?? 0f,
                stun?.InvulnRemaining ?? 0f,
                dash?.Charge ?? 0f,
                dash?.DashRemaining ?? 0f);
        }

        public void RestoreTo(GameFramework.World.Entity entity)
        {
            var stun = entity.Get<FlappyStun>();
            if (stun != null)
            {
                stun.StunRemaining = StunRemaining;
                stun.InvulnRemaining = InvulnRemaining;
            }

            var dash = entity.Get<FlappyDash>();
            if (dash != null)
            {
                dash.Charge = DashCharge;
                dash.DashRemaining = DashRemaining;
            }
        }
    }
}
