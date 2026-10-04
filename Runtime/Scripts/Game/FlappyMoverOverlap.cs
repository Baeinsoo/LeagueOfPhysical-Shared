using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 새 캡슐이 움직이는 장애물(풍차·진자·셔터)과 겹쳤나. 장애물이 새를 쳐서 들어온 경우를 잡는다 —
    /// sweep은 시작부터 겹친 것을 히트로 안 세서, 이걸 따로 안 보면 밀려나기만 하고 기절하지 않는다.
    /// 캡슐 모양은 <see cref="KinematicMover"/>와 같은 규약(발 위치 + 반지름·높이).
    /// </summary>
    public static class FlappyMoverOverlap
    {
        private static readonly Collider[] Buffer = new Collider[16];

        public static bool StruckBy(Vector3 basePosition, float radius, float height, int layerMask)
        {
            Vector3 p1 = basePosition + Vector3.up * radius;
            Vector3 p2 = basePosition + Vector3.up * (height - radius);
            int count = Physics.OverlapCapsuleNonAlloc(p1, p2, radius, Buffer, layerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider other = Buffer[i];
                if (other.GetComponentInParent<FlappyPendulum>() != null
                    || other.GetComponentInParent<FlappyWindmill>() != null
                    || other.GetComponentInParent<FlappyShutter>() != null)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
