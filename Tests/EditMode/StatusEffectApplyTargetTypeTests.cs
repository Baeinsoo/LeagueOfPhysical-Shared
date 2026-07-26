using System;
using NUnit.Framework;
using GameFramework.World;

namespace LOP.Tests.EditMode
{
    public class StatusEffectApplyTargetTypeTests
    {
        private const int SlowId = 100;

        private static StatusEffectData SlowData() => new StatusEffectData(
            SlowId, DurationPolicy.Duration, 60,
            new[] { new StatusModifierSpec((int)EntityStatType.MoveSpeed, -0.3f, ModifierType.PercentAdd) },
            StatusStackPolicy.Refresh, 1);

        private static Entity MakeActor(string id)
        {
            var e = new Entity(id);
            e.Add(new StatusEffects());
            e.Add(new Stats());
            return e;
        }

        private static bool HasSlow(Entity e) =>
            e.Get<StatusEffects>().Effects.Exists(x => x.EffectId == SlowId);

        private (StatusEffectApplyEffectHandler handler, EntityRegistry registry) Build()
        {
            var registry = new EntityRegistry();
            var handler = new StatusEffectApplyEffectHandler(
                new StatusEffectSystem(new StatsSystem()), _ => SlowData(), registry);
            return (handler, registry);
        }

        [Test]
        public void SelfAppliesToCasterOnly()
        {
            var (handler, registry) = Build();
            var caster = MakeActor("caster");
            var victim = MakeActor("victim");
            registry.Add(caster);
            registry.Add(victim);

            var hit = new AttackHitContext();
            hit.MarkLanded("victim");
            var ctx = new AbilityEffectContext(caster, caster, 10, 0, hit);

            handler.OnActiveEnter(ctx, new StatusEffectApplyEffect(SlowId, TargetType.Self));

            Assert.IsTrue(HasSlow(caster));
            Assert.IsFalse(HasSlow(victim));
        }

        [Test]
        public void HitTargetsAppliesToLandedOnly()
        {
            var (handler, registry) = Build();
            var caster = MakeActor("caster");
            var hitVictim = MakeActor("hit");
            var missedVictim = MakeActor("missed");
            registry.Add(caster);
            registry.Add(hitVictim);
            registry.Add(missedVictim);

            var hit = new AttackHitContext();
            hit.MarkLanded("hit");
            var ctx = new AbilityEffectContext(caster, caster, 10, 0, hit);

            handler.OnActiveEnter(ctx, new StatusEffectApplyEffect(SlowId, TargetType.HitTargets));

            Assert.IsTrue(HasSlow(hitVictim));
            Assert.IsFalse(HasSlow(missedVictim));
            Assert.IsFalse(HasSlow(caster));
        }

        [Test]
        public void HitTargetsWithNoLandedTargetsDoesNothing()
        {
            var (handler, registry) = Build();
            var caster = MakeActor("caster");
            registry.Add(caster);

            var ctx = new AbilityEffectContext(caster, caster, 10, 0, new AttackHitContext());

            Assert.DoesNotThrow(() =>
                handler.OnActiveEnter(ctx, new StatusEffectApplyEffect(SlowId, TargetType.HitTargets)));
            Assert.IsFalse(HasSlow(caster));
        }

        [Test]
        public void DefaultTargetIsSelf()
        {
            Assert.AreEqual(TargetType.Self, new StatusEffectApplyEffect(SlowId).Target);
        }

        [Test]
        public void HitTargetsWithNullHitContextDoesNotThrow()
        {
            var (handler, registry) = Build();
            var caster = MakeActor("caster");
            registry.Add(caster);

            var ctx = new AbilityEffectContext(caster, caster, 10, 0, null);

            Assert.DoesNotThrow(() =>
                handler.OnActiveEnter(ctx, new StatusEffectApplyEffect(SlowId, TargetType.HitTargets)));
            Assert.IsFalse(HasSlow(caster));
        }

        [Test]
        public void HitTargetsWithMissingTargetInRegistryDoesNotThrow()
        {
            var (handler, registry) = Build();
            var caster = MakeActor("caster");
            registry.Add(caster);

            var hit = new AttackHitContext();
            hit.MarkLanded("gone");  // Mark as landed but don't add to registry
            var ctx = new AbilityEffectContext(caster, caster, 10, 0, hit);

            Assert.DoesNotThrow(() =>
                handler.OnActiveEnter(ctx, new StatusEffectApplyEffect(SlowId, TargetType.HitTargets)));
            Assert.IsFalse(HasSlow(caster));
        }
    }
}
