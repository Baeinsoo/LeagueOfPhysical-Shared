namespace LOP
{
    /// <summary>마지막 장애물 결과 — 판정엔 안 쓰고 그림·소리가 본다(되감기가 같이 되돌린다). 와이어 값이라 번호를 바꾸지 않는다.</summary>
    public enum CardRushObstacleEvent { None = 0, Smashed = 1, Whiff = 2, Lifted = 3, Blocked = 4, Tumbled = 5 }

    /// <summary>마지막 구역 결과 — 피함·버팀·으악. 와이어 값이라 번호를 바꾸지 않는다.</summary>
    public enum CardRushZoneEvent { None = 0, Dodged = 1, Held = 2, Hit = 3 }
}
