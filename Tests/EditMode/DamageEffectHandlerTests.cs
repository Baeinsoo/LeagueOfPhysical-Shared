using System.Numerics;
using GameFramework;
using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    public class DamageEffectHandlerTests
    {
        private sealed class FakeOverlap : GameFramework.Physics.IOverlapQuery
        {
            private readonly string[] ids;
            public FakeOverlap(params string[] ids) { this.ids = ids; }
            public string[] OverlapSphere(Vector3 center, float radius) => ids;
        }

        private sealed class FakeSeed : IMatchSeed
        {
            public ulong Value { get; }
            public FakeSeed(ulong v) { Value = v; }
        }

        private static Entity Player(string id, EntityRegistry reg, StatsSystem stats,
                                     Vector3 pos, int str = 20, int dex = 10, int hp = 1000)
        {
            var e = new Entity(id);
            e.Add(new Ownership("owner-" + id));
            var s = new Stats();
            stats.SetBase(s, (int)EntityStatType.Strength, str);
            stats.SetBase(s, (int)EntityStatType.Dexterity, dex);
            e.Add(s);
            e.Add(new Health(hp));
            e.Add(new GameFramework.World.Transform { Position = pos, Rotation = Quaternion.Identity });
            reg.Add(e);
            return e;
        }

        private static (EntityRegistry reg, WorldEventBuffer buf, StatsSystem stats) World()
            => (new EntityRegistry(), new WorldEventBuffer(), new StatsSystem());

        private static DamageEffectHandler Handler(EntityRegistry reg, WorldEventBuffer buf,
                                                   StatsSystem stats, GameFramework.Physics.IOverlapQuery overlap)
        {
            var combat = new LOPCombatSystem(buf, new HealthSystem(), stats,
                new CombatConfig(0.05f, 0.95f, 0.05f, 0.50f, 1.25f, 1.75f));
            return new DamageEffectHandler(combat, overlap, new FakeSeed(12345UL), reg);
        }

        private static AbilityEffectContext Ctx(Entity caster)
            => new AbilityEffectContext(caster, null, 5L, 0, new AttackHitContext());

        [Test]
        public void Hits_target_in_front_within_sector()
        {
            var (reg, buf, stats) = World();
            var caster = Player("A", reg, stats, new Vector3(0, 0, 0));
            Player("B", reg, stats, new Vector3(0, 0, 3));   // 정면 +Z, 거리 3
            var h = Handler(reg, buf, stats, new FakeOverlap("B"));

            h.OnActiveEnter(Ctx(caster), new DamageEffect(0, 5f, 90f));

            Assert.AreEqual(1, buf.Count);
            Assert.IsInstanceOf<DamageDealtEvent>(buf.Snapshot[0]);
        }

        [Test]
        public void Skips_self()
        {
            var (reg, buf, stats) = World();
            var caster = Player("A", reg, stats, new Vector3(0, 0, 0));
            var h = Handler(reg, buf, stats, new FakeOverlap("A"));   // 오버랩이 자기만 반환

            h.OnActiveEnter(Ctx(caster), new DamageEffect(0, 5f, 90f));

            Assert.AreEqual(0, buf.Count);
        }

        [Test]
        public void Skips_target_behind_caster()
        {
            var (reg, buf, stats) = World();
            var caster = Player("A", reg, stats, new Vector3(0, 0, 0));
            Player("B", reg, stats, new Vector3(0, 0, -3));   // 뒤 -Z
            var h = Handler(reg, buf, stats, new FakeOverlap("B"));

            h.OnActiveEnter(Ctx(caster), new DamageEffect(0, 5f, 90f));

            Assert.AreEqual(0, buf.Count);
        }

        [Test]
        public void Skips_target_out_of_range()
        {
            var (reg, buf, stats) = World();
            var caster = Player("A", reg, stats, new Vector3(0, 0, 0));
            Player("B", reg, stats, new Vector3(0, 0, 10));   // 정면이지만 range 5 밖
            var h = Handler(reg, buf, stats, new FakeOverlap("B"));

            h.OnActiveEnter(Ctx(caster), new DamageEffect(0, 5f, 90f));

            Assert.AreEqual(0, buf.Count);
        }

        [Test]
        public void Rotation_flips_hit_to_miss()
        {
            var (reg, buf, stats) = World();
            var caster = Player("A", reg, stats, new Vector3(0, 0, 0));
            caster.Get<GameFramework.World.Transform>().Rotation =
                Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)System.Math.PI);   // 180° → forward -Z
            Player("B", reg, stats, new Vector3(0, 0, 3));    // 이제 뒤쪽
            var h = Handler(reg, buf, stats, new FakeOverlap("B"));

            h.OnActiveEnter(Ctx(caster), new DamageEffect(0, 5f, 90f));

            Assert.AreEqual(0, buf.Count);
        }

        [Test]
        public void Skips_unresolvable_id()
        {
            var (reg, buf, stats) = World();
            var caster = Player("A", reg, stats, new Vector3(0, 0, 0));
            var h = Handler(reg, buf, stats, new FakeOverlap("ghost"));   // 레지스트리에 없음

            h.OnActiveEnter(Ctx(caster), new DamageEffect(0, 5f, 90f));

            Assert.AreEqual(0, buf.Count);
        }

        [Test]
        public void End_to_end_applies_damage_to_health()
        {
            var (reg, buf, stats) = World();
            var caster = Player("A", reg, stats, new Vector3(0, 0, 0));
            var target = Player("B", reg, stats, new Vector3(0, 0, 3));
            var h = Handler(reg, buf, stats, new FakeOverlap("B"));

            h.OnActiveEnter(Ctx(caster), new DamageEffect(0, 5f, 90f));

            var evt = (DamageDealtEvent)buf.Snapshot[0];
            Assert.AreEqual("B", evt.targetId);
            if (!evt.isDodged)
                Assert.AreEqual(1000 - evt.amount, target.Get<Health>().Current);
        }
    }
}
