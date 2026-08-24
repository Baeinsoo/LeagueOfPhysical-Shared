using System.Collections.Generic;
using System.Numerics;
using GameFramework;
using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    public class DamageEffectHandlerTests
    {
        private sealed class FakeQuery : GameFramework.Physics.ICollisionQuery
        {
            private readonly GameFramework.Physics.CollisionHit[] hits;

            public FakeQuery(params GameFramework.Physics.CollisionHit[] hits) { this.hits = hits; }

            public GameFramework.Physics.CollisionHit[] OverlapSphere(
                UnityEngine.Vector3 center, float radius, int layerMask) => hits;

            public GameFramework.Physics.CollisionHit CapsuleCast(
                UnityEngine.Vector3 p1, UnityEngine.Vector3 p2, float r,
                UnityEngine.Vector3 dir, float dist, int layerMask)
                => GameFramework.Physics.CollisionHit.None;

            public GameFramework.Physics.CollisionHit Raycast(
                UnityEngine.Vector3 origin, UnityEngine.Vector3 dir, float dist, int layerMask)
                => GameFramework.Physics.CollisionHit.None;
        }

        // 테스트가 쓸 가짜 몸. TearDown에서 지운다.
        private readonly List<UnityEngine.GameObject> spawnedBodies = new List<UnityEngine.GameObject>();

        private GameFramework.Physics.CollisionHit BodyHit(string entityId)
        {
            var go = new UnityEngine.GameObject(entityId);
            var collider = go.AddComponent<UnityEngine.SphereCollider>();
            go.AddComponent<EntityActor>().SetEntityId(entityId);
            spawnedBodies.Add(go);
            return new GameFramework.Physics.CollisionHit(
                true, 0f, UnityEngine.Vector3.zero, UnityEngine.Vector3.zero, collider);
        }

        [TearDown]
        public void TearDownBodies()
        {
            foreach (var go in spawnedBodies) UnityEngine.Object.DestroyImmediate(go);
            spawnedBodies.Clear();
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
                                                   StatsSystem stats, GameFramework.Physics.ICollisionQuery query)
        {
            var combat = new LOPCombatSystem(buf, new HealthSystem(), stats,
                new CombatConfig(0.05f, 0.95f, 0.05f, 0.50f, 1.25f, 1.75f));
            return new DamageEffectHandler(combat, query, new FakeSeed(12345UL), reg);
        }

        private static AbilityEffectContext Ctx(Entity caster)
            => new AbilityEffectContext(caster, null, 5L, 0, new AttackHitContext());

        [Test]
        public void Hits_target_in_front_within_sector()
        {
            var (reg, buf, stats) = World();
            var caster = Player("A", reg, stats, new Vector3(0, 0, 0));
            Player("B", reg, stats, new Vector3(0, 0, 3));   // 정면 +Z, 거리 3
            var h = Handler(reg, buf, stats, new FakeQuery(BodyHit("B")));

            h.OnActiveEnter(Ctx(caster), new DamageEffect(0, 5f, 90f));

            Assert.AreEqual(1, buf.Count);
            Assert.IsInstanceOf<DamageDealtEvent>(buf.Snapshot[0]);
        }

        [Test]
        public void Skips_self()
        {
            var (reg, buf, stats) = World();
            var caster = Player("A", reg, stats, new Vector3(0, 0, 0));
            var h = Handler(reg, buf, stats, new FakeQuery(BodyHit("A")));   // 오버랩이 자기만 반환

            h.OnActiveEnter(Ctx(caster), new DamageEffect(0, 5f, 90f));

            Assert.AreEqual(0, buf.Count);
        }

        [Test]
        public void Skips_target_behind_caster()
        {
            var (reg, buf, stats) = World();
            var caster = Player("A", reg, stats, new Vector3(0, 0, 0));
            Player("B", reg, stats, new Vector3(0, 0, -3));   // 뒤 -Z
            var h = Handler(reg, buf, stats, new FakeQuery(BodyHit("B")));

            h.OnActiveEnter(Ctx(caster), new DamageEffect(0, 5f, 90f));

            Assert.AreEqual(0, buf.Count);
        }

        [Test]
        public void Skips_target_out_of_range()
        {
            var (reg, buf, stats) = World();
            var caster = Player("A", reg, stats, new Vector3(0, 0, 0));
            Player("B", reg, stats, new Vector3(0, 0, 10));   // 정면이지만 range 5 밖
            var h = Handler(reg, buf, stats, new FakeQuery(BodyHit("B")));

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
            var h = Handler(reg, buf, stats, new FakeQuery(BodyHit("B")));

            h.OnActiveEnter(Ctx(caster), new DamageEffect(0, 5f, 90f));

            Assert.AreEqual(0, buf.Count);
        }

        [Test]
        public void Skips_unresolvable_id()
        {
            var (reg, buf, stats) = World();
            var caster = Player("A", reg, stats, new Vector3(0, 0, 0));
            var h = Handler(reg, buf, stats, new FakeQuery(BodyHit("ghost")));   // 레지스트리에 없음

            h.OnActiveEnter(Ctx(caster), new DamageEffect(0, 5f, 90f));

            Assert.AreEqual(0, buf.Count);
        }

        [Test]
        public void End_to_end_applies_damage_to_health()
        {
            var (reg, buf, stats) = World();
            var caster = Player("A", reg, stats, new Vector3(0, 0, 0));
            var target = Player("B", reg, stats, new Vector3(0, 0, 3));
            var h = Handler(reg, buf, stats, new FakeQuery(BodyHit("B")));

            h.OnActiveEnter(Ctx(caster), new DamageEffect(0, 5f, 90f));

            var evt = (DamageDealtEvent)buf.Snapshot[0];
            Assert.AreEqual("B", evt.targetId);
            if (!evt.isDodged)
                Assert.AreEqual(1000 - evt.amount, target.Get<Health>().Current);
        }

        [Test]
        public void 한_엔티티가_콜라이더를_여럿_가져도_한_번만_맞는다()
        {
            var (reg, buf, stats) = World();
            var caster = Player("A", reg, stats, new Vector3(0, 0, 0));
            Player("B", reg, stats, new Vector3(0, 0, 3));

            // 같은 엔티티를 가리키는 히트 두 개 — 몸통 콜라이더 + 모델 콜라이더인 상황.
            GameFramework.Physics.CollisionHit first = BodyHit("B");
            var extra = new UnityEngine.GameObject("weapon");
            extra.transform.SetParent(first.Collider.transform);
            var extraCollider = extra.AddComponent<UnityEngine.BoxCollider>();
            GameFramework.Physics.CollisionHit second = new GameFramework.Physics.CollisionHit(
                true, 0f, UnityEngine.Vector3.zero, UnityEngine.Vector3.zero, extraCollider);

            var h = Handler(reg, buf, stats, new FakeQuery(first, second));

            h.OnActiveEnter(Ctx(caster), new DamageEffect(0, 5f, 90f));

            int damageEvents = 0;
            foreach (var e in buf.Snapshot) if (e is DamageDealtEvent) damageEvents++;
            Assert.AreEqual(1, damageEvents, "같은 엔티티는 한 번만 맞아야 한다");
        }
    }
}
