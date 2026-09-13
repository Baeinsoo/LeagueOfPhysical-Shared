namespace LOP
{
    /// <summary>
    /// 활쏘기가 매치 결과 자루(<c>MatchPlacementInfo.stats</c>)에 넣는 키.
    /// 자루는 타입이 느슨해서 오타를 컴파일이 못 잡는다 — 쓰는 쪽·읽는 쪽이 이 상수만 보게 한다.
    /// </summary>
    public static class ArcheryStatKeys
    {
        /// <summary>최종 점수. 읽는 쪽이 모드별 산수를 몰라도 되게 파생값도 실어 보낸다.</summary>
        public const string Score = "score";

        /// <summary>과녁을 맞혀 얻은 합.</summary>
        public const string Gained = "gained";

        /// <summary>함정을 맞혀 깎인 합.</summary>
        public const string Lost = "lost";
    }
}
