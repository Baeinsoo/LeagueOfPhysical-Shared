namespace LOP
{
    /// <summary>
    /// 어빌리티가 하는 일 하나(타입 있는 데이터 레코드). 어빌리티는 이 레코드들의 리스트를 소유한다(조합).
    /// 순수 데이터(스펙)만 — 실제 동작은 타입별 <see cref="IAbilityEffectHandler"/>가 한다.
    /// 종류 추가 = 서브타입 + 핸들러 1개(평면 컬럼/스캔 시스템 증식 없음). GAS GameplayEffect / WoW SpellEffect 대응.
    /// </summary>
    public abstract class AbilityEffect
    {
    }

    /// <summary>데미지를 준다(공격). 타게팅 형상은 이 effect가 들고 있다(부채꼴). 실제 판정은 서버 핸들러.</summary>
    public sealed class DamageEffect : AbilityEffect
    {
        public readonly int Amount;
        public readonly float Range;    // 판정 반경
        public readonly float Angle;    // 부채꼴 전체 각(도). 0이면 형상 핸들러가 정함.

        public DamageEffect(int amount, float range, float angle)
        {
            Amount = amount;
            Range = range;
            Angle = angle;
        }
    }

    /// <summary>상태효과를 건다(버프/디버프). 적용된 효과는 독립 <see cref="StatusEffects"/> 컴포넌트로 살아간다(수명 분리).</summary>
    public sealed class StatusEffectApplyEffect : AbilityEffect
    {
        public readonly int StatusEffectId;     // TbStatusEffect 참조(런타임 데이터는 핸들러가 resolve)

        public StatusEffectApplyEffect(int statusEffectId)
        {
            StatusEffectId = statusEffectId;
        }
    }

    /// <summary>전방으로 민다(대시). 모션/물리는 side 개념이라 실제 push는 side 핸들러.</summary>
    public sealed class MotionEffect : AbilityEffect
    {
        public readonly float Speed;

        public MotionEffect(float speed)
        {
            Speed = speed;
        }
    }
}
