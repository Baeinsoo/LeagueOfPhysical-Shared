using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 한 틱 사이에 세로로 선 <b>같은 규격</b>의 캡슐 둘이 <b>처음 닿은 순간</b>을 찾아, 그때의
    /// 접촉 방향을 준다(순수 계산 — 물리엔진을 부르지 않는다).
    ///
    /// <para><b>왜 필요한가.</b> 틱이 끝난 모습만 보면 위에서 내리꽂은 몸은 이미 상대를 깊이
    /// 파고든 뒤다. 그렇게 깊이 겹친 두 캡슐을 "가장 짧게 떼어내는 방향"은 <b>옆</b>이라,
    /// 머리를 밟았는데도 옆으로 밀려나고 발밑 접촉이 아예 없던 일이 된다. 초속 90 m/s에서 한
    /// 틱은 1.8 m라 실제로 절반 가까운 밟기가 그렇게 새어 나갔다.</para>
    ///
    /// <para>업계 표준 매핑: <see cref="LaserSweep"/>과 같은 Mirtich의 Conservative
    /// Advancement(확실히 안전한 만큼만 시간을 앞으로 감는 방식)다. 다만 여기는 두 몸이 다
    /// 세로로 서 있고 회전이 없어 각속도 항이 없다 — <b>최대 접근 속도 = 한 틱 동안의 상대
    /// 이동 거리</b>로 끝난다.</para>
    ///
    /// <para>겹침 판정 기하는 <see cref="BodyOverlap"/>과 같은 식(<see cref="BodyOverlap.AxisDelta"/>)을
    /// 쓴다 — 두 벌로 두면 언젠가 서로 다른 답을 내기 때문이다.</para>
    /// </summary>
    public static class BodySweep
    {
        /// <summary>
        /// 이만큼 가까워지면 닿은 것으로 본다. <b>없으면 안 된다</b> — 안전 전진 폭이 남은 거리에
        /// 비례해서, 스치듯 다가오는 경우 거리가 닿는 거리에 <b>가까워지기만 하고 절대 닿지</b>
        /// 않는다(<see cref="LaserSweep.HitTolerance"/>가 같은 이유로 있다).
        /// </summary>
        public const float HitTolerance = 0.01f;

        /// <summary>
        /// 이동 전(<paramref name="aFrom"/>·<paramref name="bFrom"/>)에서 이동 후
        /// (<paramref name="aTo"/>·<paramref name="bTo"/>)까지 곧게 옮겨졌다고 보고, 처음 닿는
        /// 순간의 <paramref name="pushDir"/>(a를 b 밖으로 밀어낼 단위 방향)를 찾는다.
        /// <paramref name="timeOfImpact"/>는 틱 안에서의 시각(0~1)이다.
        ///
        /// <para>못 찾으면 false. 부르는 쪽은 그때 <b>틱 끝 모습의 법선으로 물러선다</b> —
        /// 결론이 안 난 접촉을 버리면 몸이 그냥 통과해 버리기 때문이다(스치는 접촉은 원래
        /// 수렴이 느리다).</para>
        /// </summary>
        public static bool TryContactNormal(Vector3 aFrom, Vector3 aTo, Vector3 bFrom, Vector3 bTo,
                                            float radius, float height,
                                            out Vector3 pushDir, out float timeOfImpact)
        {
            pushDir = Vector3.zero;
            timeOfImpact = 0f;

            float touchDistance = radius * 2f;

            //  틱 동안 둘 사이가 좁아질 수 있는 최대 속도 = 상대 이동 거리. 한쪽을 v만큼 옮기면
            //  두 도형 사이 거리는 아무리 많아도 |v|만큼만 변한다(거리 함수가 이동에 대해 1-립시츠).
            float closing = Vector3.Distance(bTo - bFrom, aTo - aFrom);
            if (closing <= 1e-6f)
            {
                return false;   // 서로 안 움직였다 — 틱 끝 모습이 곧 시작 모습이라 훑을 것이 없다
            }

            //  걸음 수 상한을 <b>이 짝이 실제로 움직인 거리</b>에서 뽑는다. 상수로 박으면 낙하
            //  속도를 올렸을 때 조용히 다시 못 찾기 시작한다. 한 걸음은 "남은 거리가 허용치보다
            //  클 때"만 밟히고 그 걸음의 전진 폭은 HitTolerance보다 크므로, 상대 이동 거리를
            //  허용치로 나눈 횟수를 넘길 수 없다 — 속도를 바꿔도 따라오는 구조적 상한이다.
            int maxSteps = Mathf.CeilToInt(closing / HitTolerance) + 1;

            float t = 0f;
            for (int step = 0; step < maxSteps; step++)
            {
                Vector3 a = Vector3.LerpUnclamped(aFrom, aTo, t);
                Vector3 b = Vector3.LerpUnclamped(bFrom, bTo, t);

                Vector3 delta = BodyOverlap.AxisDelta(a, b, radius, height);
                float distance = delta.magnitude;
                if (distance <= touchDistance + HitTolerance)
                {
                    timeOfImpact = t;
                    //  정확히 같은 자리면 방향을 거리에서 구할 수 없다. BodyOverlap과 <b>같은</b>
                    //  규칙을 쓴다 — 두 곳이 다른 방향을 고르면 클·서가 갈린다.
                    pushDir = distance < 1e-6f ? Vector3.down : -delta / distance;
                    return true;
                }

                t += (distance - touchDistance) / closing;
                if (t >= 1f)
                {
                    return false;   // 이 틱 안에는 안 닿는다
                }
            }
            return false;   // 상한까지 돌고도 결론 못 냄 — 스치는 접촉
        }
    }
}
