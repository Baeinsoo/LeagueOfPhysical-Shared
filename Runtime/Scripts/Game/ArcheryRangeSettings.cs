using System.Collections.Generic;

namespace LOP
{
    /// <summary>과녁이 설 자리 하나에 대한 값. 자리는 씬이, 이 숫자들은 마스터데이터가 준다.</summary>
    public readonly struct ArcheryRangeStand
    {
        /// <summary>레인 안에서 몇 번째 자리인가. 씬의 <see cref="ArcheryLane.Stands"/> 차례와 짝이다.</summary>
        public readonly int StandIndex;

        /// <summary>사대에서 이 자리까지의 거리(m). <b>씬이 진짜 자리를 갖고 있고 이 값은 그 선언</b>이다 —
        /// 둘이 어긋나면 서버가 판 시작에 잡는다(배포 데이터 검사가 이 값으로 사거리를 판단하기 때문).</summary>
        public readonly float DistanceM;

        /// <summary>이 자리의 과녁이 서 있는 시간(틱).</summary>
        public readonly int ExposureTicks;

        /// <summary>이 자리의 과녁이 좌우로 흔드는 폭(m). 0이면 안 움직인다.</summary>
        public readonly float LateralSpan;

        /// <summary>왕복 한 번이 걸리는 시간(초). <see cref="LateralSpan"/>이 0이면 안 쓰인다.</summary>
        public readonly float LateralPeriod;

        public ArcheryRangeStand(int standIndex, float distanceM, int exposureTicks,
                                 float lateralSpan, float lateralPeriod)
        {
            StandIndex = standIndex;
            DistanceM = distanceM;
            ExposureTicks = exposureTicks;
            LateralSpan = lateralSpan;
            LateralPeriod = lateralPeriod;
        }
    }

    /// <summary>사거리 코스에만 쓰이는 값들. 웨이브 맵에서는 <see cref="None"/>이다.</summary>
    public sealed class ArcheryRangeSettings
    {
        /// <summary>사거리 과녁이 쓰는 종류(판 하나). 코스가 난수로 고르지 않는다 — 늘 이것이다.</summary>
        public ArcheryTargetKind Kind { get; }

        /// <summary>자리 목록. <see cref="ArcheryRangeStand.StandIndex"/> 오름차순으로 들어온다.</summary>
        public IReadOnlyList<ArcheryRangeStand> Stands { get; }

        /// <summary>과녁이 사라진 뒤 다음 과녁이 설 때까지의 틈(틱).</summary>
        public int StepGapTicks { get; }

        /// <summary>웨이브 맵이 드는 빈 값. 널 검사를 부르는 쪽마다 하지 않으려는 것이다.</summary>
        public static readonly ArcheryRangeSettings None =
            new ArcheryRangeSettings(default, new ArcheryRangeStand[0], 0);

        public ArcheryRangeSettings(ArcheryTargetKind kind, IReadOnlyList<ArcheryRangeStand> stands, int stepGapTicks)
        {
            Kind = kind;
            Stands = stands ?? new ArcheryRangeStand[0];
            StepGapTicks = stepGapTicks;
        }
    }
}
