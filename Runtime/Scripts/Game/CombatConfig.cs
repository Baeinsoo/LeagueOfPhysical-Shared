namespace LOP
{
    /// <summary>전역 전투 튜닝(회피/크리 확률·배수 clamp). MasterData TbCombatConfig에서 side provider가 채워
    /// LOPCombatSystem에 주입. 순수 데이터 — Shared는 MasterData 패키지 비참조라 plain struct로 전달받는다.</summary>
    public readonly struct CombatConfig
    {
        public readonly float DodgeChanceMin;
        public readonly float DodgeChanceMax;
        public readonly float CritChanceMin;
        public readonly float CritChanceMax;
        public readonly float CritMultMin;
        public readonly float CritMultMax;

        public CombatConfig(float dodgeChanceMin, float dodgeChanceMax,
                            float critChanceMin, float critChanceMax,
                            float critMultMin, float critMultMax)
        {
            DodgeChanceMin = dodgeChanceMin;
            DodgeChanceMax = dodgeChanceMax;
            CritChanceMin = critChanceMin;
            CritChanceMax = critChanceMax;
            CritMultMin = critMultMin;
            CritMultMax = critMultMax;
        }
    }
}
