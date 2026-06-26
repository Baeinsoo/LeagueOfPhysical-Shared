using GameFramework.World;
using LOP;
using NUnit.Framework;

namespace LOP.Tests
{
    public class StatusEffectSystemTests
    {
        const float Tolerance = 1e-4f;

        private StatsSystem _stats;
        private StatusEffectSystem _system;

        [SetUp]
        public void SetUp()
        {
            _stats = new StatsSystem();
            _system = new StatusEffectSystem(_stats);
        }

        private Entity MakeEntity(EntityStatType statType, float baseValue)
        {
            var entity = new Entity("e1");
            var stats = new Stats();
            stats.BaseStats[(int)statType] = baseValue;
            entity.Add(stats);
            entity.Add(new StatusEffects());
            return entity;
        }

        private float Value(Entity entity, EntityStatType statType)
            => _stats.GetValue(entity.Get<Stats>(), (int)statType);

        private int ActiveCount(Entity entity) => entity.Get<StatusEffects>().Effects.Count;

        private static StatusEffectData Haste(long durationTicks)
            => new StatusEffectData(
                effectId: 1,
                durationPolicy: DurationPolicy.Duration,
                durationTicks: durationTicks,
                modifiers: new[] { new StatusModifierSpec((int)EntityStatType.Dexterity, 0.3f, ModifierType.PercentAdd) },
                stackPolicy: StatusStackPolicy.Refresh,
                maxStacks: 1);

        [Test]
        public void Apply_Duration_AddsModifier_IncreasesValue()
        {
            var entity = MakeEntity(EntityStatType.Dexterity, 10f);

            _system.Apply(entity, Haste(5), "src", 0);

            // 10 * (1 + 0.3) = 13
            Assert.That(Value(entity, EntityStatType.Dexterity), Is.EqualTo(13f).Within(Tolerance));
            Assert.That(ActiveCount(entity), Is.EqualTo(1));
        }

        [Test]
        public void Tick_BeforeExpire_StaysActive()
        {
            var entity = MakeEntity(EntityStatType.Dexterity, 10f);
            _system.Apply(entity, Haste(5), "src", 0);

            _system.Tick(entity, 4);

            Assert.That(Value(entity, EntityStatType.Dexterity), Is.EqualTo(13f).Within(Tolerance));
            Assert.That(ActiveCount(entity), Is.EqualTo(1));
        }

        [Test]
        public void Tick_AtExpire_RemovesModifier_Reverts()
        {
            var entity = MakeEntity(EntityStatType.Dexterity, 10f);
            _system.Apply(entity, Haste(5), "src", 0);

            _system.Tick(entity, 5);

            Assert.That(Value(entity, EntityStatType.Dexterity), Is.EqualTo(10f).Within(Tolerance));
            Assert.That(ActiveCount(entity), Is.EqualTo(0));
            Assert.That(entity.Get<Stats>().Modifiers, Is.Empty);
        }

        [Test]
        public void Apply_Refresh_ExtendsExpire()
        {
            var entity = MakeEntity(EntityStatType.Dexterity, 10f);
            _system.Apply(entity, Haste(5), "src", 0);   // expire @5
            _system.Apply(entity, Haste(5), "src", 3);   // refresh → expire @8

            _system.Tick(entity, 5);
            Assert.That(ActiveCount(entity), Is.EqualTo(1), "refresh로 연장되어 5틱엔 아직 활성");

            _system.Tick(entity, 8);
            Assert.That(ActiveCount(entity), Is.EqualTo(0), "8틱에 만료");
        }

        [Test]
        public void Apply_StackMagnitude_ScalesAndCaps()
        {
            var entity = MakeEntity(EntityStatType.Strength, 10f);
            var stacking = new StatusEffectData(
                effectId: 2,
                durationPolicy: DurationPolicy.Duration,
                durationTicks: 100,
                modifiers: new[] { new StatusModifierSpec((int)EntityStatType.Strength, 2f, ModifierType.Flat) },
                stackPolicy: StatusStackPolicy.StackMagnitude,
                maxStacks: 3);

            _system.Apply(entity, stacking, "src", 0);   // x1 → 12
            Assert.That(Value(entity, EntityStatType.Strength), Is.EqualTo(12f).Within(Tolerance));

            _system.Apply(entity, stacking, "src", 0);   // x2 → 14
            Assert.That(Value(entity, EntityStatType.Strength), Is.EqualTo(14f).Within(Tolerance));

            _system.Apply(entity, stacking, "src", 0);   // x3 → 16
            Assert.That(Value(entity, EntityStatType.Strength), Is.EqualTo(16f).Within(Tolerance));

            _system.Apply(entity, stacking, "src", 0);   // x4 capped → 16
            Assert.That(Value(entity, EntityStatType.Strength), Is.EqualTo(16f).Within(Tolerance));
            Assert.That(entity.Get<StatusEffects>().Effects[0].StackCount, Is.EqualTo(3));
        }

        [Test]
        public void Remove_Explicit_CleansUp()
        {
            var entity = MakeEntity(EntityStatType.Strength, 10f);
            var infinite = new StatusEffectData(
                effectId: 3,
                durationPolicy: DurationPolicy.Infinite,
                durationTicks: 0,
                modifiers: new[] { new StatusModifierSpec((int)EntityStatType.Strength, 5f, ModifierType.Flat) },
                stackPolicy: StatusStackPolicy.Refresh,
                maxStacks: 1);

            _system.Apply(entity, infinite, "src", 0);
            Assert.That(Value(entity, EntityStatType.Strength), Is.EqualTo(15f).Within(Tolerance));

            bool removed = _system.Remove(entity, 3);

            Assert.That(removed, Is.True);
            Assert.That(Value(entity, EntityStatType.Strength), Is.EqualTo(10f).Within(Tolerance));
            Assert.That(ActiveCount(entity), Is.EqualTo(0));
        }

        [Test]
        public void Apply_Instant_PermanentBase_NoTracking()
        {
            var entity = MakeEntity(EntityStatType.Strength, 10f);
            var instant = new StatusEffectData(
                effectId: 4,
                durationPolicy: DurationPolicy.Instant,
                durationTicks: 0,
                modifiers: new[] { new StatusModifierSpec((int)EntityStatType.Strength, 5f, ModifierType.Flat) },
                stackPolicy: StatusStackPolicy.Refresh,
                maxStacks: 1);

            _system.Apply(entity, instant, "src", 0);

            Assert.That(Value(entity, EntityStatType.Strength), Is.EqualTo(15f).Within(Tolerance));
            Assert.That(ActiveCount(entity), Is.EqualTo(0), "Instant은 추적 안 함");

            // 영구 변경이므로 Remove 대상 없음, 값 유지
            Assert.That(_system.Remove(entity, 4), Is.False);
            Assert.That(Value(entity, EntityStatType.Strength), Is.EqualTo(15f).Within(Tolerance));
        }
    }
}
