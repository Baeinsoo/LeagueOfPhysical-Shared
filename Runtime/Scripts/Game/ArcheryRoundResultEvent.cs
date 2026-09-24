using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// 한 발 승부의 한 라운드가 끝났다는 사실(연출용). 결과 화면이 <b>이 사건 하나로</b> 그려지게
    /// 모두의 착탄점을 싣는다. 점수의 진실원본은 스냅샷이다 — 유실돼도 점수는 맞다.
    /// </summary>
    public sealed record ArcheryRoundResultEvent(
        int roundIndex,
        int multiplier,
        IReadOnlyList<ArcheryRoundPlacement> placements
    ) : GameFramework.World.WorldEvent;
}
