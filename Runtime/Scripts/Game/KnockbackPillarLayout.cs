namespace LOP
{
    /// <summary>기둥 설정 한 벌: 몇 개를, 얼마나 먼 원 위에, 어떤 크기로 세울지.</summary>
    public sealed class KnockbackPillarLayout
    {
        public int Count { get; }
        public float RingRadius { get; }
        public float Radius { get; }
        public float Height { get; }

        //  설정을 안 준 옛 호출부가 "기둥 없음"으로 읽도록 하는 기본값.
        public static readonly KnockbackPillarLayout None = new KnockbackPillarLayout(0, 0f, 0f, 0f);

        public KnockbackPillarLayout(int count, float ringRadius, float radius, float height)
        {
            Count = count;
            RingRadius = ringRadius;
            Radius = radius;
            Height = height;
        }
    }
}
