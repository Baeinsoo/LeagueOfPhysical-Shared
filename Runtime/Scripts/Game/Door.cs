namespace LOP
{
    /// <summary>
    /// 여닫이 문 하나. <b>상태가 없다</b> — 틱만 넣으면 자세가 나오므로 스냅샷에 실을 것도,
    /// 되감기에서 되돌릴 것도 없다(<see cref="Laser"/>와 같은 성질).
    /// </summary>
    public readonly struct Door
    {
        /// <summary>구멍 중심. 닫혔을 때 두 패널이 맞물리는 자리다.</summary>
        public readonly System.Numerics.Vector3 Center;

        /// <summary>덮는 폭의 절반(=구멍 반폭). 패널 하나는 이 값의 절반 길이다.</summary>
        public readonly float HalfWidth;

        /// <summary>미끄러지는 방향과 직교하는 쪽 절반.</summary>
        public readonly float HalfDepth;

        /// <summary>패널 두께(세로).</summary>
        public readonly float Thickness;

        /// <summary>패널이 미끄러지는 방향(XZ 평면 각, 라디안).</summary>
        public readonly float AxisAngle;

        public readonly int Period;
        public readonly int OpenTicks;

        /// <summary>여닫는 데 걸리는 틱. 이 움직임 자체가 예고다.</summary>
        public readonly int MoveTicks;

        public readonly int Phase;

        public Door(System.Numerics.Vector3 center, float halfWidth, float halfDepth,
                    float thickness, float axisAngle,
                    int period, int openTicks, int moveTicks, int phase)
        {
            Center = center;
            HalfWidth = halfWidth;
            HalfDepth = halfDepth;
            Thickness = thickness;
            AxisAngle = axisAngle;
            Period = period;
            OpenTicks = openTicks;
            MoveTicks = moveTicks;
            Phase = phase;
        }
    }
}
