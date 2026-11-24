using System;
using System.Numerics;

namespace LOP.TriGrab
{
    public static class TriGrabMath
    {
        const float Deg2Rad = (float)(Math.PI / 180.0);
        const float Rad2Deg = (float)(180.0 / Math.PI);

        /// <summary>yaw(도) → 바닥 방향. 넉백과 같은 규약 (sin, cos).</summary>
        public static Vector2 DirFromYaw(float yawDegrees)
        {
            float r = yawDegrees * Deg2Rad;
            return new Vector2((float)Math.Sin(r), (float)Math.Cos(r));
        }

        public static float YawFromDir(Vector2 d) => (float)Math.Atan2(d.X, d.Y) * Rad2Deg;
    }
}
