namespace LOP
{
    /// <summary>탄이 사람을 맞혔다는 사실. 어느 탄인지는 (쏜 사람, 발사 틱)으로 가리킨다.</summary>
    public sealed record KnockbackHitEvent(
        string shooterId,
        long fireTick,
        string victimId
    ) : GameFramework.World.WorldEvent;
}
