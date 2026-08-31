namespace LOP
{
    /// <summary>
    /// 한 자세로 주어진 높이만큼 떨어지는 동안 <b>옆으로 갈 수 있는 최대 거리</b>를 낸다.
    /// 코스의 구멍과 구멍 사이가 이 값보다 멀면 그 코스는 통과할 수 없다(스펙 §7.4 코스 검사).
    ///
    /// 수평 속도 0에서 출발한다고 보므로 실제보다 짧게 나온다 — 앞 구간의 속도를 물고 오기 때문이다.
    /// 일부러 그렇게 뒀다: 이 함수가 "간다"고 하면 진짜로 간다.
    /// </summary>
    public static class SkydiveReach
    {
        public static float MaxHorizontal(float fallDistance, float fallSpeed,
                                          float moveSpeed, float turnAccel)
        {
            if (fallSpeed <= 0f || turnAccel <= 0f || moveSpeed <= 0f || fallDistance <= 0f)
            {
                return 0f;
            }

            float fallTime = fallDistance / fallSpeed;
            float timeToTopSpeed = moveSpeed / turnAccel;

            if (fallTime <= timeToTopSpeed)
            {
                return 0.5f * turnAccel * fallTime * fallTime;   // 아직 가속 중
            }

            float accelDistance = 0.5f * moveSpeed * timeToTopSpeed;
            return accelDistance + moveSpeed * (fallTime - timeToTopSpeed);
        }
    }
}
