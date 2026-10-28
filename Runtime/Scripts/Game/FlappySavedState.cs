namespace LOP
{
    /// <summary>
    /// 되감기용 Flappy 고유 상태의 한 틱 사진 — 스턴 타이머, 대시(게이지·남은시간), 통과 기록, 연속 날갯짓 수.
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
        public readonly int FlapCombo;

        private FlappySavedState(float stunRemaining, float invulnRemaining,
                                 float dashCharge, float dashRemaining,
                                 long finishedTick, float finishDepth, int flapCombo)
        {
            StunRemaining = stunRemaining;
            InvulnRemaining = invulnRemaining;
            DashCharge = dashCharge;
            DashRemaining = dashRemaining;
            FinishedTick = finishedTick;
            FinishDepth = finishDepth;
            FlapCombo = flapCombo;
        }

        //  넷을 따로 확인한다 — 일부만 달린 엔티티가 있어도 나머지는 담긴다.
        public static FlappySavedState Capture(GameFramework.World.Entity entity)
        {
            var stun = entity.Get<FlappyStun>();
            var dash = entity.Get<FlappyDash>();
            var finish = entity.Get<FinishState>();
            var combo = entity.Get<FlappyFlapCombo>();
            return new FlappySavedState(
                stun?.StunRemaining ?? 0f,
                stun?.InvulnRemaining ?? 0f,
                dash?.Charge ?? 0f,
                dash?.DashRemaining ?? 0f,
                finish?.FinishedTick ?? FinishState.NotFinished,
                finish?.Depth ?? 0f,
                combo?.Count ?? 0);
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

            //  연속 수는 게이지와 짝으로 되돌린다 — 고비 보상이 "수가 고비 값이 되는 틱"에만 생기므로, 둘이 같이 되돌아가야
            //  재생이 고비를 꼭 한 번 지난다(spec 2026-10-27 §3).
            var combo = entity.Get<FlappyFlapCombo>();
            if (combo != null)
            {
                combo.Count = FlapCombo;
            }
        }
    }
}
