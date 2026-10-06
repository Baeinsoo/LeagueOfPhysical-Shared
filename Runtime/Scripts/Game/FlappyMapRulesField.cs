namespace LOP
{
    /// <summary>
    /// 맵이 추격자·수동 대시를 켜는지. 씬 마커(<see cref="FlappyMapRules"/>)가 맵 로드 때
    /// 스스로 값을 넣고, 사라질 때 거둔다(<see cref="FlappyBoostPadField"/>와 같은 통로).
    ///
    /// <para><b>기본은 둘 다 꺼짐이다.</b> 마커가 없는 맵(지금의 라이브 맵 포함)에서는 추격자가
    /// 아무도 잡지 않고 대시는 부스트 패드로만 받는다 — 룰이 꺼져 있다고 보지, 터지지 않는다.</para>
    /// </summary>
    public class FlappyMapRulesField
    {
        public bool Chaser { get; private set; }

        public bool ManualDash { get; private set; }

        public void Set(bool chaser, bool manualDash)
        {
            Chaser = chaser;
            ManualDash = manualDash;
        }

        /// <summary>맵을 다시 로드하면 옛 마커가 사라진다 — 그때 거둬 기본값(둘 다 꺼짐)으로 되돌린다.</summary>
        public void Clear()
        {
            Chaser = false;
            ManualDash = false;
        }
    }
}
