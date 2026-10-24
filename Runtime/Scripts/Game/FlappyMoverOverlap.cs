using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 새 캡슐이 움직이는 장애물(풍차·진자·셔터·승강기·낙석)과 겹쳤나. 장애물이 새를 쳐서 들어온 경우를 잡는다 —
    /// sweep은 시작부터 겹친 것을 히트로 안 세서, 이걸 따로 안 보면 밀려나기만 하고 기절하지 않는다.
    /// 캡슐 모양은 <see cref="KinematicMover"/>와 같은 규약(발 위치 + 반지름·높이).
    /// </summary>
    public static class FlappyMoverOverlap
    {
        private static readonly Collider[] Buffer = new Collider[16];

        //  lyingLength: 누운 캡슐의 축 방향 전체 길이. 0 이하면 세운(Y) 캡슐(오늘 그대로) —
        //  두 끝점 식은 CapsuleEnds.Of가 KinematicMover.Cast와 함께 쓴다.
        public static bool StruckBy(Vector3 basePosition, float radius, float height, int layerMask,
            float lyingLength = 0f)
        {
            CapsuleEnds.Of(basePosition, radius, height, lyingLength, out Vector3 p1, out Vector3 p2);
            int count = Physics.OverlapCapsuleNonAlloc(p1, p2, radius, Buffer, layerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (IsMover(Buffer[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 이 콜라이더가 움직이는 장애물(풍차·진자·셔터·승강기·낙석)의 것인가. 장애물은 맵과 같은 층(Default)에
        /// 있어 층으로는 못 가른다 — 표식 컴포넌트로 가린다. 콜라이더가 없으면(테스트 스텁의 히트) 맵으로 본다.
        /// </summary>
        public static bool IsMover(Collider collider)
        {
            return collider != null
                && (collider.GetComponentInParent<FlappyPendulum>() != null
                    || collider.GetComponentInParent<FlappyWindmill>() != null
                    || collider.GetComponentInParent<FlappyShutter>() != null
                    || collider.GetComponentInParent<FlappyLift>() != null
                    || collider.GetComponentInParent<FlappyRockfall>() != null);
        }
    }
}
