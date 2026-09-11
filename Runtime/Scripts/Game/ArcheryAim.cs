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
    }
}
