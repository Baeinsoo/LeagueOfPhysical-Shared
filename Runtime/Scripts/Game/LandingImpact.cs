namespace LOP
{
    /// <summary>
    /// 이 틱에 아래로 멈추면서 받은 충격 속도. 착지한 틱에만 값이 있고 나머지 틱은 0이다.
    ///
    /// <para>왜 컴포넌트가 필요한가: 충돌 직전 속도는 <b>이동이 끝나면 사라진다</b>(지면에 막혀
    /// 0이 된다). 문 크러시는 틱과 위치만으로 다시 계산할 수 있어 나를 것이 없었지만, 착지는
    /// 그럴 수 없다.</para>
    ///
    /// <para>스냅샷·저장 상태에 넣지 않는다. 기준은 "매 틱 다시 계산되나"가 아니라
    /// <b>이 틱 안에서 쓰이기 전에 읽히는 값인가</b>다 — 이 값은 이동이 쓴 뒤에야, 같은 틱
    /// 안에서만 읽힌다. 되감아도 그 틱이 다시 돌면 다시 쓰이므로 낡은 값을 물려줄 자리가
    /// 없다. (반례: <see cref="GameFramework.World.GroundState"/>는 자세 판정이 이동보다
    /// 먼저 읽어서, 안 되돌리면 재생 첫 틱이 되감기 전 값을 본다 — 그래서 그건 저장한다.)</para>
    /// </summary>
    public class LandingImpact : GameFramework.World.Component
    {
        /// <summary>아래로 갈 때 양수.</summary>
        public float DownwardSpeed;
    }
}
