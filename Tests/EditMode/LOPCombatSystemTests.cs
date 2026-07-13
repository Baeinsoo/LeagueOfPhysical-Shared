using GameFramework;
using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    public class LOPCombatSystemTests
    {
        private static (WorldEventBuffer buf, LOPCombatSystem combat, StatsSystem stats) NewCombat()
        {
            var buf = new WorldEventBuffer();
            var stats = new StatsSystem();
            var combat = new LOPCombatSystem(buf, new HealthSystem(), stats);
            return (buf, combat, stats);
        }

        private static Entity Player(string id, StatsSystem stats, int str, int dex, int? hp = null)
        {
            var e = new Entity(id);
            e.Add(new Ownership("owner-" + id));
            var s = new Stats();
            stats.SetBase(s, (int)EntityStatType.Strength, str);
            stats.SetBase(s, (int)EntityStatType.Dexterity, dex);
            e.Add(s);
            if (hp.HasValue) e.Add(new Health(hp.Value));
            return e;
        }

        [Test]
        public void Attack_is_deterministic_for_same_inputs()
        {
            var a = NewCombat(); var b = NewCombat();
            a.combat.Attack(Player("A", a.stats, 20, 10), Player("B", a.stats, 10, 10, 100), 10, 5, 0, 12345UL);
            b.combat.Attack(Player("A", b.stats, 20, 10), Player("B", b.stats, 10, 10, 100), 10, 5, 0, 12345UL);

            var da = (DamageDealtEvent)a.buf.Snapshot[0];
            var db = (DamageDealtEvent)b.buf.Snapshot[0];
            Assert.AreEqual(da.amount, db.amount);
            Assert.AreEqual(da.isCritical, db.isCritical);
            Assert.AreEqual(da.isDodged, db.isDodged);
        }

        [Test]
        public void Attack_event_matches_health_change()
        {
            var (buf, combat, stats) = NewCombat();
            var target = Player("B", stats, 10, 10, 100);
            combat.Attack(Player("A", stats, 20, 10), target, 10, 5, 0, 12345UL);

            var evt = (DamageDealtEvent)buf.Snapshot[0];
            var health = target.Get<Health>();
            if (evt.isDodged)
            {
                Assert.AreEqual(0, evt.amount);
                Assert.AreEqual(100, health.Current);
            }
            else
            {
                Assert.Greater(evt.amount, 0);
                Assert.AreEqual(System.Math.Max(0, 100 - evt.amount), health.Current);  // 데미지가 HP 초과 시 0으로 클램프
            }
        }

        [Test]
        public void Attack_death_event_iff_target_dies()
        {
            var (buf, combat, stats) = NewCombat();
            var target = Player("B", stats, 10, 10, 1);   // 1 HP
            combat.Attack(Player("A", stats, 20, 10), target, 10, 5, 0, 12345UL);

            var dmg = (DamageDealtEvent)buf.Snapshot[0];
            bool hasDeath = buf.Snapshot.Count > 1 && buf.Snapshot[1] is DeathEvent;
            if (dmg.isDodged)
            {
                Assert.IsFalse(hasDeath);
                Assert.AreEqual(1, target.Get<Health>().Current);
            }
            else
            {
                Assert.IsTrue(hasDeath, "비회피 히트로 1HP 대상이 죽으면 DeathEvent");
                Assert.AreEqual(0, target.Get<Health>().Current);
            }
        }

        [Test]
        public void Attack_noop_when_neither_is_player()
        {
            var (buf, combat, stats) = NewCombat();
            var attacker = new Entity("A"); attacker.Add(new Stats());
            var target = new Entity("B"); target.Add(new Health(100));

            combat.Attack(attacker, target, 10, 5, 0, 12345UL);

            Assert.AreEqual(0, buf.Count);
            Assert.AreEqual(100, target.Get<Health>().Current);
        }

        [Test]
        public void Attack_damage_scales_with_effect_base_amount()
        {
            // 같은 시드/스탯, base만 다르게 → 회피/크리 판정 동일, 데미지는 base로 구동(MasterData 구동 확인).
            var low = NewCombat(); var high = NewCombat();
            low.combat.Attack(Player("A", low.stats, 20, 10), Player("B", low.stats, 10, 10, 1000), 10, 5, 0, 42UL);
            high.combat.Attack(Player("A", high.stats, 20, 10), Player("B", high.stats, 10, 10, 1000), 100, 5, 0, 42UL);

            var dl = (DamageDealtEvent)low.buf.Snapshot[0];
            var dh = (DamageDealtEvent)high.buf.Snapshot[0];
            Assert.AreEqual(dl.isDodged, dh.isDodged);   // 같은 시드 → 같은 회피 판정
            if (dl.isDodged)
            {
                Assert.AreEqual(0, dl.amount);
                Assert.AreEqual(0, dh.amount);
            }
            else
            {
                Assert.Greater(dh.amount, dl.amount);   // 높은 base → 더 큰 데미지
            }
        }
    }
}
