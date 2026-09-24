using UnityEngine;

namespace LOP
{
    public static class ArcheryFaceCoords
    {
        /// <summary>
        /// 꽂힌 자리를 <b>과녁 면 위의 좌표</b>로 바꾼다 — 사수가 보는 기준으로 x는 오른쪽 y는 위,
        /// <paramref name="radius"/>로 나눈 값이다(1을 넣으면 미터).
        /// <para><paramref name="facing"/>은 과녁이 바라보는 쪽, 즉 <b>사수를 향한</b> 방향이다.</para>
        /// </summary>
        public static Vector2 ToFaceOffset(Vector3 offsetFromTarget, Vector3 facing, float radius)
        {
            if (radius <= 0f)
            {
                return Vector2.zero;
            }

            Vector3 shooterForward = -facing.normalized;
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(up, shooterForward).normalized;
            if (right.sqrMagnitude < 0.5f)
            {
                return Vector2.zero;   // 과녁이 바로 위나 아래를 보는 축퇴 — 이 게임엔 없다
            }

            return new Vector2(Vector3.Dot(offsetFromTarget, right),
                               Vector3.Dot(offsetFromTarget, up)) / radius;
        }
    }
}
