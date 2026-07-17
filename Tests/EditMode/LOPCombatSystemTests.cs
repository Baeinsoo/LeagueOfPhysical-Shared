using GameFramework;
using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    public class LOPCombatSystemTests
    {
        // 기본 config = 현 하드코딩 값(동작 무변화)
        private static CombatConfig DefaultConfig()
            => new CombatConfig(0.05f, 0.95f, 0.05f, 0.50f, 1.25f, 1.75f);

        private static (WorldEventBuffer buf, LOPCombatSystem combat, StatsSystem stats) NewCombatCfg(CombatConfig cfg)
        {
            var buf = new WorldEventBuffer();
            var stats = new StatsSystem();
            var combat = new LOPCombatSystem(buf, new HealthSystem(), stats, cfg);
            return (buf, combat, stats);
        }

        private static (WorldEventBuffer buf, LOPCombatSystem combat, StatsSystem stats) NewCombat()
            => NewCombatCfg(DefaultConfig());

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
        public void IsDodged_is_deterministic_and_effectIndex_independent()
        {
            var a = NewCombat();
            var atk = Player("A", a.stats, 10, 10);
            var tgt = Player("B", a.stats, 10, 10, 100);
            bool d1 = a.combat.IsDodged(atk, tgt, 7, 999UL);
            bool d2 = a.combat.IsDodged(atk, tgt, 7, 999UL);
            Assert.AreEqual(d1, d2);   // 같은 입력 = 같은 결과 (effectIndex 파라미터 자체가 없음)
        }

        [Test]
        public void Attack_marks_landed_iff_not_dodged()
        {
            var (buf, combat, stats) = NewCombat();
            var atk = Player("A", stats, 20, 10);
            var tgt = Player("B", stats, 10, 10, 100);
            var hit = new AttackHitContext();
            combat.Attack(atk, tgt, 10, 5, 0, 12345UL, hit);

            var evt = (DamageDealtEvent)buf.Snapshot[0];
            Assert.AreEqual(!evt.isDodged, hit.Landed("B"));   // 명중 기록 == 닷지 아님
        }

        [Test]
        public void Attack_is_deterministic_for_same_inputs()
        {
            var a = NewCombat(); var b = NewCombat();
            a.combat.Attack(Player("A", a.stats, 20, 10), Player("B", a.stats, 10, 10, 100), 10, 5, 0, 12345UL, new AttackHitContext());
            b.combat.Attack(Player("A", b.stats, 20, 10), Player("B", b.stats, 10, 10, 100), 10, 5, 0, 12345UL, new AttackHitContext());

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
            combat.Attack(Player("A", stats, 20, 10), target, 10, 5, 0, 12345UL, new AttackHitContext());

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
            combat.Attack(Player("A", stats, 20, 10), target, 10, 5, 0, 12345UL, new AttackHitContext());

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

            combat.Attack(attacker, target, 10, 5, 0, 12345UL, new AttackHitContext());

            Assert.AreEqual(0, buf.Count);
            Assert.AreEqual(100, target.Get<Health>().Current);
        }

        [Test]
        public void Attack_damage_scales_with_effect_base_amount()
        {
            // 같은 시드/스탯, base만 다르게 → 회피/크리 판정 동일, 데미지는 base로 구동(MasterData 구동 확인).
            var low = NewCombat(); var high = NewCombat();
            low.combat.Attack(Player("A", low.stats, 20, 10), Player("B", low.stats, 10, 10, 1000), 10, 5, 0, 42UL, new AttackHitContext());
            high.combat.Attack(Player("A", high.stats, 20, 10), Player("B", high.stats, 10, 10, 1000), 100, 5, 0, 42UL, new AttackHitContext());

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

        [Test]
        public void Config_forces_always_dodge_when_min_is_one()
        {
            // dodge clamp [1,1] → 항상 회피
            var cfg = new CombatConfig(1f, 1f, 0.05f, 0.50f, 1.25f, 1.75f);
            var (buf, combat, stats) = NewCombatCfg(cfg);
            var target = Player("B", stats, 10, 10, 100);
            combat.Attack(Player("A", stats, 20, 10), target, 10, 5, 0, 12345UL, new AttackHitContext());
            var evt = (DamageDealtEvent)buf.Snapshot[0];
            Assert.IsTrue(evt.isDodged);
            Assert.AreEqual(0, evt.amount);
        }

        [Test]
        public void Config_forces_never_dodge_when_max_is_zero()
        {
            // dodge clamp [0,0] → 절대 회피 안 함
            var cfg = new CombatConfig(0f, 0f, 0.05f, 0.50f, 1.25f, 1.75f);
            var (buf, combat, stats) = NewCombatCfg(cfg);
            var target = Player("B", stats, 10, 10, 100);
            combat.Attack(Player("A", stats, 20, 10), target, 10, 5, 0, 12345UL, new AttackHitContext());
            var evt = (DamageDealtEvent)buf.Snapshot[0];
            Assert.IsFalse(evt.isDodged);
            Assert.Greater(evt.amount, 0);
        }
    }
}
