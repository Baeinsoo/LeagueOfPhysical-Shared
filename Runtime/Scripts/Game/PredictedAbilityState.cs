using System.Collections.Generic;
using GameFramework.World;

namespace LOP
{
    /// <summary>
    /// 내 캐릭 예측 재생용 어빌리티/상태이상/스탯/마나 상태의 한 틱 사진(깊은 복사).
    /// 위치/속도는 별도(GameFramework.Netcode.EntitySnapshot). 롤백 재조정이 앵커 틱으로 복원 후 재생에 쓴다.
    /// </summary>
    public sealed class PredictedAbilityState
    {
        public ActiveAbility? ActiveAbility { get; private set; }
        public Dictionary<int, AbilitySlot> Slots { get; private set; }
        public List<ActiveEffect> StatusEffects { get; private set; }
        public Dictionary<int, float> BaseStats { get; private set; }
        public List<StatModifier> Modifiers { get; private set; }
        public int UnspentPoints { get; private set; }
        public int ManaCurrent { get; private set; }
        public int ManaMax { get; private set; }

        public static PredictedAbilityState Capture(Entity entity)
        {
            var s = new PredictedAbilityState();
            var abilities = entity.Get<Abilities>();
            s.ActiveAbility = abilities?.ActiveAbility;
            s.Slots = abilities != null
                ? new Dictionary<int, AbilitySlot>(abilities.Slots)
                : new Dictionary<int, AbilitySlot>();

            var status = entity.Get<StatusEffects>();
            s.StatusEffects = status != null
                ? new List<ActiveEffect>(status.Effects)
                : new List<ActiveEffect>();

            var stats = entity.Get<Stats>();
            s.BaseStats = stats != null
                ? new Dictionary<int, float>(stats.BaseStats)
                : new Dictionary<int, float>();
            s.Modifiers = stats != null
                ? new List<StatModifier>(stats.Modifiers)
                : new List<StatModifier>();
            s.UnspentPoints = stats?.UnspentPoints ?? 0;

            var mana = entity.Get<Mana>();
            s.ManaCurrent = mana?.Current ?? 0;
            s.ManaMax = mana?.Max ?? 0;
            return s;
        }

        public void RestoreTo(Entity entity)
        {
            var abilities = entity.Get<Abilities>();
            if (abilities != null)
            {
                abilities.ActiveAbility = ActiveAbility;
                abilities.Slots.Clear();
                foreach (var kv in Slots)
                {
                    abilities.Slots[kv.Key] = kv.Value;
                }
            }

            var status = entity.Get<StatusEffects>();
            if (status != null)
            {
                status.Effects.Clear();
                status.Effects.AddRange(StatusEffects);
            }

            var stats = entity.Get<Stats>();
            if (stats != null)
            {
                stats.BaseStats.Clear();
                foreach (var kv in BaseStats)
                {
                    stats.BaseStats[kv.Key] = kv.Value;
                }
                stats.Modifiers.Clear();
                stats.Modifiers.AddRange(Modifiers);
                stats.UnspentPoints = UnspentPoints;
            }

            var mana = entity.Get<Mana>();
            if (mana != null)
            {
                mana.Current = ManaCurrent;
                mana.Max = ManaMax;
            }
        }
    }
}
