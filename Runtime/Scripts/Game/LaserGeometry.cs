using System;
using System.Numerics;

namespace LOP
{
    /// <summary>
    /// 틱을 넣으면 레이저의 자세가 나오는 식. 컨텍스트가 없는 순수 계산이라 static이다.
    ///
    /// <para>판정은 서버에서만 도므로(스펙 §4.6) <c>Cos</c>/<c>Sin</c>을 그대로 쓴다 — 클·서가
    /// 끝자리까지 같을 필요가 없다. 클라는 이 결과로 그리기만 한다.</para>
    /// </summary>
    public static class LaserGeometry
    {
        /// <summary>톱니를 접어 만든 삼각파. <c>[-half, +half]</c>를 주기 <c>4·half</c>로 왕복한다.</summary>
        public static float Fold(float x, float half)
        {
            if (half <= 0f)
            {
                return 0f;
            }
            float period = 4f * half;
            float m = x + half;
            m -= MathF.Floor(m / period) * period;
            return m <= 2f * half ? m - half : 3f * half - m;
        }

        /// <param name="t">틱. 정수가 아니어도 된다 — 한 틱 안을 훑을 때 소수로 들어온다.</param>
        public static float Angle(in Laser laser, float t)
        {
            float advance = laser.AngularSpeed * t;
            return laser.SweepHalfRange > 0f
                ? laser.StartAngle + Fold(advance, laser.SweepHalfRange)
                : laser.StartAngle + advance;
        }

        public static void SegmentAt(in Laser laser, float t, out Vector3 a, out Vector3 b)
        {
            float angle = Angle(laser, t);
            a = laser.Pivot;
            b = laser.Pivot + new Vector3(
                MathF.Cos(angle) * laser.Length, 0f, MathF.Sin(angle) * laser.Length);
        }

        /// <summary>
        /// 이 틱에 켜져 있나. 주기가 <b>정수 틱</b>이라 한 틱 안에서는 값이 변하지 않는다 —
        /// 그래서 판정은 틱 시작에 한 번만 보고 꺼져 있으면 통째로 건너뛸 수 있다.
        /// </summary>
        public static bool Lit(in Laser laser, long tick)
        {
            if (laser.Period <= 0 || laser.OnTicks >= laser.Period)
            {
                return true;
            }
            long m = (tick + laser.Phase) % laser.Period;
            if (m < 0)
            {
                m += laser.Period;
            }
            return m < laser.OnTicks;
        }
    }
}
