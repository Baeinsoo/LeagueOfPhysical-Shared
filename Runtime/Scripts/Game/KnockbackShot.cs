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

        /// <summary>그 틱의 자리를 사거리 끝에서 멈춘 것. 판정은 사거리까지만 본다.</summary>
        public Vector2 PositionWithinRange(long tick, float speed, float range, float dt)
            => Origin + Direction * Mathf.Min(speed * (tick - FireTick) * dt, range);

        /// <summary>
        /// 앞 틱에 이미 사거리 끝에 닿았으면 다 쓴 탄. 사거리를 넘는 그 틱은 아직 살려 두어
        /// 마지막 선분(앞 틱 자리 → 사거리 끝)도 판정되게 한다 — 서버 판정은 월드 틱의 정리 뒤에 돈다.
        /// </summary>
        public bool IsSpent(long tick, float speed, float range, float dt)
            => speed * (tick - 1 - FireTick) * dt >= range;
    }
}
