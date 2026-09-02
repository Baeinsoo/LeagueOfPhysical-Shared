namespace LOP
{
    /// <summary>
    /// 한 틱 분량의 플레이어 조종 커맨드(순수 데이터, 와이어 독립). Quake usercmd / Source CUserCmd 대응.
    /// 클라·서버 모두 이 커맨드를 <see cref="InputBuffer"/>에 채워 넣고, 이동 시스템이 이번 틱 커맨드를 꺼내 쓴다.
    /// proto(와이어)는 송수신 어댑터에서 이 타입으로 변환된다 — 도메인은 이 순수 데이터만 다룬다.
    /// </summary>
    public class InputCommand
    {
        public long SequenceNumber { get; set; }
        public float Horizontal { get; set; }
        public float Vertical { get; set; }
        public bool Jump { get; set; }
        public int AbilityId { get; set; }

        /// <summary>자세 축. 0이면 대자, 1이면 다이브. 사이는 연속이다.</summary>
        public float Posture { get; set; }

        /// <summary>패러세일을 펴고 있나. 자세 축과 무관한 별개 도구다.</summary>
        public bool Glide { get; set; }

        /// <summary>
        /// 자세 슬라이더를 잡고 있나. 자세 값만으로는 <b>"안 잡음"과 "잡았는데 대자"</b>가
        /// 똑같이 0이라 구분할 수 없어서 따로 싣는다 — 그 구분이 스카이다이빙 진입 조건이다.
        /// </summary>
        public bool Posing { get; set; }

        /// <summary>대시 버튼. 누른 틱에만 참인 이산 액션이다(<see cref="Jump"/>와 같은 짝).</summary>
        public bool Dash { get; set; }

        // 진단 로그가 커맨드를 한 줄로 찍을 때 쓴다. 무엇이 실렸는지는 커맨드 자신이 안다 —
        // 읽는 쪽(넷코드 로그)이 필드를 하나씩 나열하면 그쪽이 게임 내용을 알게 된다.
        public override string ToString()
            => $"h={Horizontal:F2} v={Vertical:F2} jump={Jump} ability={AbilityId} posture={Posture:F2} glide={Glide} posing={Posing} dash={Dash}";
    }
}
