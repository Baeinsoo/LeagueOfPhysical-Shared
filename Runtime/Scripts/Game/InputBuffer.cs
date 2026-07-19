using System.Collections.Generic;
using GameFramework.World;

namespace LOP
{
    /// <summary>
    /// 플레이어 엔티티가 가진 입력 커맨드 버퍼(순수 데이터 컴포넌트). 클라·서버 공용.
    /// 클라는 로컬 캡처를, 서버는 네트워크로 도착한 커맨드를 틱 키로 여기 채워 두고,
    /// 이동 시스템이 이번 틱에 확정된 <see cref="Current"/>를 읽어 이동을 계산한다.
    /// 채우기/소비/비우기 로직은 <see cref="InputBufferSystem"/>에 둔다(Anemic).
    /// DOTS command stream / Overwatch input buffer / Quake usercmd 큐 대응.
    /// </summary>
    public class InputBuffer : Component
    {
        /// <summary>틱 → 그 틱의 커맨드. 서버는 지터로 몰려온 여러 틱, 클라는 최근 redundancy 윈도우.</summary>
        public SortedDictionary<long, InputCommand> Commands { get; } = new SortedDictionary<long, InputCommand>();

        /// <summary>이번 틱에 소비하기로 확정된 커맨드. 이동 시스템이 읽는다. null = 이번 틱 입력 없음.</summary>
        public InputCommand Current { get; set; }

        /// <summary>중복 제거 기준 — 이 시퀀스 이하는 이미 처리됨. 초기 -1.</summary>
        public long LastProcessedSequence { get; set; } = -1;

        /// <summary>다음에 기대하는 시퀀스(재접속 seq 시드용).</summary>
        public long ExpectedNextSequence { get; set; }

        /// <summary>입력 도착 타이밍 통계(Phase 4 서버 피드백). 서버만 채운다 — 클라는 사용 안 함.</summary>
        public GameFramework.Netcode.InputTimingTracker TimingTracker { get; } = new GameFramework.Netcode.InputTimingTracker();
    }
}
