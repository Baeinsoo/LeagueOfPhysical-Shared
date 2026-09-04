namespace LOP
{
    /// <summary>
    /// 되감기용 Flappy 고유 상태의 한 틱 사진 — 스턴 타이머, 대시(게이지·남은시간), 통과 기록.
    /// 위치·속도는
    /// <see cref="GameFramework.World.WorldBase"/>가 이미 담으므로 여기엔 그 밖의 것만 담는다.
    /// </summary>
    public readonly struct FlappySavedState
    {
        public readonly float StunRemaining;
        public readonly float InvulnRemaining;
        public readonly float DashCharge;
        public readonly float DashRemaining;
        public readonly long FinishedTick;
        public readonly float FinishDepth;

        private FlappySavedState(float stunRemaining, float invulnRemaining,
                                 float dashCharge, float dashRemaining,
                                 long finishedTick, float finishDepth)
        {
            StunRemaining = stunRemaining;
            InvulnRemaining = invulnRemaining;
            DashCharge = dashCharge;
            DashRemaining = dashRemaining;
            FinishedTick = finishedTick;
            FinishDepth = finishDepth;
        }

        //  셋을 따로 확인한다 — 일부만 달린 엔티티가 있어도 나머지는 담긴다.
        public static FlappySavedState Capture(GameFramework.World.Entity entity)
        {
            var stun = entity.Get<FlappyStun>();
            var dash = entity.Get<FlappyDash>();
            var finish = entity.Get<FinishState>();
            return new FlappySavedState(
                stun?.StunRemaining ?? 0f,
                stun?.InvulnRemaining ?? 0f,
                dash?.Charge ?? 0f,
                dash?.DashRemaining ?? 0f,
                finish?.FinishedTick ?? FinishState.NotFinished,
                finish?.Depth ?? 0f);
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

            //  통과 기록도 되돌린다 — 클라가 통과를 예측하므로, 안 되돌리면 재생 뒤에도
            //  "이미 통과함"이 남아 새가 영영 감속한 채로 있는다.
            var finish = entity.Get<FinishState>();
            if (finish != null)
            {
                finish.FinishedTick = FinishedTick;
                finish.Depth = FinishDepth;
            }
        }
    }
}
