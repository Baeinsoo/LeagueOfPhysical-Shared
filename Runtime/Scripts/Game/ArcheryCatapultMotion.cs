namespace LOP
{
    /// <summary>
    /// 투석기 표적 한 종류의 움직임 값(2차 spec §2). 종류(<see cref="ArcheryTargetKind"/>)가 들고 있고,
    /// 생성기(<see cref="ArcheryCatapultGenerator"/>)가 범위 안에서 뽑아 표적(<see cref="ArcheryTarget"/>)에 옮긴다.
    /// <b>코드는 표적 이름을 모른다</b> — "물풍선은 출렁인다"는 데이터가 출렁 폭을 적어서 생긴다.
    /// 전부 0이면 1차처럼 옆으로만 난다.
    /// </summary>
    public readonly struct ArcheryCatapultMotion
    {
        /// <summary>시작 높이(m, 땅에서).</summary>
        public readonly float HeightMinM;
        public readonly float HeightMaxM;

        /// <summary>수명 동안 위로 그리는 호의 높이(m) — 수박의 "높은 포물선".</summary>
        public readonly float ArcMinM;
        public readonly float ArcMaxM;

        /// <summary>위로 계속 솟는 속도(m/s) — 폭죽 로켓.</summary>
        public readonly float ClimbMinMps;
        public readonly float ClimbMaxMps;

        /// <summary>위아래 출렁 폭(m, 가운데에서 한쪽). 0이면 안 출렁인다.</summary>
        public readonly float BobAmplitudeM;

        /// <summary>출렁 횟수(초당).</summary>
        public readonly float BobHz;

        /// <summary>빙글 도는 빠르기(초당 바퀴). 0보다 크면 판정 반지름이 회전각을 따른다(<see cref="ArcheryTargetMotion.RadiusAt"/>).</summary>
        public readonly float SpinHz;

        /// <summary>길이 머무는 가로 범위(m, 가운데에서 한쪽). 0이면 설정의 <see cref="ArcheryCatapultSettings.LateralHalfRangeM"/>.</summary>
        public readonly float LateralHalfM;

        public ArcheryCatapultMotion(float heightMinM = 0f, float heightMaxM = 0f, float arcMinM = 0f, float arcMaxM = 0f,
                                     float climbMinMps = 0f, float climbMaxMps = 0f,
                                     float bobAmplitudeM = 0f, float bobHz = 0f, float spinHz = 0f, float lateralHalfM = 0f)
        {
            HeightMinM = heightMinM;
            HeightMaxM = heightMaxM;
            ArcMinM = arcMinM;
            ArcMaxM = arcMaxM;
            ClimbMinMps = climbMinMps;
            ClimbMaxMps = climbMaxMps;
            BobAmplitudeM = bobAmplitudeM;
            BobHz = bobHz;
            SpinHz = spinHz;
            LateralHalfM = lateralHalfM;
        }
    }
}
