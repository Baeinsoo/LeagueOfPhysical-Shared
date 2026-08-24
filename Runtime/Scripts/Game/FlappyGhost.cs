namespace LOP
{
    /// <summary>
    /// 맵에 부딪힌 새가 잠깐 멈춰 있는 상태. 멈춤이 끝나면 잠깐 무적이 되어 같은 벽에 연달아 걸리지 않는다.
    /// 데이터만 갖는다 — 진입·감소는 <see cref="FlappyGhostSystem"/>이 한다.
    /// </summary>
    public class FlappyGhost : GameFramework.World.Component
    {
        /// <summary>멈춤이 끝나기까지 남은 시간(초). 0이면 정상 상태다.</summary>
        public float Remaining;

        /// <summary>다시 걸리지 않는 시간이 끝나기까지 남은 시간(초).</summary>
        public float InvulnRemaining;
    }
}
