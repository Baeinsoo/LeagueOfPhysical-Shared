using System.Collections.Generic;

namespace LOP
{
    /// <summary>과녁 한 종류. 작을수록 맞히기 어렵고 그만큼 비싸다.</summary>
    public readonly struct ArcheryTargetKind
    {
        /// <summary>맞았다고 칠 반경(m).</summary>
        public readonly float Radius;

        /// <summary>맞히면 점수가 이만큼 움직인다. 함정은 음수다.</summary>
        public readonly int Points;

        /// <summary>뽑힐 상대 비율. 합이 100일 필요는 없다 — 서로의 크기만 의미가 있다.</summary>
        public readonly int Weight;

        /// <summary>맞히면 안 되는 과녁인가. 함정끼리, 성한 것끼리 따로 뽑는다.</summary>
        public readonly bool IsTrap;

        /// <summary>공인가 판인가.</summary>
        public readonly ArcheryTargetShape Shape;

        /// <summary>
        /// 중심에서 바깥으로 가는 띠 목록. 비어 있으면 <see cref="Points"/>짜리 띠 하나로 친다 —
        /// 띠 데이터가 없던 시절의 과녁이 그대로 동작하게 하려는 것이다.
        /// </summary>
        public readonly IReadOnlyList<ArcheryRingBand> Bands;

        public ArcheryTargetKind(float radius, int points, int weight, bool isTrap,
                                 ArcheryTargetShape shape, IReadOnlyList<ArcheryRingBand> bands)
        {
            Radius = radius;
            Points = points;
            Weight = weight;
            IsTrap = isTrap;
            Shape = shape;
            Bands = bands;
        }
    }
}
