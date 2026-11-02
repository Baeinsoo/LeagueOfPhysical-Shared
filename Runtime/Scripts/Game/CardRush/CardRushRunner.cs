namespace LOP
{
    /// <summary>선수 하나의 카드 러시 상태. 되감기 저장 단위 그대로라 값 복사로 저장·복원한다.</summary>
    public struct CardRushRunnerState
    {
        public int Step;            // 지금 잡으려는 칸(0부터). StepCount면 완주
        public int Attempt;         // 이 칸에서 틀린 횟수 — 시작 색 씨앗
        public long MoveStartTick;  // 마지막으로 맞힌 틱(완주 틱도 이것). 0 = 아직 없음
        public long StunEndTick;    // 마지막 틀림의 멈춤이 끝나는 틱. 0 = 아직 없음
        public int Streak;
        public int BestStreak;
        public int Misses;

        public bool IsFinished(int stepCount) => Step >= stepCount;
    }

    public sealed class CardRushRunner : GameFramework.World.Component
    {
        public CardRushRunnerState State;
        /// <summary>스폰 때 정해지는 레인 출발점. 안 바뀌어 되감기 대상이 아니다.</summary>
        public UnityEngine.Vector3 LaneOrigin;
    }
}
