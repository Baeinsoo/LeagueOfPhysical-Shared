using System.Numerics;
using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    public class KnockbackEffectHandlerTests
    {
        private static Entity Caster(string id, EntityRegistry reg)
        {
            var e = new Entity(id);
            e.Add(new GameFramework.World.Transform { Position = Vector3.Zero, Rotation = Quaternion.Identity });
            e.Add(new MotionContributions());
            reg.Add(e);
            return e;
        }

        private static Entity Target(string id, EntityRegistry reg, Vector3 pos)
        {
            var e = new Entity(id);
            e.Add(new GameFramework.World.Transform { Position = pos, Rotation = Quaternion.Identity });
            e.Add(new MotionContributions());
            reg.Add(e);
            return e;
        }

        private static AbilityEffectContext Ctx(Entity caster, AttackHitContext hit)
            => new AbilityEffectContext(caster, null, 5L, 0, hit);

        private static KnockbackEffect Effect() => new KnockbackEffect(5f, 12, 0.8f);   // strength, durationTicks, decayPerTick

        [Test]
        public void Pushes_only_landed_targets()
        {
            var reg = new EntityRegistry();
            var caster = Caster("A", reg);
            var hitTarget = Target("B", reg, new Vector3(0, 0, 3));
            var missTarget = Target("C", reg, new Vector3(0, 0, 3));
            var hit = new AttackHitContext();
            hit.MarkLanded("B");   // B만 명중, C는 닷지

            new KnockbackEffectHandler(reg).OnActiveEnter(Ctx(caster, hit), Effect());

            Assert.AreEqual(1, hitTarget.Get<MotionContributions>().Items.Count);   // 명중 → 넉백
            Assert.AreEqual(0, missTarget.Get<MotionContributions>().Items.Count);  // 닷지 → 넉백 없음
        }

        [Test]
        public void No_landed_targets_pushes_nothing()
        {
            var reg = new EntityRegistry();
            var caster = Caster("A", reg);
            var target = Target("B", reg, new Vector3(0, 0, 3));
            new KnockbackEffectHandler(reg).OnActiveEnter(Ctx(caster, new AttackHitContext()), Effect());
            Assert.AreEqual(0, target.Get<MotionContributions>().Items.Count);
        }

        [Test]
        public void Null_hitContext_pushes_nothing()
        {
            var reg = new EntityRegistry();
            var caster = Caster("A", reg);
            Assert.DoesNotThrow(() =>
                new KnockbackEffectHandler(reg).OnActiveEnter(
                    new AbilityEffectContext(caster, null, 5L, 0, null), Effect()));
        }
    }
}
