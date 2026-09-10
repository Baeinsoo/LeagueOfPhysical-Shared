using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 몸 둘이 부딪혔을 때 주고받는 속도(질량은 서로 같다고 본다).
    ///
    /// <para>부딪힌 속도를 0으로 지우면 위에 있는 몸이 아래 몸을 발판처럼 밟고 서게 되고,
    /// 중력이 곧바로 다시 붙여서 매 프레임 재충돌한다. 서로 속도를 주고받아야 갈라진다.</para>
    ///
    /// <para>일반형 <c>j = -(1+e)·v_closing / (1/m₁ + 1/m₂)</c>에 <c>m₁ = m₂</c>를 넣어 정리하면
    /// 아래의 0.5가 남는다. 질량을 다르게 하려면 그 상수 자리를 연다.</para>
    /// </summary>
    public static class ContactImpulse
    {
        /// <summary>이보다 느리게 다가온 충돌은 튕기지 않는다 — 얹혀 있을 때 미세하게 떠는 걸 막는다.</summary>
        public const float RestingSpeed = 1.5f;

        /// <summary>
        /// 충돌 후 self의 속도. <paramref name="normal"/>은 self를 상대 밖으로 밀어내는 방향이다.
        /// 부르는 쪽이 축을 제한하고 싶으면 <b>넣기 전에</b> 세 인자 모두에 마스크를 씌운다 —
        /// 결과에만 씌우면 다가오는 속도가 전 축으로 계산돼 답이 달라진다.
        /// </summary>
        public static Vector3 Resolve(Vector3 vSelf, Vector3 vOther, Vector3 normal, float restitution)
        {
            float closing = Vector3.Dot(vSelf - vOther, normal);
            if (closing >= 0f)
            {
                return vSelf;   // 이미 멀어지는 중이면 건드리지 않는다
            }

            float e = -closing < RestingSpeed ? 0f : restitution;
            return vSelf - (1f + e) * closing * 0.5f * normal;
        }
    }
}
