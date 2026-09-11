namespace LOP
{
    /// <summary>
    /// 과녁 하나가 먹혔다는 사실. <b>서버가 확정해 내려보내는 이산 사건</b>이다 — 과녁이 *뜨는* 것은
    /// 양쪽이 계산으로 알지만, 두세 개뿐인 과녁을 <b>누가 먼저 맞혔는지</b>는 계산으로 알 수 없다.
    ///
    /// <para>점수 자체는 이 사건으로 보내지 않는다 — 점수는 유실되면 안 되는 값이라 스냅샷
    /// (<c>EntitySnap.score</c>)이 진실원본이다. 여기 실린 <paramref name="points"/>는 "+2" 같은
    /// 연출용이다.</para>
    /// </summary>
    public sealed record ArcheryTargetHitEvent(
        string shooterId,
        long fireTick,
        int points
    ) : GameFramework.World.WorldEvent;
}
