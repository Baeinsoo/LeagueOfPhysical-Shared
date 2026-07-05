using System.Collections.Generic;
using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    // 재생 = 앵커 복원 후 어빌리티/상태이상 시스템을 라이브와 같은 순서로 재진행. 최종 상태가 라이브와 일치해야 한다.
    public class AbilityReplayDeterminismTests
    {
        const int AbilityId = 1;
        const int HasteEffectId = 100;
        const int MpCost = 20;

        private ManaSystem _mana;
        private StatsSystem _stats;
        private StatusEffectSystem _status;
        private AbilityEffectExecutor _executor;
        private AbilitySystem _abilities;

        [SetUp]
        public void SetUp()
        {
            _mana = new ManaSystem();
            _stats = new StatsSystem();
            _status = new StatusEffectSystem(_stats);
            var handler = new StatusEffectApplyEffectHandler(_status, Resolve);
            _executor = new AbilityEffectExecutor(new IAbilityEffectHandler[] { handler });
            _abilities = new AbilitySystem(_mana);
        }

        private static StatusEffectData? Resolve(int id)
            => id == HasteEffectId ? (StatusEffectData?)new StatusEffectData(
                HasteEffectId, DurationPolicy.Duration, 3,
                new[] { new StatusModifierSpec((int)EntityStatType.Dexterity, 0.3f, ModifierType.PercentAdd) },
                StatusStackPolicy.Refresh, 1) : null;

        private static AbilityData Haste()
            => new AbilityData(AbilityId, 10, MpCost, 2, 3, 2,
                               new AbilityEffect[] { new StatusEffectApplyEffect(HasteEffectId) });

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

        // 한 틱 진행 = 재조정 재생 루프의 어빌리티/상태 부분과 같은 순서(이동/물리 제외).
        private void AdvanceTick(Entity e, long tick)
        {
            _abilities.Tick(e, tick);
            _status.Tick(e, tick);
            _executor.DriveActiveEntity(e, null, tick);
        }

        private static void AssertStateEqual(PredictedAbilityState expected, Entity actual, long atTick)
        {
            var abilities = actual.Get<Abilities>();
            Assert.AreEqual(expected.ActiveAbility?.Phase, abilities.ActiveAbility?.Phase, $"tick {atTick}: phase");
            Assert.AreEqual(expected.Slots[AbilityId].CooldownEndTick,
                            abilities.Slots[AbilityId].CooldownEndTick, $"tick {atTick}: cooldown");
            Assert.AreEqual(expected.StatusEffects.Count,
                            actual.Get<StatusEffects>().Effects.Count, $"tick {atTick}: status count");
            Assert.AreEqual(expected.ManaCurrent, actual.Get<Mana>().Current, $"tick {atTick}: mana");
            Assert.AreEqual(expected.Modifiers.Count,
                            actual.Get<Stats>().Modifiers.Count, $"tick {atTick}: modifiers");
        }

        [Test]
        public void RestoreThenReplay_ReproducesAbilityAndStatusState()
        {
            const int anchor = 1;
            const int last = 9;

            // 1) 라이브 진행 — 틱 0에 발동, 매 틱 진행 후 상태 캡처.
            var e = MakeEntity();
            _abilities.Grant(e, AbilityId);
            _abilities.TryActivate(e, Haste(), e, 0);

            var recorded = new Dictionary<long, PredictedAbilityState>();
            for (long t = 0; t <= last; t++)
            {
                AdvanceTick(e, t);
                recorded[t] = PredictedAbilityState.Capture(e);
            }

            // 2) 앵커(anchor)로 복원 후 anchor+1..last 재진행.
            recorded[anchor].RestoreTo(e);
            for (long t = anchor + 1; t <= last; t++)
            {
                AdvanceTick(e, t);
                AssertStateEqual(recorded[t], e, t);   // 재현 == 라이브
            }
        }
    }
}
