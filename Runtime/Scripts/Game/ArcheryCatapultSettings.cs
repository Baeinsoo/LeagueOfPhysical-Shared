namespace LOP
{
    /// <summary>
    /// 투석기 사냥에만 쓰는 값들. 다른 코스에서는 <see cref="None"/>이다.
    /// 틱 값은 50Hz 기준이다(1초 = 50틱). <b>클·서가 같은 값을 들어야 한다</b> — 한 칸만 달라도 무리 시각이 갈린다.
    /// </summary>
    public sealed class ArcheryCatapultSettings
    {
        public int RoundCount { get; }
        /// <summary>라운드 소개(자리 안내) 틱.</summary>
        public int IntroTicks { get; }
        /// <summary>사냥 틱. 무리는 이 안에서만 뜬다.</summary>
        public int HuntTicks { get; }
        /// <summary>라운드 결과판 틱.</summary>
        public int ResultTicks { get; }
        /// <summary>무리 사이 간격(틱) — "준비"가 시작하는 시각끼리의 간격이다.</summary>
        public int VolleyPeriodTicks { get; }
        /// <summary>"준비" 신호에서 발사까지(틱).</summary>
        public int CueTicks { get; }
        /// <summary>사냥 끝의 이 틱 안에서는 새 "준비"를 시작하지 않는다.</summary>
        public int QuietTicks { get; }
        /// <summary>표적이 떠 있는 틱. 맞든 안 맞든 지나면 사라진다.</summary>
        public int TargetLifeTicks { get; }
        /// <summary>한 무리에 쏘면 안 되는 것이 낄 확률(0~1).</summary>
        public float TrapChance { get; }
        /// <summary>표적의 길이 머무는 가로 범위(m, 레인 중심에서 한쪽).</summary>
        public float LateralHalfRangeM { get; }

        /// <summary>한 무리에 대박(황금 종·피냐타)이 낄 확률(0~1). 금지물과 따로 뽑는다.</summary>
        public float JackpotChance { get; }

        public int RoundTicks => IntroTicks + HuntTicks + ResultTicks;

        /// <summary>
        /// 라운드당 무리 수 = "준비"를 시작할 수 있는 시각(0, 간격, 2·간격, …)이 사냥 − 조용한 틱보다 앞인 것의 개수.
        /// </summary>
        public int VolleysPerRound => VolleyPeriodTicks <= 0 || HuntTicks <= QuietTicks
            ? 0
            : (HuntTicks - QuietTicks + VolleyPeriodTicks - 1) / VolleyPeriodTicks;

        public int VolleyCount => RoundCount * VolleysPerRound;

        public long MatchTicks => (long)RoundCount * RoundTicks;

        public static readonly ArcheryCatapultSettings None =
            new ArcheryCatapultSettings(0, 0, 0, 0, 0, 0, 0, 0, 0f, 0f);

        public ArcheryCatapultSettings(int roundCount, int introTicks, int huntTicks, int resultTicks,
                                       int volleyPeriodTicks, int cueTicks, int quietTicks, int targetLifeTicks,
                                       float trapChance, float lateralHalfRangeM, float jackpotChance = 0f)
        {
            RoundCount = roundCount;
            IntroTicks = introTicks;
            HuntTicks = huntTicks;
            ResultTicks = resultTicks;
            VolleyPeriodTicks = volleyPeriodTicks;
            CueTicks = cueTicks;
            QuietTicks = quietTicks;
            TargetLifeTicks = targetLifeTicks;
            TrapChance = trapChance;
            LateralHalfRangeM = lateralHalfRangeM;
            JackpotChance = jackpotChance;
        }
    }
}
