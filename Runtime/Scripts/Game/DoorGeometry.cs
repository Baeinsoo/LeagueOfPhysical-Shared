using System;
using System.Numerics;

namespace LOP
{
    /// <summary>
    /// 문의 자세를 틱에서 뽑는다. <b>판정과 그림이 이 한 식을 같이 쓴다</b> — 판정은 정수 틱,
    /// 그림은 소수 틱(프레임 사이)을 넣는다. 식이 둘이면 보이는 자세와 맞는 자세가 갈린다.
    /// </summary>
    public static class DoorGeometry
    {
        /// <summary>0 = 완전히 닫힘, 1 = 완전히 열림.</summary>
        public static float Openness(in Door door, double tick)
        {
            if (door.Period <= 0)
            {
                return 1f;   // 주기가 없으면 늘 열린 구멍이다
            }

            double raw = tick + door.Phase;
            double t = raw - Math.Floor(raw / door.Period) * door.Period;

            double closing = door.OpenTicks + door.MoveTicks;
            double opening = door.Period - door.MoveTicks;

            if (t < door.OpenTicks)
            {
                return 1f;
            }
            if (t < closing)
            {
                return (float)(1.0 - (t - door.OpenTicks) / door.MoveTicks);
            }
            if (t < opening)
            {
                return 0f;
            }
            return (float)((t - opening) / door.MoveTicks);
        }

        /// <summary>
        /// 패널 <paramref name="index"/>(0 또는 1)의 중심. 닫히면 각각 구멍의 반쪽을 덮고,
        /// 열리면 구멍 밖으로 완전히 물러난다.
        /// </summary>
        public static Vector3 PanelCenter(in Door door, int index, float openness)
        {
            float sign = index == 0 ? -1f : 1f;
            float half = door.HalfWidth * 0.5f;
            float offset = half + door.HalfWidth * openness;
            float c = MathF.Cos(door.AxisAngle);
            float s = MathF.Sin(door.AxisAngle);
            return door.Center + new Vector3(c, 0f, s) * (sign * offset);
        }
    }
}
