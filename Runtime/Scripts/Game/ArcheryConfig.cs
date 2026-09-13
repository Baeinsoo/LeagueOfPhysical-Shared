using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// Archery 튜닝값. MasterData(<c>TbArcheryConfig</c>/<c>TbArcheryTarget</c>)에서 사이드 provider가
    /// 채워 시뮬에 넘긴다(Shared는 MasterData 패키지를 참조하지 않는다 — <see cref="SkydiveConfig"/>와 같은 짝).
    ///
    /// <para><b>클·서가 반드시 같은 값을 들어야 한다.</b> 하나라도 다르면 웨이브 계산이 갈려
    /// 서로 다른 과녁을 보게 되는데, 화면은 멀쩡해 보이고 점수만 이상해진다.</para>
    /// </summary>
    public sealed class ArcheryConfig
    {
        /// <summary>웨이브 하나가 서 있는 틱 수. 과녁 수명도 이것과 같다 — 다음 웨이브가 오면 사라진다.</summary>
        public int WavePeriodTicks { get; }

        /// <summary>한 웨이브의 최소 과녁 수.</summary>
        public int MinTargets { get; }

        /// <summary>한 웨이브의 최대 과녁 수(이 값 포함).</summary>
        public int MaxTargets { get; }

        /// <summary>과녁이 뜨는 원기둥의 반지름(m). 가운데는 원점이다.</summary>
        public float SpawnRadius { get; }

        /// <summary>과녁이 뜨는 가장 낮은 높이(m).</summary>
        public float SpawnMinY { get; }

        /// <summary>과녁이 뜨는 가장 높은 높이(m).</summary>
        public float SpawnMaxY { get; }

        /// <summary>같은 웨이브의 과녁 중심끼리 최소한 이만큼은 떨어뜨린다(m).</summary>
        public float MinSeparation { get; }

        /// <summary>뽑을 수 있는 과녁 종류. 비어 있으면 과녁이 안 뜬다.</summary>
        public IReadOnlyList<ArcheryTargetKind> Kinds { get; }

        public ArcheryConfig(int wavePeriodTicks, int minTargets, int maxTargets,
                             float spawnRadius, float spawnMinY, float spawnMaxY, float minSeparation,
                             IReadOnlyList<ArcheryTargetKind> kinds)
        {
            WavePeriodTicks = wavePeriodTicks;
            MinTargets = minTargets;
            MaxTargets = maxTargets;
            SpawnRadius = spawnRadius;
            SpawnMinY = spawnMinY;
            SpawnMaxY = spawnMaxY;
            MinSeparation = minSeparation;
            Kinds = kinds;
        }
    }
}
