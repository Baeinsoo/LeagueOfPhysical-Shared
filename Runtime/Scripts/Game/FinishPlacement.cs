namespace LOP
{
    /// <summary>
    /// 서버가 정한 결승선 등수(1부터, 0 = 아직). <b>시뮬은 이 값을 읽지도 쓰지도 않는다</b> —
    /// 서버가 채워 스냅샷으로 보내고 화면이 읽는 표시값이다.
    ///
    /// <para>그래서 되돌리기 대상이 아니다(<c>FlappySavedState</c>에 안 담는다). 반대로
    /// <see cref="FinishState"/>는 시뮬이 적는 값이라 담는다 — 둘을 나눠 둔 이유가 이것이다.</para>
    ///
    /// <para>스냅샷을 만드는 코드는 게임을 안 가리므로 게임별 시스템을 알 수 없다. 그래서 등수도
    /// 다른 게임별 값들처럼 <b>컴포넌트에 실어</b> 보낸다 — 없는 게임에서는 0이 나간다.</para>
    /// </summary>
    public class FinishPlacement : GameFramework.World.Component
    {
        public int Value;
    }
}
