using UnityEngine;

namespace LOP
{
    /// <summary>
    /// <see cref="ContactImpulse"/>의 세로 전용 입구. 전진 속도가 상수로 고정돼 손댈 수 없던
    /// 게임(Flappy Race)과 그 프로토타입이 이 모양으로 부른다.
    ///
    /// <para>세로만 넣는 것이 곧 "세로 축 마스크"다 — 다가오는 속도도 세로로만 계산된다.</para>
    /// </summary>
    public static class VerticalBounce
    {
        /// <inheritdoc cref="ContactImpulse.RestingSpeed"/>
        public const float RestingSpeed = ContactImpulse.RestingSpeed;

        /// <summary>
        /// 충돌 후 self의 세로 속도.
        /// <paramref name="normalY"/>는 self를 상대 밖으로 밀어내는 방향의 세로 성분(-1~1)이다.
        /// </summary>
        public static float ResolveVy(float vySelf, float vyOther, float normalY, float restitution)
        {
            return ContactImpulse.Resolve(
                new Vector3(0f, vySelf, 0f),
                new Vector3(0f, vyOther, 0f),
                new Vector3(0f, normalY, 0f),
                restitution).y;
        }
    }
}
