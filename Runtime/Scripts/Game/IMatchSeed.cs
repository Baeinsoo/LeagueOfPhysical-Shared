namespace LOP
{
    /// <summary>매치당 결정론 RNG 씨앗을 노출하는 얇은 인터페이스. 양쪽 MatchSeed(서버=생성, 클라=수신 보관)가
    /// 구현해, 공유 전투 핸들러가 사이드 무지로 씨앗을 읽는다.</summary>
    public interface IMatchSeed
    {
        ulong Value { get; }
    }
}
