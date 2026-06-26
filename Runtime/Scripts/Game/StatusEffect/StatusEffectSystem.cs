using GameFramework.World;

namespace LOP
{
    /// <summary>
    /// 상태이상 로직(상태 없음). GAS GameplayEffect 생명주기(Apply→Tick(만료)→Remove)를 anemic으로 구현.
    /// 모디파이어는 효과 인스턴스 SourceId로 달고, 만료/제거 시 그 SourceId로 일괄 해제한다.
    /// </summary>
    public class StatusEffectSystem
    {
        private readonly StatsSystem _statsSystem;

        public StatusEffectSystem(StatsSystem statsSystem)
        {
            _statsSystem = statsSystem;
        }

        private static string SourceIdFor(int effectId) => "se:" + effectId;

        /// <summary>효과를 타깃에 적용한다(GAS ApplyGameplayEffectToTarget). 스택/지속/모디파이어 해소.</summary>
        public void Apply(Entity target, in StatusEffectData data, string sourceEntityId, long currentTick)
        {
            var effects = target.Get<StatusEffects>();
            if (effects == null)
            {
                return;
            }
            var stats = target.Get<Stats>();

            // Instant = 영구 베이스 변경. 추적/모디파이어 없음.
            if (data.DurationPolicy == DurationPolicy.Instant)
            {
                if (stats != null && data.Modifiers != null)
                {
                    foreach (var m in data.Modifiers)
                    {
                        _statsSystem.AddBase(stats, m.StatType, m.Value);
                    }
                }
                return;
            }

            int effectId = data.EffectId;
            string sourceId = SourceIdFor(effectId);
            long expire = data.DurationPolicy == DurationPolicy.Duration ? currentTick + data.DurationTicks : -1L;

            int idx = effects.Effects.FindIndex(e => e.EffectId == effectId);
            if (idx < 0)
            {
                effects.Effects.Add(new ActiveEffect(data.EffectId, expire, 1, sourceEntityId, sourceId));
                AddModifiers(stats, data, sourceId, 1);
                return;
            }

            // 재적용: 지속 리프레시(+ StackMagnitude면 스택 증가·모디파이어 재배율)
            var active = effects.Effects[idx];
            int stack = active.StackCount;
            if (data.StackPolicy == StatusStackPolicy.StackMagnitude && stack < data.MaxStacks)
            {
                stack++;
                _statsSystem.RemoveModifiersBySourceId(stats, sourceId);
                AddModifiers(stats, data, sourceId, stack);
            }
            effects.Effects[idx] = new ActiveEffect(active.EffectId, expire, stack, active.SourceEntityId, sourceId);
        }

        /// <summary>만료된(Duration) 효과를 제거하고 모디파이어를 해제한다. 매 틱 호출.</summary>
        public void Tick(Entity entity, long currentTick)
        {
            var effects = entity.Get<StatusEffects>();
            if (effects == null)
            {
                return;
            }
            var stats = entity.Get<Stats>();

            for (int i = effects.Effects.Count - 1; i >= 0; i--)
            {
                var e = effects.Effects[i];
                if (e.ExpireTick >= 0 && currentTick >= e.ExpireTick)
                {
                    if (stats != null)
                    {
                        _statsSystem.RemoveModifiersBySourceId(stats, e.SourceId);
                    }
                    effects.Effects.RemoveAt(i);
                }
            }
        }

        /// <summary>효과를 명시적으로 제거한다(디스펠 등). 모디파이어도 함께 해제.</summary>
        public bool Remove(Entity entity, int effectId)
        {
            var effects = entity.Get<StatusEffects>();
            if (effects == null)
            {
                return false;
            }
            int idx = effects.Effects.FindIndex(e => e.EffectId == effectId);
            if (idx < 0)
            {
                return false;
            }

            var stats = entity.Get<Stats>();
            if (stats != null)
            {
                _statsSystem.RemoveModifiersBySourceId(stats, effects.Effects[idx].SourceId);
            }
            effects.Effects.RemoveAt(idx);
            return true;
        }

        private void AddModifiers(Stats stats, in StatusEffectData data, string sourceId, int stackCount)
        {
            if (stats == null || data.Modifiers == null)
            {
                return;
            }
            foreach (var m in data.Modifiers)
            {
                _statsSystem.AddModifier(stats, new StatModifier(m.StatType, m.Value * stackCount, m.Type, sourceId));
            }
        }
    }
}
