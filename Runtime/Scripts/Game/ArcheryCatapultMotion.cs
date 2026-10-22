namespace LOP
{
    /// <summary>
    /// 투석기 표적 한 종류의 움직임 값(2차 spec §2 + 3차 spec §1·§2). 종류(<see cref="ArcheryTargetKind"/>)가 들고 있고,
    /// 생성기(<see cref="ArcheryCatapultGenerator"/>)가 범위 안에서 뽑아 표적(<see cref="ArcheryTarget"/>)에 옮긴다.
    /// <b>코드는 표적 이름을 모른다</b> — "물풍선은 출렁인다"는 데이터가 출렁 폭을 적어서 생긴다.
    /// 전부 0이면 설정 범위 아무 데서나 나와 옆으로만 난다.
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

        /// <summary>
        /// <b>나오는</b> 가로 범위(m, 가운데에서 한쪽). 0이면 설정의 <see cref="ArcheryCatapultSettings.LateralHalfRangeM"/>.
        /// 3차부터 길이 머무는 범위가 아니라 출발점의 범위다 — 빨리 나는 표적은 범위 밖으로 날아가도 된다(3차 spec §1).
        /// <see cref="SpawnSideMaxM"/>가 있으면 안 읽힌다.
        /// </summary>
        public readonly float LateralHalfM;

        /// <summary>
        /// 좌우 바깥 띠(m, 가운데에서 이만큼~저만큼)에서 나오는 종류 — 접시·종·피냐타(3차 spec §1). 0보다 크면 쪽은 무작위이고
        /// <b>늘 가운데 쪽으로</b> 간다(옆으로 나는 속도·그네 둘 다). 0이면 <see cref="LateralHalfM"/> 범위 아무 데서나, 방향도 무작위.
        /// </summary>
        public readonly float SpawnSideMinM;
        public readonly float SpawnSideMaxM;

        /// <summary>이 종류만의 수명(틱). 0이면 설정의 <see cref="ArcheryCatapultSettings.TargetLifeTicks"/> — 대박은 2초(100틱)로 짧다(3차 spec §2).</summary>
        public readonly int LifeTicks;

        /// <summary>수명 동안 한 번 건너가는 그네의 폭 A(m) — 피냐타. 가로 = A·(1 − cos(π·u/L)), 끝까지 2A를 건너간다.</summary>
        public readonly float SwingAmplitudeM;

        /// <summary>그네가 가운데를 지날 때 낮아지는 깊이 D(m) — 세로 = −D·sin²(π·u/L).</summary>
        public readonly float SwingDipM;

        public ArcheryCatapultMotion(float heightMinM = 0f, float heightMaxM = 0f, float arcMinM = 0f, float arcMaxM = 0f,
                                     float climbMinMps = 0f, float climbMaxMps = 0f,
                                     float bobAmplitudeM = 0f, float bobHz = 0f, float spinHz = 0f, float lateralHalfM = 0f,
                                     float spawnSideMinM = 0f, float spawnSideMaxM = 0f, int lifeTicks = 0,
                                     float swingAmplitudeM = 0f, float swingDipM = 0f)
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
            SpawnSideMinM = spawnSideMinM;
            SpawnSideMaxM = spawnSideMaxM;
            LifeTicks = lifeTicks;
            SwingAmplitudeM = swingAmplitudeM;
            SwingDipM = swingDipM;
        }
    }
}
