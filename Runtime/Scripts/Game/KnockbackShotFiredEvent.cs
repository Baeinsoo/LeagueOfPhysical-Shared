using UnityEngine;

namespace LOP
{
    /// <summary>넉백 탄이 떠났다는 사실. 서버가 확정해 내려보내는 이산 사건이다(남의 발사를 입력 재생으로 되살리지 않는다).</summary>
    public sealed record KnockbackShotFiredEvent(
        string shooterId,
        long fireTick,
        Vector2 origin,
        Vector2 direction
    ) : GameFramework.World.WorldEvent;
}
