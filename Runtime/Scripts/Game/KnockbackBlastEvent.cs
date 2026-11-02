using UnityEngine;

namespace LOP
{
    /// <summary>충격파 폭탄이 터졌다는 사실. 그림용이다 — 누가 밀렸는지는 맞음 사건과 스냅샷이 말한다.</summary>
    public sealed record KnockbackBlastEvent(
        string shooterId,
        Vector2 position,
        long tick
    ) : GameFramework.World.WorldEvent;
}
