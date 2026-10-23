namespace LOP
{
    /// <summary>
    /// 맵이 추격자·수동 대시·알갱이 충전을 켜는지. 씬 마커(<see cref="FlappyMapRules"/>)가 맵 로드 때
    /// 스스로 값을 넣고, 사라질 때 거둔다(<see cref="FlappyBoostPadField"/>와 같은 통로).
    ///
    /// <para><b>기본은 전부 꺼짐이다.</b> 마커가 없는 맵(지금의 라이브 맵 포함)에서는 추격자가
    /// 아무도 잡지 않고 대시는 부스트 패드로만 받는다 — 룰이 꺼져 있다고 보지, 터지지 않는다.</para>
    /// </summary>
    public class FlappyMapRulesField
    {
        /// <summary>알갱이 하나가 채우는 게이지(칸). 32알 = 1칸 — 웹 시제품에서 정한 값.</summary>
        public const float DefaultMoteValue = 1f / 32f;

        /// <summary>몸 표면에서 이만큼 안의 알갱이를 먹는다(m). 웹 시제품에서 정한 값.</summary>
        public const float DefaultMoteReach = 0.5f;

        public bool Chaser { get; private set; }

        public bool ManualDash { get; private set; }

        /// <summary>
        /// 대시 게이지를 다이브 대신 빛 알갱이(<see cref="FlappyMoteField"/>)로 채우나.
        /// 켜지면 다이브 충전은 쉬고, 출발 게이지는 0, 박으면 게이지를 절반 잃는다.
        /// </summary>
        public bool MoteCharge { get; private set; }

        public float MoteValue { get; private set; } = DefaultMoteValue;

        public float MoteReach { get; private set; } = DefaultMoteReach;

        public void Set(bool chaser, bool manualDash, bool moteCharge = false,
                        float moteValue = DefaultMoteValue, float moteReach = DefaultMoteReach)
        {
            Chaser = chaser;
            ManualDash = manualDash;
            MoteCharge = moteCharge;
            MoteValue = moteValue;
            MoteReach = moteReach;
        }

        /// <summary>맵을 다시 로드하면 옛 마커가 사라진다 — 그때 거둬 기본값(전부 꺼짐)으로 되돌린다.</summary>
        public void Clear()
        {
            Set(false, false);
        }
    }
}
