namespace LOP
{
    /// <summary>
    /// 어빌리티 설정(불변 디자인 데이터) — 얇은 컨테이너: 프레임 타이밍 + 코스트/쿨다운 + <see cref="Effects"/> 리스트.
    /// "어빌리티가 뭘 하는지"는 effect 리스트의 조합으로 표현한다(타입별 서브클래스 없음 — 한 클래스, 다른 데이터).
    /// Luban TbAbility로 외부화되며, 코어는 이 구조체만 소비한다(Luban 생성 타입은 side provider가 매핑).
    /// </summary>
    public readonly struct AbilityData
    {
        public readonly int AbilityId;
        public readonly long CooldownTicks;
        public readonly int MpCost;
        public readonly long StartupTicks;      // 윈드업(=캐스트). 0이면 발동 틱에 곧장 Active.
        public readonly long ActiveTicks;       // 판정 창(effect가 발화하는 구간) 길이.
        public readonly long RecoveryTicks;     // 후딜(busy 잠금).
        public readonly AbilityEffect[] Effects;
        public readonly float StartupMoveScale;
        public readonly float ActiveMoveScale;
        public readonly float RecoveryMoveScale;
        public readonly bool BlockJump;

        public AbilityData(int abilityId, long cooldownTicks, int mpCost,
                           long startupTicks, long activeTicks, long recoveryTicks,
                           AbilityEffect[] effects,
                           float startupMoveScale = 1f, float activeMoveScale = 1f,
                           float recoveryMoveScale = 1f, bool blockJump = false)
        {
            AbilityId = abilityId;
            CooldownTicks = cooldownTicks;
            MpCost = mpCost;
            StartupTicks = startupTicks;
            ActiveTicks = activeTicks;
            RecoveryTicks = recoveryTicks;
            Effects = effects;
            StartupMoveScale = startupMoveScale;
            ActiveMoveScale = activeMoveScale;
            RecoveryMoveScale = recoveryMoveScale;
            BlockJump = blockJump;
        }
    }
}
