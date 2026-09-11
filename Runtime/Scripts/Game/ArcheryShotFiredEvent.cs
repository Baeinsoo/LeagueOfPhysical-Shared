using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 화살 한 발이 떠났다는 사실. <b>서버가 확정해 내려보내는 이산 사건</b>이다 —
    /// 남의 발사를 입력 재생으로 되살리려 하면 늦게 도착한 입력이 영영 안 읽혀서 실패한다
    /// (2026-09-11 실측: lag 9~10틱, current 항상 null).
    /// </summary>
    public sealed record ArcheryShotFiredEvent(
        string shooterId,
        long fireTick,
        Vector3 origin,
        Vector3 velocity
    ) : GameFramework.World.WorldEvent;
}
