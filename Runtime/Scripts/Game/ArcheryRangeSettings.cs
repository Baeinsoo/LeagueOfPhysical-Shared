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

        /// <summary>
        /// 이 자리에 서는 과녁 면의 반지름(m). <b>0이면 과녁 종류에 적힌 값</b>을 쓴다.
        ///
        /// <para>거리마다 다른 이유는 실제 양궁과 같다 — 세계양궁연맹은 먼 거리(90/70/60m)에
        /// 122cm 과녁을, 가까운 거리(50/30m)에 80cm를, 실내 18m에 40cm를 쓴다. <b>거리가
        /// 짧으면 과녁을 줄여 체감 난이도를 맞추는 것</b>이다. 한 크기로 통일하면 가까운 자리는
        /// 거저 10점이고 먼 자리는 흔들림이 링을 통째로 잡아먹어, 양쪽 다 실력을 못 가린다
        /// (화면에서 10점 링이 12m 57px vs 90m 7.6px — 7.5배 차이였다).</para>
        /// </summary>
        public readonly float FaceRadiusM;

        //  faceRadiusM은 기본 0 = "과녁 종류에 적힌 값을 쓴다". 이 자리 크기를 신경 안 쓰는
        //  호출부(대부분의 시험)가 그대로 컴파일되고, 뜻도 그대로다.
        public ArcheryRangeStand(int standIndex, float distanceM, int exposureTicks,
                                 float lateralSpan, float lateralPeriod, float faceRadiusM = 0f)
        {
            StandIndex = standIndex;
            DistanceM = distanceM;
            ExposureTicks = exposureTicks;
            LateralSpan = lateralSpan;
            LateralPeriod = lateralPeriod;
            FaceRadiusM = faceRadiusM;
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

        /// <summary>
        /// 자리 하나마다 주어지는 화살 수. 전체 화살은 <c>자리 수 × 이 값</c>이다.
        ///
        /// <para>한 자리에 여러 발을 주는 이유는 이 게임의 실력이 <b>리드 추정</b>이기 때문이다 —
        /// 화살이 날아가는 동안 과녁이 움직이니 빈 공간을 겨눠야 하는데, 그건 *빗나간 걸 보고
        /// 고치면서* 는다. 한 자리에 한 발이면 표본이 하나뿐이라 고칠 기회가 없다.</para>
        ///
        /// <para>화살은 <b>전체 주머니</b>라(<c>ArcheryQuiver.Remaining</c>) 어려운 자리에 더 쓰고
        /// 쉬운 자리에 아낄 수 있다 — 배분도 선택이다.</para>
        /// </summary>
        public int ArrowsPerStand { get; }

        /// <summary>웨이브 맵이 드는 빈 값. 널 검사를 부르는 쪽마다 하지 않으려는 것이다.</summary>
        public static readonly ArcheryRangeSettings None =
            new ArcheryRangeSettings(default, new ArcheryRangeStand[0], 0);

        //  arrowsPerStand는 기본 1 = "자리마다 한 발". 이 값을 신경 안 쓰는 호출부(대부분의
        //  시험)가 그대로 컴파일되고, 뜻도 옛 동작 그대로다.
        public ArcheryRangeSettings(ArcheryTargetKind kind, IReadOnlyList<ArcheryRangeStand> stands,
                                    int stepGapTicks, int arrowsPerStand = 1)
        {
            Kind = kind;
            Stands = stands ?? new ArcheryRangeStand[0];
            StepGapTicks = stepGapTicks;
            ArrowsPerStand = arrowsPerStand;
        }
    }
}
