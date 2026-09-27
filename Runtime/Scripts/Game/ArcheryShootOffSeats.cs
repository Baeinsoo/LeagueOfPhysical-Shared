namespace LOP
{
    /// <summary>
    /// 한 발 승부의 자리. 모두 한 과녁을 쏘되 각자 실제 자리에 나란히 선다(화면용 속임수 없음).
    /// 자리마다 과녁을 보는 각이 조금 다르므로(20m에서 3.2m 옆이면 9°, 25cm 멀다) 라운드마다 한 칸씩
    /// 돌아서, 판 전체에서 모두가 모든 자리를 같은 횟수만큼 쓴다.
    /// </summary>
    public static class ArcheryShootOffSeats
    {
        public const float SpacingMeters = 1.6f;

        /// <summary>명단 순번이 <paramref name="rosterIndex"/>인 사람이 <paramref name="seatRound"/> 라운드에 앉는 자리(왼쪽부터 0).</summary>
        public static int SlotOf(int rosterIndex, int seatRound, int count)
        {
            if (count <= 0)
            {
                return 0;
            }
            return ((rosterIndex + seatRound) % count + count) % count;
        }

        /// <summary>그 자리가 사대 가운데에서 사수 기준 오른쪽으로 몇 미터인가. 가운데를 중심으로 좌우 대칭이다.</summary>
        public static float LateralOffset(int slot, int count) => (slot - (count - 1) * 0.5f) * SpacingMeters;
    }
}
