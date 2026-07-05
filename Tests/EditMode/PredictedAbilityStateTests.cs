using System.Collections.Generic;
using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    public class PredictedAbilityStateTests
    {
        private static Entity MakeEntity()
        {
            var e = new Entity("caster");
            e.Add(new Abilities());
            e.Add(new Mana(100));
            var stats = new Stats();
            stats.BaseStats[(int)EntityStatType.Dexterity] = 10f;
            e.Add(stats);
            e.Add(new StatusEffects());
            return e;
        }

        [Test]
        public void Capture_IsDeepCopy_LiveMutationDoesNotLeak()
        {
            var e = MakeEntity();
            e.Get<Abilities>().Slots[7] = new AbilitySlot(7, 42);
            e.Get<StatusEffects>().Effects.Add(new ActiveEffect(3, 50, 1, "src", "se:3"));

            var snap = PredictedAbilityState.Capture(e);

            // 캡처 후 라이브를 바꿔도 스냅은 그대로여야 한다.
            e.Get<Abilities>().Slots[7] = new AbilitySlot(7, 999);
            e.Get<StatusEffects>().Effects.Clear();
            e.Get<Mana>().Current = 0;

            Assert.AreEqual(42, snap.Slots[7].CooldownEndTick);
            Assert.AreEqual(1, snap.StatusEffects.Count);
            Assert.AreEqual(3, snap.StatusEffects[0].EffectId);
            Assert.AreEqual(100, snap.ManaCurrent);
        }

        [Test]
        public void RestoreTo_OverwritesLiveState()
        {
            var e = MakeEntity();
            e.Get<Mana>().Current = 80;
            e.Get<StatusEffects>().Effects.Add(new ActiveEffect(3, 50, 1, "src", "se:3"));
            var snap = PredictedAbilityState.Capture(e);

            // 라이브를 다르게 바꾼 뒤 복원하면 스냅 시점으로 돌아와야 한다.
            e.Get<Mana>().Current = 10;
            e.Get<StatusEffects>().Effects.Clear();
            e.Get<Abilities>().ActiveAbility = new ActiveAbility(1, AbilityPhase.Active, 0, 5, 7, e, new AbilityEffect[0]);

            snap.RestoreTo(e);

            Assert.AreEqual(80, e.Get<Mana>().Current);
            Assert.AreEqual(1, e.Get<StatusEffects>().Effects.Count);
            Assert.IsNull(e.Get<Abilities>().ActiveAbility);
        }
    }
}
