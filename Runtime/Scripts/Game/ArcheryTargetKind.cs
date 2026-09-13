namespace LOP
{
    /// <summary>과녁 한 종류. 작을수록 맞히기 어렵고 그만큼 비싸다.</summary>
    public readonly struct ArcheryTargetKind
    {
        /// <summary>맞았다고 칠 반경(m).</summary>
        public readonly float Radius;

        /// <summary>맞히면 받는 점수.</summary>
        public readonly int Points;

        /// <summary>뽑힐 상대 비율. 합이 100일 필요는 없다 — 서로의 크기만 의미가 있다.</summary>
        public readonly int Weight;

        public ArcheryTargetKind(float radius, int points, int weight)
        {
            Radius = radius;
            Points = points;
            Weight = weight;
        }
    }
}
