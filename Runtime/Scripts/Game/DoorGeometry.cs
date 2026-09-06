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

        /// <summary>
        /// 이 틱에 <b>완전히 닫힌</b> 패널과 몸이 겹치나. 닫히는 중에는 벽일 뿐이라 false다 —
        /// 밀려날 기회를 다 준 뒤에 묻는다.
        ///
        /// <para>몸이 선 캡슐(위아래 끝의 x·z가 같다)이라 거리를 닫힌 식으로 낼 수 있다:
        /// 각 축의 초과분을 재서 합치면 상자까지의 최단거리다.</para>
        /// </summary>
        public static bool Crushes(in Door door, long tick,
                                   Vector3 bottom, Vector3 top, float radius)
        {
            if (Openness(door, tick) > 0f)
            {
                return false;
            }

            //  문이 미끄러지는 방향을 x축으로 두고 본다 — 상자가 축에 정렬돼 계산이 단순해진다.
            float c = MathF.Cos(-door.AxisAngle);
            float s = MathF.Sin(-door.AxisAngle);
            Vector3 d = bottom - door.Center;
            float localX = d.X * c - d.Z * s;
            float localZ = d.X * s + d.Z * c;

            float halfPanel = door.HalfWidth * 0.5f;
            float halfThick = door.Thickness * 0.5f;
            float panelY = door.Center.Y;

            for (int index = 0; index < 2; index++)
            {
                float sign = index == 0 ? -1f : 1f;
                float panelX = sign * halfPanel;

                float dx = MathF.Max(MathF.Abs(localX - panelX) - halfPanel, 0f);
                float dz = MathF.Max(MathF.Abs(localZ) - door.HalfDepth, 0f);
                float dy = MathF.Max(MathF.Max(panelY - halfThick - top.Y,
                                               bottom.Y - (panelY + halfThick)), 0f);

                if (dx * dx + dy * dy + dz * dz <= radius * radius)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
