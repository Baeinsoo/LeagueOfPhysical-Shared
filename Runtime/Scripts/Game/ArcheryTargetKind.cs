using System.Collections.Generic;

namespace LOP
{
    /// <summary>과녁 한 종류. 작을수록 맞히기 어렵고 그만큼 비싸다.</summary>
    public readonly struct ArcheryTargetKind
    {
        /// <summary>맞았다고 칠 반경(m).</summary>
        public readonly float Radius;

        /// <summary>띠가 없을 때 쓰는 대비값. 띠가 있으면 그쪽이 이긴다. 함정은 음수다.</summary>
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

        /// <summary>마스터데이터 id. 화면이 생김새를 고르는 데 쓴다.</summary>
        public readonly int Id;

        /// <summary>투석기 사냥: 이 종류가 뜨는 거리대(m, 사대에서 앞으로). 둘 다 0이면 투석기에 안 나온다.</summary>
        public readonly float DistanceMinM;
        public readonly float DistanceMaxM;

        /// <summary>투석기 사냥: 옆으로 나는 속도대(m/s).</summary>
        public readonly float DriftMinMps;
        public readonly float DriftMaxMps;

        /// <summary>맞히면 활을 못 드는 틱 수(쏘면 안 되는 것만). 0이면 없다.</summary>
        public readonly int StunTicks;

        public ArcheryTargetKind(float radius, int points, int weight, bool isTrap,
                                 ArcheryTargetShape shape, IReadOnlyList<ArcheryRingBand> bands,
                                 int id = 0, float distanceMinM = 0f, float distanceMaxM = 0f,
                                 float driftMinMps = 0f, float driftMaxMps = 0f, int stunTicks = 0)
        {
            Radius = radius;
            Points = points;
            Weight = weight;
            IsTrap = isTrap;
            Shape = shape;
            Bands = bands;
            Id = id;
            DistanceMinM = distanceMinM;
            DistanceMaxM = distanceMaxM;
            DriftMinMps = driftMinMps;
            DriftMaxMps = driftMaxMps;
            StunTicks = stunTicks;
        }
    }
}
