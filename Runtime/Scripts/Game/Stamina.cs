namespace LOP
{
    /// <summary>
    /// 한 판 동안 쓸 수 있는 활공 총량. 패러세일만 이걸 먹고, 자유낙하는 공짜다.
    /// </summary>
    public class Stamina : GameFramework.World.Component
    {
        public float Current;

        /// <summary>잔고 0에서의 "마지막 한 번" 펼침을 이미 썼나.</summary>
        public bool EmergencyUsed;

        /// <summary>그 마지막 펼침이 끝나기까지 남은 시간(초). 0이면 비상 상태가 아니다.</summary>
        public float EmergencyRemaining;
    }
}
