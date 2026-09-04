namespace LOP
{
    /// <summary>
    /// 결승선을 언제 어떤 깊이로 넘었는지. 시뮬이 적고, 서버가 등수를 매길 때 읽는다.
    ///
    /// <para>깊이를 함께 적는 이유는 <b>같은 틱에 둘이 닿는 일이 기본값</b>이기 때문이다 —
    /// 모든 새가 같은 속도로 달린다. 더 깊이 넘어가 있다는 것은 그만큼 먼저 닿았다는 뜻이다.</para>
    /// </summary>
    public class FinishState : GameFramework.World.Component
    {
        /// <summary>아직 안 넘었다는 표시. 틱 0이 실제로 올 수 있어 0을 못 쓴다.</summary>
        public const long NotFinished = -1;

        public long FinishedTick = NotFinished;

        /// <summary>처음 닿은 틱에 결승선을 넘어간 깊이(m).</summary>
        public float Depth;

        public bool Finished => FinishedTick != NotFinished;
    }
}
