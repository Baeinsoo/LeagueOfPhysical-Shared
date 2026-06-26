using GameFramework.World;
using LOP;
using NUnit.Framework;

namespace LOP.Tests
{
    public class AbilitySystemTests
    {
        const float Tolerance = 1e-4f;
        const int AbilityId = 1;
        const int MpCost = 20;
        const int Cooldown = 10;

        private ManaSystem _mana;
        private StatsSystem _stats;
        private StatusEffectSystem _statusEffects;
        private AbilitySystem _system;

        [SetUp]
        public void SetUp()
        {
            _mana = new ManaSystem();
            _stats = new StatsSystem();
            _statusEffects = new StatusEffectSystem(_stats);
            _system = new AbilitySystem(_mana, _statusEffects);
        }

        private Entity MakeEntity(int manaMax, float dexBase)
        {
            var entity = new Entity("caster");
            entity.Add(new Abilities());
            entity.Add(new Mana(manaMax));
            var stats = new Stats();
            stats.BaseStats[(int)EntityStatType.Dexterity] = dexBase;
            entity.Add(stats);
            entity.Add(new StatusEffects());
            return entity;
        }

        private static AbilityData Ability()
            => new AbilityData(AbilityId, Cooldown, MpCost, 0L, TargetingMode.Self, 0f, new[] { 100 });

        private static StatusEffectData[] HasteEffects()
            => new[]
            {
                new StatusEffectData(
                    100, DurationPolicy.Duration, 5,
                    new[] { new StatusModifierSpec((int)EntityStatType.Dexterity, 0.3f, ModifierType.PercentAdd) },
                    StatusStackPolicy.Refresh, 1)
            };

        private int Mana(Entity e) => e.Get<Mana>().Current;
        private long CooldownEnd(Entity e) => e.Get<Abilities>().Slots[AbilityId].CooldownEndTick;
        private int EffectCount(Entity e) => e.Get<StatusEffects>().Effects.Count;
        private float Dex(Entity e) => _stats.GetValue(e.Get<Stats>(), (int)EntityStatType.Dexterity);

        [Test]
        public void Grant_AddsReadySlot()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);

            Assert.That(_system.CanActivate(e, Ability(), 0), Is.True);
        }

        [Test]
        public void TryActivate_Success_SpendsMana_SetsCooldown_AppliesEffect()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);

            bool ok = _system.TryActivate(e, Ability(), e, HasteEffects(), 0);

            Assert.That(ok, Is.True);
            Assert.That(Mana(e), Is.EqualTo(100 - MpCost));
            Assert.That(CooldownEnd(e), Is.EqualTo(Cooldown)); // 0 + 10
            Assert.That(EffectCount(e), Is.EqualTo(1));
            Assert.That(Dex(e), Is.EqualTo(13f).Within(Tolerance)); // 10 * 1.3
        }

        [Test]
        public void TryActivate_OnCooldown_Fails()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);
            _system.TryActivate(e, Ability(), e, HasteEffects(), 0);

            bool again = _system.TryActivate(e, Ability(), e, HasteEffects(), 0);

            Assert.That(again, Is.False);
            Assert.That(Mana(e), Is.EqualTo(100 - MpCost), "쿨다운 실패 시 추가 차감 없음");
            Assert.That(EffectCount(e), Is.EqualTo(1), "효과 추가 적용 없음");
        }

        [Test]
        public void TryActivate_AfterCooldown_Succeeds()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);
            _system.TryActivate(e, Ability(), e, HasteEffects(), 0);   // cd end @10

            bool ok = _system.TryActivate(e, Ability(), e, HasteEffects(), Cooldown);

            Assert.That(ok, Is.True);
            Assert.That(Mana(e), Is.EqualTo(100 - MpCost * 2));
            Assert.That(CooldownEnd(e), Is.EqualTo(Cooldown * 2)); // 10 + 10
        }

        [Test]
        public void CanActivate_InsufficientMana_False()
        {
            var e = MakeEntity(10, 10f); // max 10 < MpCost 20
            _system.Grant(e, AbilityId);

            Assert.That(_system.CanActivate(e, Ability(), 0), Is.False);

            bool ok = _system.TryActivate(e, Ability(), e, HasteEffects(), 0);
            Assert.That(ok, Is.False);
            Assert.That(Mana(e), Is.EqualTo(10), "부수효과 없음");
            Assert.That(EffectCount(e), Is.EqualTo(0));
        }

        [Test]
        public void TryActivate_NotGranted_False()
        {
            var e = MakeEntity(100, 10f); // Grant 안 함

            bool ok = _system.TryActivate(e, Ability(), e, HasteEffects(), 0);

            Assert.That(ok, Is.False);
            Assert.That(EffectCount(e), Is.EqualTo(0));
        }

        [Test]
        public void TryActivate_NullProducedEffects_StillCommits()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);

            bool ok = _system.TryActivate(e, Ability(), e, null, 0);

            Assert.That(ok, Is.True);
            Assert.That(Mana(e), Is.EqualTo(100 - MpCost));
            Assert.That(CooldownEnd(e), Is.EqualTo(Cooldown));
            Assert.That(EffectCount(e), Is.EqualTo(0));
        }
    }
}
