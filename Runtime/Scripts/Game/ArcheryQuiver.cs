namespace LOP
{
    /// <summary>
    /// 남은 화살(데이터만). <b>이 컴포넌트가 붙어 있을 때만 화살에 한도가 있다</b> —
    /// 원형 맵은 안 붙이므로 예전처럼 무제한이다.
    ///
    /// <para>서버가 권위지만 클라도 <b>같은 규칙으로 세어</b> 예측한다(발사 자체가 이미 예측이다).
    /// 그래서 되감을 때 이 값도 같이 되돌아가야 한다 — <see cref="ArcheryWorld"/>가 저장한다.</para>
    /// </summary>
    public class ArcheryQuiver : GameFramework.World.Component
    {
        /// <summary>쏠 수 있는 화살 수. 0이면 더 못 쏜다.</summary>
        public int Remaining;
    }
}
