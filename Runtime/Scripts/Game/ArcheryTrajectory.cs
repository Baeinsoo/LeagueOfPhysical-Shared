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

        public static Vector3 PositionAt(in ArcheryShot shot, float secondsSinceFire)
        {
            float t = secondsSinceFire;
            return shot.Origin
                 + shot.Velocity * t
                 + (shot.Wind + new Vector3(0f, -Gravity, 0f)) * (0.5f * t * t);
        }

        public static Vector3 VelocityAt(in ArcheryShot shot, float secondsSinceFire)
        {
            return shot.Velocity + (shot.Wind + new Vector3(0f, -Gravity, 0f)) * secondsSinceFire;
        }
    }
}
