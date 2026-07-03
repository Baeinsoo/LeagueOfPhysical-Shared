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
    }
}
