using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 과녁이 솟았다 떨어지는 길. 상태가 없는 순수 계산이라 클·서·뷰가 같은 시각을 넣으면 같은 답을
    /// 얻는다 — <b>그래서 과녁을 통신으로 보낼 필요가 없다</b>.
    ///
    /// <para>화살(<see cref="ArcheryTrajectory"/>)과 같은 모양이다: 출발점·초기속도·출발시각만 있으면
    /// 어느 시각의 위치든 나온다.</para>
    /// </summary>
    public static class ArcheryTargetMotion
    {
        /// <summary>
        /// 과녁에 걸리는 중력. <b>화살과 같은 값이다</b> — 중력은 세계의 성질이지 물체의 설정이
        /// 아니다. 다르게 두면 같은 화면에서 화살과 과녁이 서로 다른 속도로 떨어져,
        /// 화살로 과녁을 따라가는 이 게임에서는 바로 눈에 띈다.
        /// </summary>
        public const float Gravity = ArcheryTrajectory.Gravity;

        /// <summary>
        /// 그 높이까지 솟으려면 얼마로 출발해야 하나(m/s). 중력이 고정이라 높이 하나가 속도도
        /// 수명도 정한다 — 따로 적을 값이 없다.
        /// </summary>
        public static float RiseSpeedFor(float riseHeight)
        {
            if (riseHeight <= 0f)
            {
                return 0f;
            }
            //  정점 높이 H = v0²/(2g)를 v0에 대해 풀면 v0 = sqrt(2gH)다.
            return Mathf.Sqrt(2f * Gravity * riseHeight);
        }

        /// <summary>그 시각의 과녁 자리. 솟기 전에는 출발점에 가만히 있다.</summary>
        public static Vector3 PositionAt(in ArcheryTarget target, double tick, float tickInterval)
        {
            float t = (float)((tick - target.SpawnTick) * tickInterval);
            if (t <= 0f)
            {
                return target.Origin;
            }
            float y = target.RiseSpeed * t - 0.5f * Gravity * t * t;
            return new Vector3(target.Origin.x, target.Origin.y + y, target.Origin.z);
        }

        /// <summary>아직 공중에 있나. 솟기 전과 떨어진 뒤에는 거짓이다.</summary>
        public static bool IsAlive(in ArcheryTarget target, double tick, float tickInterval)
        {
            double elapsed = (tick - target.SpawnTick) * tickInterval;
            return elapsed >= 0d && elapsed <= target.LifetimeSeconds;
        }
    }
}
