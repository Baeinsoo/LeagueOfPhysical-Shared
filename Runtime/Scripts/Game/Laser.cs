using System.Numerics;

namespace LOP
{
    /// <summary>
    /// 레이저 하나의 설정. <b>상태가 없다</b> — 틱을 넣으면 자세가 나오는 식의 계수일 뿐이라
    /// 스냅샷에 실을 것도, 롤백에서 되돌릴 것도 없다.
    ///
    /// <para>빔은 <see cref="Pivot"/>에서 한쪽으로만 뻗은 선분(시계바늘)이고, Y축을 중심으로
    /// 수평면에서 돈다. 통로를 가로지르는 빔은 Pivot을 벽 쪽에 둬서 만든다.</para>
    /// </summary>
    public readonly struct Laser
    {
        public readonly Vector3 Pivot;
        public readonly float Length;
        /// <summary>빔의 굵기(반지름). 캐릭터 반지름과 더해 허용 거리를 만든다.</summary>
        public readonly float Radius;
        public readonly float StartAngle;
        /// <summary>rad / 틱. 0이면 고정 빔이다.</summary>
        public readonly float AngularSpeed;
        /// <summary>0보다 크면 전회전 대신 이 폭만큼 왕복한다.</summary>
        public readonly float SweepHalfRange;
        /// <summary>점멸 주기(틱). 0 이하면 늘 켜져 있다.</summary>
        public readonly int Period;
        public readonly int OnTicks;
        public readonly int Phase;

        public Laser(Vector3 pivot, float length, float radius,
                     float startAngle, float angularSpeed, float sweepHalfRange,
                     int period, int onTicks, int phase)
        {
            Pivot = pivot;
            Length = length;
            Radius = radius;
            StartAngle = startAngle;
            AngularSpeed = angularSpeed;
            SweepHalfRange = sweepHalfRange;
            Period = period;
            OnTicks = onTicks;
            Phase = phase;
        }
    }
}
