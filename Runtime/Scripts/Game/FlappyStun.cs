namespace LOP
{
    /// <summary>
    /// 부딪혀서 잠시 못 움직이는 상태(스턴). 스턴이 끝나면 잠깐 무적이 되어 같은 벽에 연달아 걸리지 않는다.
    /// 데이터만 갖는다 — 진입·감소는 <see cref="FlappyStunSystem"/>이 한다.
    /// </summary>
    public class FlappyStun : GameFramework.World.Component
    {
        /// <summary>스턴이 끝나기까지 남은 시간(초). 0이면 정상 상태다.</summary>
        public float StunRemaining;

        /// <summary>다시 걸리지 않는 시간이 끝나기까지 남은 시간(초).</summary>
        public float InvulnRemaining;
    }
}
