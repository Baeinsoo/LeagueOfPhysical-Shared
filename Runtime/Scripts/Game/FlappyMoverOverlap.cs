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

        //  메서드 그룹을 넘길 때마다 대리자가 새로 생긴다 — 매 틱 부르는 자리라 한 번 만들어 둔다.
        public static readonly System.Func<Collider, bool> MoverFilter = IsMover;
        public static readonly System.Func<Collider, bool> StrikingFilter = IsStrikingMover;
        public static readonly System.Func<Collider, bool> GateFilter = IsGate;

        //  lyingLength: 누운 캡슐의 축 방향 전체 길이. 0 이하면 세운(Y) 캡슐(오늘 그대로) —
        //  두 끝점 식은 CapsuleEnds.Of가 KinematicMover.Cast와 함께 쓴다.
        public static bool StruckBy(Vector3 basePosition, float radius, float height, int layerMask,
            float lyingLength = 0f)
            => Overlaps(basePosition, radius, height, layerMask, lyingLength, MoverFilter);

        /// <summary>
        /// 캡슐이 <paramref name="which"/>가 고른 콜라이더와 겹쳤나. 기절·무적인 새는 치는 장애물과 문을 따로 봐야
        /// 해서(<see cref="IsStrikingMover"/>·<see cref="IsGate"/>) 거름을 인자로 받는다.
        /// </summary>
        public static bool Overlaps(Vector3 basePosition, float radius, float height, int layerMask,
            float lyingLength, System.Func<Collider, bool> which)
        {
            CapsuleEnds.Of(basePosition, radius, height, lyingLength, out Vector3 p1, out Vector3 p2);
            int count = Physics.OverlapCapsuleNonAlloc(p1, p2, radius, Buffer, layerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (which(Buffer[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 이 콜라이더가 움직이는 장애물(치는 장애물 + 문)의 것인가. 장애물은 맵과 같은 층(Default)에
        /// 있어 층으로는 못 가른다 — 표식 컴포넌트로 가린다. 콜라이더가 없으면(테스트 스텁의 히트) 맵으로 본다.
        /// </summary>
        public static bool IsMover(Collider collider) => IsStrikingMover(collider) || IsGate(collider);

        /// <summary>
        /// 새를 <b>치는</b> 장애물(진자·풍차·낙석)인가. 기절·무적인 새는 이것만 통과한다(10-24 사용자 결정) —
        /// 맞고 멈춘 새를 계속 끌고 다니거나 다시 쳐서 연속 기절을 만들던 쪽이 이것들이다.
        /// </summary>
        public static bool IsStrikingMover(Collider collider)
        {
            return collider != null
                && (collider.GetComponentInParent<FlappyPendulum>() != null
                    || collider.GetComponentInParent<FlappyWindmill>() != null
                    || collider.GetComponentInParent<FlappyRockfall>() != null);
        }

        /// <summary>
        /// 길을 여닫는 <b>문</b>(셔터·승강기)인가. 기절·무적이어도 문은 예전처럼 막고 민다(10-24 사용자 결정) —
        /// 통과시키면 닫힌 문에 일부러 박아 기절한 뒤 그 무적으로 문을 뚫고 지나갈 수 있다.
        /// </summary>
        public static bool IsGate(Collider collider)
        {
            return collider != null
                && (collider.GetComponentInParent<FlappyShutter>() != null
                    || collider.GetComponentInParent<FlappyLift>() != null);
        }
    }
}
