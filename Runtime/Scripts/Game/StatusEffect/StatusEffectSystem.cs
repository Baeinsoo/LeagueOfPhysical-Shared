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

        /// <summary>
        /// 서버가 보낸 효과 목록으로 이 엔티티의 상태이상을 맞춘다(스냅샷이 권위).
        /// 없는 건 걸고, 사라진 건 떼고, 스택이 다르면 다시 계산한다 — 스탯 모디파이어까지 함께 맞춘다.
        /// <para>내 캐릭 예측은 *내가 건* 효과만 안다. 남이 건 것(슬로우 등)은 계산조차 하지 않으므로
        /// 서버가 알려줘야 하고, 안 그러면 서버만 나를 느리게 움직여 위치가 어긋난다.
        /// 넉백 기여를 스냅에서 복원하는 것과 같은 축이다.</para>
        /// <para><paramref name="resolver"/>로 설정을 찾는다 — 와이어엔 id·만료틱·스택만 실리고
        /// 모디파이어 명세는 마스터데이터에 있다(코어는 MasterData를 직접 참조하지 않는다).</para>
        /// </summary>
        public void ApplyAuthoritativeState(Entity entity,
                                            System.Collections.Generic.IReadOnlyList<ActiveEffect> authoritative,
                                            System.Func<int, StatusEffectData?> resolver)
        {
            var effects = entity.Get<StatusEffects>();
            if (effects == null)
            {
                return;
            }
            if (authoritative == null)
            {
                return;
            }
            var stats = entity.Get<Stats>();

            // 1) 서버에 없는 것 제거(모디파이어도 함께)
            for (int i = effects.Effects.Count - 1; i >= 0; i--)
            {
                var local = effects.Effects[i];
                bool stillActive = false;
                for (int j = 0; j < authoritative.Count; j++)
                {
                    if (authoritative[j].EffectId == local.EffectId)
                    {
                        stillActive = true;
                        break;
                    }
                }
                if (stillActive == false)
                {
                    if (stats != null)
                    {
                        _statsSystem.RemoveModifiersBySourceId(stats, local.SourceId);
                    }
                    effects.Effects.RemoveAt(i);
                }
            }

            // 2) 서버에 있는 것 추가/갱신
            for (int a = 0; a < authoritative.Count; a++)
            {
                var server = authoritative[a];
                string sourceId = SourceIdFor(server.EffectId);
                int idx = effects.Effects.FindIndex(e => e.EffectId == server.EffectId);

                if (idx < 0)
                {
                    var data = resolver(server.EffectId);
                    if (data == null)
                    {
                        continue;   // 설정을 모르는 효과 — 무시(구버전 데이터 등)
                    }
                    effects.Effects.Add(new ActiveEffect(
                        server.EffectId, server.ExpireTick, server.StackCount, server.SourceEntityId, sourceId));
                    AddModifiers(stats, data.Value, sourceId, server.StackCount);
                    continue;
                }

                var current = effects.Effects[idx];
                if (current.StackCount != server.StackCount)
                {
                    var data = resolver(server.EffectId);
                    if (data != null && stats != null)
                    {
                        _statsSystem.RemoveModifiersBySourceId(stats, current.SourceId);
                        AddModifiers(stats, data.Value, current.SourceId, server.StackCount);
                    }
                }
                // 만료 틱은 서버 값으로 덮는다(리프레시된 지속시간을 내가 모를 수 있다).
                effects.Effects[idx] = new ActiveEffect(
                    server.EffectId, server.ExpireTick, server.StackCount, current.SourceEntityId, current.SourceId);
            }
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
