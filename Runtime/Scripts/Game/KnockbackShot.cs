using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 넉백 탄 한 발이 떠난 사실. 탄은 엔티티가 아니다 — 누가·언제·어디서·어느 쪽으로 쐈는지만 있으면
    /// 어느 틱의 위치든 계산되므로 이 값만 오가고 궤적은 양쪽이 각자 낸다. 평면(XZ)에서 움직여 y는 z로 쓴다.
    /// </summary>
    public readonly struct KnockbackShot
    {
        public readonly string ShooterId;
        public readonly long FireTick;
        public readonly Vector2 Origin;
        public readonly Vector2 Direction;

        public KnockbackShot(string shooterId, long fireTick, Vector2 origin, Vector2 direction)
        {
            ShooterId = shooterId;
            FireTick = fireTick;
            Origin = origin;
            Direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        }

        public Vector2 PositionAt(long tick, float speed, float dt)
            => Origin + Direction * (speed * (tick - FireTick) * dt);

        public bool IsSpent(long tick, float speed, float range, float dt)
            => speed * (tick - FireTick) * dt > range;
    }
}
