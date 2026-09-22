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

        /// <summary>
        /// 지금 들고 있는 화살이 <b>어느 자리 몫인가</b>. 자리가 바뀌면 다시 채운다.
        ///
        /// <para><b>왜 자리마다 채우나</b>: 전체를 한 주머니로 두면 <b>제일 쉬운 자리에 다 붓는 것이
        /// 최적</b>이 된다 — 12m의 10점 링은 화면에서 18.8px, 90m는 7.6px이라 같은 화살로 얻는
        /// 기대 점수가 비교가 안 된다. 그러면 거리를 여섯 개 둔 의미가 통째로 사라진다.
        /// (과녁이 맞으면 사라지던 시절엔 그 제동이 저절로 걸려 있었다.)</para>
        ///
        /// <para>−1은 "아직 한 번도 안 채움". 자리 번호는 0부터라 기본값으로 쓸 수 없다.</para>
        /// </summary>
        public int RefilledWave = -1;
    }
}
