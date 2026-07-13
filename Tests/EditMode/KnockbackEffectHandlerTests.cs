using System.Numerics;
using GameFramework;
using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    public class KnockbackEffectHandlerTests
    {
        private sealed class FakeOverlap : GameFramework.IOverlapQuery
        {
            private readonly string[] ids;
            public FakeOverlap(params string[] ids) { this.ids = ids; }
            public string[] OverlapSphere(Vector3 center, float radius) => ids;
        }

        private static Entity Caster(string id, EntityRegistry reg, Quaternion rot)
        {
            var e = new Entity(id);
            e.Add(new GameFramework.World.Transform { Position = Vector3.Zero, Rotation = rot });
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

        private static KnockbackEffectHandler Handler(EntityRegistry reg, GameFramework.IOverlapQuery overlap)
            => new KnockbackEffectHandler(overlap, reg);

        private static AbilityEffectContext Ctx(Entity caster)
            => new AbilityEffectContext(caster, null, 5L, 0);

        private static KnockbackEffect Effect()
            => new KnockbackEffect(5f, 5f, 90f, 12, 0.8f);   // strength, range, angle, durationTicks, decayPerTick

        [Test]
        public void PushesTargetInFront()
        {
            var reg = new EntityRegistry();
            var caster = Caster("A", reg, Quaternion.Identity);   // forward = +Z
            var target = Target("B", reg, new Vector3(0, 0, 3));
            Handler(reg, new FakeOverlap("B")).OnActiveEnter(Ctx(caster), Effect());
            Assert.AreEqual(1, target.Get<MotionContributions>().Items.Count);
        }

        [Test]
        public void SkipsSelf()
        {
            var reg = new EntityRegistry();
            var caster = Caster("A", reg, Quaternion.Identity);
            Handler(reg, new FakeOverlap("A")).OnActiveEnter(Ctx(caster), Effect());
            Assert.AreEqual(0, caster.Get<MotionContributions>().Items.Count);
        }

        [Test]
        public void SkipsBehindTarget()
        {
            var reg = new EntityRegistry();
            var caster = Caster("A", reg, Quaternion.Identity);
            var target = Target("B", reg, new Vector3(0, 0, -3));
            Handler(reg, new FakeOverlap("B")).OnActiveEnter(Ctx(caster), Effect());
            Assert.AreEqual(0, target.Get<MotionContributions>().Items.Count);
        }

        [Test]
        public void SkipsOutOfRange()
        {
            var reg = new EntityRegistry();
            var caster = Caster("A", reg, Quaternion.Identity);
            var target = Target("B", reg, new Vector3(0, 0, 20));
            Handler(reg, new FakeOverlap("B")).OnActiveEnter(Ctx(caster), Effect());
            Assert.AreEqual(0, target.Get<MotionContributions>().Items.Count);
        }

        [Test]
        public void SkipsUnresolvableId()
        {
            var reg = new EntityRegistry();
            var caster = Caster("A", reg, Quaternion.Identity);
            Assert.DoesNotThrow(() =>
                Handler(reg, new FakeOverlap("ghost")).OnActiveEnter(Ctx(caster), Effect()));
        }
    }
}
