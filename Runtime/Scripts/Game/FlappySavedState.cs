namespace LOP
{
    /// <summary>
    /// 되감기용 Flappy 고유 상태의 한 틱 사진 — 유령정지 타이머. 위치·속도는
    /// <see cref="GameFramework.World.WorldBase"/>가 이미 담으므로 여기엔 그 밖의 것만 담는다.
    /// </summary>
    public readonly struct FlappySavedState
    {
        public readonly float GhostRemaining;
        public readonly float InvulnRemaining;

        private FlappySavedState(float ghostRemaining, float invulnRemaining)
        {
            GhostRemaining = ghostRemaining;
            InvulnRemaining = invulnRemaining;
        }

        public static FlappySavedState Capture(GameFramework.World.Entity entity)
        {
            var ghost = entity.Get<FlappyGhost>();
            return ghost == null
                ? new FlappySavedState(0f, 0f)
                : new FlappySavedState(ghost.Remaining, ghost.InvulnRemaining);
        }

        public void RestoreTo(GameFramework.World.Entity entity)
        {
            var ghost = entity.Get<FlappyGhost>();
            if (ghost == null)
            {
                return;
            }
            ghost.Remaining = GhostRemaining;
            ghost.InvulnRemaining = InvulnRemaining;
        }
    }
}
