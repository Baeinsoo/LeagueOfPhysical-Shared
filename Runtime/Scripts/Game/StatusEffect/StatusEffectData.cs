using GameFramework.World;

namespace LOP
{
    /// <summary>효과 지속 정책. Instant=즉시 1회(영구 베이스 변경), Duration=한시, Infinite=명시 제거까지.</summary>
    public enum DurationPolicy { Instant, Duration, Infinite }

    /// <summary>재적용 시 스택 규칙. Refresh=지속만 리셋, StackMagnitude=스택 증가로 효과 배율.</summary>
    public enum StatusStackPolicy { Refresh, StackMagnitude }

    /// <summary>효과가 부여하는 스탯 모디파이어 하나(설정값). 적용 시 SourceId가 붙어 <see cref="StatModifier"/>가 된다.</summary>
    public readonly struct StatusModifierSpec
    {
        public readonly int StatType;
        public readonly float Value;
        public readonly ModifierType Type;

        public StatusModifierSpec(int statType, float value, ModifierType type)
        {
            StatType = statType;
            Value = value;
            Type = type;
        }
    }

    /// <summary>
    /// 상태이상 설정(불변 디자인 데이터). 후속 슬라이스에서 Luban 테이블로 외부화되며, 코어는 이 구조체만 소비한다.
    /// </summary>
    public readonly struct StatusEffectData
    {
        public readonly int EffectId;
        public readonly DurationPolicy DurationPolicy;
        public readonly long DurationTicks;
        public readonly StatusModifierSpec[] Modifiers;
        public readonly StatusStackPolicy StackPolicy;
        public readonly int MaxStacks;

        public StatusEffectData(int effectId, DurationPolicy durationPolicy, long durationTicks,
                                StatusModifierSpec[] modifiers, StatusStackPolicy stackPolicy, int maxStacks)
        {
            EffectId = effectId;
            DurationPolicy = durationPolicy;
            DurationTicks = durationTicks;
            Modifiers = modifiers;
            StackPolicy = stackPolicy;
            MaxStacks = maxStacks;
        }
    }
}
