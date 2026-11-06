namespace LOP
{
    /// <summary>선수 하나의 카드 러시 상태. 되감기 저장 단위 그대로라 값 복사로 저장·복원한다.</summary>
    public struct CardRushRunnerState
    {
        public int Step;            // 지금 잡으려는 칸(0부터). StepCount면 완주
        public int Attempt;         // 이 칸에서 틀린 횟수 — 시작 색 씨앗
        public long MoveStartTick;  // 마지막으로 맞힌 틱(완주 틱도 이것). 0 = 아직 없음
        public long StunEndTick;    // 마지막 틀림의 멈춤이 끝나는 틱. 0 = 아직 없음
        public long SpinStartTick;  // 카드가 돌기 시작한 틱(첫 탭). 0 = 대기(뒷면) — 아직 안 눌렀거나 방금 던졌다
        public int Streak;
        public int BestStreak;
        public int Misses;

        //  3차(장애물 레이스) 칸 — 위 옛 칸(Step·Attempt·MoveStartTick·Streak·BestStreak·Misses)은 3차 Task 7에서 지운다.
        //  SpinStartTick·StunEndTick은 3차도 같이 쓴다: 카드가 (다시) 도는 틱 = 마지막 사용 + 쿨다운 / 기절이 끝나는 틱.
        public int PosMm;               // 출발선에서 앞으로 간 거리(mm). 정수라 클·서가 같다
        public int UseCount;            // 카드를 쓴 횟수 — 다음 첫 장의 씨앗
        public long DashStartTick;      // 대시를 쓴 틱. 0 = 대시 중 아님
        public long BlockEndTick;       // 방패가 끝나는 틱. 0 = 없음
        public int ObstacleIndex;       // 지금 상대하는 장애물 번호(코스 장애물 줄)
        public long ObstacleTick;       // 바리케이드면 닿은 틱, 굴러오는 것이면 굴러오기 시작한 틱. 0 = 아직
        public int ZoneIndex;           // 지금 상대하는 구역 번호(코스 구역 줄)
        public long ZoneTick;           // 그 구역의 경고가 켜진 틱. 0 = 아직
        public int ObstacleEvent;       // 마지막으로 치운 장애물의 결과(CardRushObstacleEvent) — 그림·소리용. 헛방은 여기 안 적는다
        public long ObstacleEventTick;
        public int ZoneEvent;           // 마지막 구역 결과(CardRushZoneEvent)
        public long ZoneEventTick;
        public long WhiffTick;          // 마지막 헛방 틱. 0 = 없음 — 결과 칸과 따로 둬야 막음·올라감 직후 헛방이 그 결과를 안 지운다
        public long FinishTick;         // 완주 틱. 0 = 아직
        public int CleanStreak;         // 지금 연속 무사 통과
        public int BestCleanStreak;     // 이 판 최고(서버 통계·기록) — 와이어에 없다
        public int Hits;                // 이 판 맞은 수(서버 통계) — 와이어에 없다

        public bool IsFinished(int stepCount) => Step >= stepCount;
    }

    public sealed class CardRushRunner : GameFramework.World.Component
    {
        public CardRushRunnerState State;
        /// <summary>스폰 때 정해지는 레인 출발점. 안 바뀌어 되감기 대상이 아니다.</summary>
        public UnityEngine.Vector3 LaneOrigin;
        /// <summary>0~7. 첫 장 씨앗이라 클·서가 같은 식으로 구한다. 스폰 때 정해져 되감기 대상이 아니다.</summary>
        public int Lane;
        /// <summary>
        /// 맵 규칙보다 먼저 생긴 몸(비정상 순서)이라 레인을 아직 못 정했다 — 월드가 규칙을 받으면 <see cref="LaneOrigin"/>(받은 자리)로 다시 구한다.
        /// </summary>
        public bool LanePending;
    }
}
