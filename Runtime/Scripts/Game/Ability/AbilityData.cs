namespace LOP
{
    /// <summary>어빌리티 타게팅 종류(설정). 지오메트리 해소는 후속.</summary>
    public enum TargetingMode { Self, Unit, Point, Direction }

    /// <summary>
    /// 어빌리티 설정(불변 디자인 데이터). 후속 슬라이스에서 Luban TbAbility로 외부화되며, 코어는 이 구조체만 소비한다.
    /// <para>ProducesEffectIds는 *어떤 효과인지*만 가리킨다 — 실제 StatusEffectData는 호출자(사이드)가 resolve해 TryActivate에 넘긴다.</para>
    /// </summary>
    public readonly struct AbilityData
    {
        public readonly int AbilityId;
        public readonly long CooldownTicks;
        public readonly int MpCost;
        public readonly long CastTimeTicks;     // 0 = 즉발 (캐스트 경로는 후속)
        public readonly TargetingMode TargetingMode;
        public readonly float Range;
        public readonly int[] ProducesEffectIds;

        public AbilityData(int abilityId, long cooldownTicks, int mpCost, long castTimeTicks,
                           TargetingMode targetingMode, float range, int[] producesEffectIds)
        {
            AbilityId = abilityId;
            CooldownTicks = cooldownTicks;
            MpCost = mpCost;
            CastTimeTicks = castTimeTicks;
            TargetingMode = targetingMode;
            Range = range;
            ProducesEffectIds = producesEffectIds;
        }
    }
}
