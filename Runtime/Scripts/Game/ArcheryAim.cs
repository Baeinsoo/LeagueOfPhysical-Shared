namespace LOP
{
    /// <summary>
    /// 활을 겨누고 당기는 상태(데이터만). 처리는 <see cref="ArcheryAimSystem"/>이 한다.
    /// </summary>
    public class ArcheryAim : GameFramework.World.Component
    {
        /// <summary>좌우 조준 각도(도).</summary>
        public float Yaw;

        /// <summary>위아래 조준 각도(도). 양수면 위를 본다.</summary>
        public float Pitch;

        /// <summary>지금 당기고 있나.</summary>
        public bool Drawing;

        /// <summary>당기기 시작한 절대 틱. <see cref="Drawing"/>이 거짓이면 의미 없다.</summary>
        public long DrawStartTick;

        /// <summary>
        /// 얼마나 당겼나(0~1). 손가락이 끈 거리가 아니라 <see cref="ArcheryAimSystem"/>이 정해진
        /// 속도(<see cref="ArcheryAimSystem.DrawRisePerSecond"/>/<see cref="ArcheryAimSystem.DrawFallPerSecond"/>)로
        /// 매 틱 램프업/램프다운한 값이다 — 진실원본은 여기(시뮬)에만 있다.
        /// <see cref="ArcheryAimSystem.DrawThreshold"/>를 못 넘고 떼면 안 쏘고 취소된다.
        /// </summary>
        public float DrawRatio;
    }
}
