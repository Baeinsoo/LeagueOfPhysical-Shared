using System.Collections.Generic;
using GameFramework.World;

namespace LOP
{
    /// <summary>
    /// 되감기용으로 남기는 LOP 고유 상태의 한 틱 사진(깊은 복사) — 어빌리티·상태이상·스탯·마나.
    /// 위치·속도는 <see cref="GameFramework.World.WorldBase"/>가 담으므로 여기엔 없다.
    /// Unreal <c>FSavedMove_Character</c>를 게임이 서브클래싱해 자기 데이터를 얹는 것과 같은 자리.
    /// </summary>
    public sealed class LOPSavedState
    {
        public AbilityActivation? Activation { get; private set; }
        public Dictionary<int, GrantedAbility> Granted { get; private set; }
        public List<ActiveEffect> StatusEffects { get; private set; }
        public Dictionary<int, float> BaseStats { get; private set; }
        public List<StatModifier> Modifiers { get; private set; }
        public int UnspentPoints { get; private set; }
        public int ManaCurrent { get; private set; }
        public int ManaMax { get; private set; }

        public static LOPSavedState Capture(Entity entity)
        {
            var s = new LOPSavedState();
            var abilities = entity.Get<Abilities>();
            s.Activation = abilities?.Activation;
            s.Granted = abilities != null
                ? new Dictionary<int, GrantedAbility>(abilities.Granted)
                : new Dictionary<int, GrantedAbility>();

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
                abilities.Activation = Activation;
                abilities.Granted.Clear();
                foreach (var kv in Granted)
                {
                    abilities.Granted[kv.Key] = kv.Value;
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
