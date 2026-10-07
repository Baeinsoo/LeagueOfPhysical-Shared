namespace LOP
{
    /// <summary>
    /// 모든 모드가 매치 결과 자루(<c>MatchPlacementInfo.stats</c>)에 함께 쓰는 키. 모드 고유 키는 각 모드가 정한다(<see cref="ArcheryStatKeys"/>).
    /// 자루는 서버 → 로비 저장 → 결과 화면·전적까지 그대로 흐른다 — 새 칸을 만들지 않고 여기에 싣는다.
    /// </summary>
    public static class MatchStatKeys
    {
        /// <summary>판 도중 나가 끝까지 안 돌아왔다(값 1). 그래서 꼴찌다 — 화면이 "나감"이라고 알린다.</summary>
        public const string Left = "left";
    }
}
