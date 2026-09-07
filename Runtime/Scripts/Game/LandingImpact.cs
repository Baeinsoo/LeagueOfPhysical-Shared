namespace LOP
{
    /// <summary>
    /// 이 틱에 아래로 멈추면서 받은 충격 속도. 착지한 틱에만 값이 있고 나머지 틱은 0이다.
    ///
    /// <para>왜 컴포넌트가 필요한가: 충돌 직전 속도는 <b>이동이 끝나면 사라진다</b>(지면에 막혀
    /// 0이 된다). 문 크러시는 틱과 위치만으로 다시 계산할 수 있어 나를 것이 없었지만, 착지는
    /// 그럴 수 없다.</para>
    ///
    /// <para><see cref="GameFramework.World.GroundState"/>와 같은 성질이라 스냅샷·저장 상태에
    /// 넣지 않는다 — 매 틱 이동이 다시 계산하므로 되감기 재생이 같은 값을 낸다.</para>
    /// </summary>
    public class LandingImpact : GameFramework.World.Component
    {
        /// <summary>아래로 갈 때 양수.</summary>
        public float DownwardSpeed;
    }
}
