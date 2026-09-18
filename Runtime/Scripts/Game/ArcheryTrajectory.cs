using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 화살의 궤적. 상태가 없는 순수 계산이라 클·서·뷰가 같은 식에 같은 시각을 넣으면 같은 답을 얻는다.
    /// </summary>
    public static class ArcheryTrajectory
    {
        /// <summary>
        /// 화살에 걸리는 중력. 지구 중력(9.81)을 그대로 쓴다 — 활쏘기 모드에는 떨어지는
        /// 캐릭터가 없어 걷기 모드의 중력(약 19.6)과 맞출 이유가 없다. 활은 활답게 날아가면 된다.
        /// </summary>
        public const float Gravity = 9.81f;

        /// <summary>이 시간이 지난 화살은 목록에서 지운다. 화면 밖으로 나간 뒤에도 들고 있을 이유가 없다.</summary>
        public const float LifetimeSeconds = 3f;

        /// <summary>좌우(y축 회전)·위아래 각도를 방향 벡터로. 위아래 각도가 양수면 위를 본다.</summary>
        public static Vector3 DirectionFrom(float yawDegrees, float pitchDegrees)
        {
            float yaw = yawDegrees * Mathf.Deg2Rad;
            float pitch = pitchDegrees * Mathf.Deg2Rad;
            float horizontal = Mathf.Cos(pitch);
            return new Vector3(
                Mathf.Sin(yaw) * horizontal,
                Mathf.Sin(pitch),
                Mathf.Cos(yaw) * horizontal);
        }

        /// <summary>
        /// 수평으로 <paramref name="distance"/>m, 화살이 떠나는 높이보다
        /// <paramref name="heightOffset"/>m 위(음수면 아래)에 있는 과녁을 <paramref name="speed"/>로
        /// 맞히려면 <b>수평에서 몇 도를 올려 쏴야 하는가</b>. 닿을 수 없으면 NaN.
        ///
        /// <para>포물선은 같은 자리를 두 각도로 맞힐 수 있다(낮게 쏘거나 높이 띄우거나).
        /// 여기서는 <b>낮은 쪽</b>을 준다 — 빨리 도착하고 바람·오차에 덜 휘둘린다.</para>
        ///
        /// <para>활 조준기(사이트)의 핀이 이 값으로 놓인다. 화면이 착탄점을 그려 주는 것과는
        /// 다르다 — 이건 <b>거리별 기준선</b>일 뿐이라, 어느 핀을 쓸지와 그 핀을 과녁에 붙들고
        /// 있는 일은 여전히 쏘는 사람 몫이다.</para>
        /// </summary>
        public static float LaunchPitchDegrees(float distance, float heightOffset, float speed)
        {
            if (distance <= 0f || speed <= 0f)
            {
                return float.NaN;
            }

            //  v⁴ − g(g·x² + 2·y·v²) 가 음수면 그 속도로는 애초에 닿지 않는다.
            float v2 = speed * speed;
            float discriminant = v2 * v2 - Gravity * (Gravity * distance * distance + 2f * heightOffset * v2);
            if (discriminant < 0f)
            {
                return float.NaN;
            }

            float root = Mathf.Sqrt(discriminant);
            return Mathf.Atan((v2 - root) / (Gravity * distance)) * Mathf.Rad2Deg;
        }

        public static Vector3 PositionAt(in ArcheryShot shot, float secondsSinceFire)
        {
            float t = secondsSinceFire;
            return shot.Origin
                 + shot.Velocity * t
                 + new Vector3(0f, -0.5f * Gravity * t * t, 0f);
        }

        public static Vector3 VelocityAt(in ArcheryShot shot, float secondsSinceFire)
        {
            return shot.Velocity + new Vector3(0f, -Gravity * secondsSinceFire, 0f);
        }
    }
}
