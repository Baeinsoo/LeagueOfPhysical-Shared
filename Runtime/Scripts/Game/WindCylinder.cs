using System.Numerics;

namespace LOP
{
    /// <summary>
    /// 바람이 부는 원기둥 하나. 맵의 <see cref="WindVolume"/> 마커가 만들어
    /// <see cref="WindField"/>에 넣는다.
    ///
    /// 세로로 세운 기류 기둥이든 넓적하게 눕힌 횡풍 구간이든 같은 모양이라 판정도 한 벌이다.
    /// </summary>
    public sealed class WindCylinder
    {
        public readonly Vector3 Center;
        public readonly float Radius;
        public readonly float Height;

        /// <summary>방향 × 세기 (m/s).</summary>
        public readonly Vector3 Wind;

        public WindCylinder(Vector3 center, float radius, float height, Vector3 wind)
        {
            Center = center;
            Radius = radius;
            Height = height;
            Wind = wind;
        }

        public bool Contains(Vector3 point)
        {
            float half = Height * 0.5f;
            float dy = point.Y - Center.Y;
            if (dy < -half || dy > half)
            {
                return false;
            }

            float dx = point.X - Center.X;
            float dz = point.Z - Center.Z;
            return dx * dx + dz * dz <= Radius * Radius;
        }
    }
}
