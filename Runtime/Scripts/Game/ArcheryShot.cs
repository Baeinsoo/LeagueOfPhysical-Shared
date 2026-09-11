using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 화살 한 발이 떠난 사실. <b>화살은 엔티티가 아니다</b> — 누가·언제·어디서·어느 속도로
    /// 떠났는지만 있으면 어느 시각의 위치든 계산되므로, 이 다섯 값만 오가고 궤적은 양쪽이 각자 낸다.
    /// </summary>
    public readonly struct ArcheryShot
    {
        public readonly string ShooterId;
        public readonly long FireTick;
        public readonly Vector3 Origin;
        public readonly Vector3 Velocity;

        public ArcheryShot(string shooterId, long fireTick, Vector3 origin, Vector3 velocity)
        {
            ShooterId = shooterId;
            FireTick = fireTick;
            Origin = origin;
            Velocity = velocity;
        }
    }
}
