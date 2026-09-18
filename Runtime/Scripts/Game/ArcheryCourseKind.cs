namespace LOP
{
    /// <summary>
    /// 이 맵의 과녁이 어떻게 뜨는가. <b>맵이 값으로 고른다</b>(<c>TbArcheryConfig.course_kind</c>) —
    /// 코드가 맵 이름을 알지 않게 하려는 것이다.
    /// </summary>
    public enum ArcheryCourseKind
    {
        /// <summary>묶음이 주기적으로 솟는다(원형 맵). 웨이브마다 난수를 여러 번 쓴다.</summary>
        Wave = 0,

        /// <summary>정해진 순서대로 거리별 과녁이 선다(사거리 맵). 난수는 판 시작에 한 번뿐이다.</summary>
        Range = 1,
    }
}
