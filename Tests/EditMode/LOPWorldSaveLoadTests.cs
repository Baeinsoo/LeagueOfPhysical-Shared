using System.Numerics;
using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    // 월드가 자기 게임 상태(마나·상태이상)를 담고 되돌리는지. 위치·속도는 WorldBase 몫이라 여기선 안 본다.
    public class LOPWorldSaveLoadTests
    {
        private class FakeQuery : GameFramework.Physics.ICollisionQuery
        {
            public GameFramework.Physics.CollisionHit CapsuleCast(UnityEngine.Vector3 p1, UnityEngine.Vector3 p2,
                float radius, UnityEngine.Vector3 dir, float dist, int mask) => GameFramework.Physics.CollisionHit.None;
        }

        private class SpyBridge : GameFramework.World.IMotionBridge
        {
            public void SyncTransforms() { }
            public void Depenetrate(GameFramework.World.Entity e) { }
            public void Separate(GameFramework.World.Entity e) { }
            public void PushMotion(GameFramework.World.Entity e) { }
        }

        private static Entity MakeEntity(string id)
        {
            var e = new Entity(id);
            e.Add(new Simulated());
            e.Add(new GameFramework.World.Transform());
            e.Add(new Velocity());
            e.Add(new CapsuleShape(0.35f, 1.5f));
            e.Add(new Abilities());
            e.Add(new StatusEffects());
            e.Add(new Stats());
            e.Add(new Mana(100));
            return e;
        }

        private static LOPWorld MakeWorld(EntityRegistry registry)
            => new LOPWorld(registry, new WorldEventBuffer(),
                new MovementSystem(new StatsSystem(), new MotionContributionSystem()),
                new AbilitySystem(new ManaSystem()), new StatusEffectSystem(new StatsSystem()),
                new AbilityEffectExecutor(null), new KinematicMoveSystem(new FakeQuery(), ~0),
                new SpyBridge(),
                // 저장·복원만 보는 파일이라 발동은 관심 밖 — 어떤 id도 해소하지 않는 활성화기.
                new AbilityActivator(new AbilitySystem(new ManaSystem()), _ => null, registry, new WorldEventBuffer()));

        [Test]
        public void LoadState_마나를_저장한_시점으로_되돌린다()
        {
            var registry = new EntityRegistry();
            var entity = MakeEntity("a");
            registry.Add(entity);
            var world = MakeWorld(registry);

            world.SaveState(10);
            entity.Get<Mana>().Current = 5;

            Assert.IsTrue(world.LoadState(10));
            Assert.AreEqual(100, entity.Get<Mana>().Current);
        }

        [Test]
        public void LoadState_기록없는_틱이면_false다()
        {
            var registry = new EntityRegistry();
            registry.Add(MakeEntity("a"));
            var world = MakeWorld(registry);

            Assert.IsFalse(world.LoadState(7));
        }

        [Test]
        public void TryGetSavedStatusEffects_저장시점_목록을_돌려준다()
        {
            var registry = new EntityRegistry();
            var entity = MakeEntity("a");
            entity.Get<StatusEffects>().Effects.Add(new ActiveEffect(100, 20, 1, "src", "srcId"));
            registry.Add(entity);
            var world = MakeWorld(registry);

            world.SaveState(10);
            entity.Get<StatusEffects>().Effects.Clear();

            Assert.IsTrue(world.TryGetSavedStatusEffects(10, "a", out var effects));
            Assert.AreEqual(1, effects.Count);
        }
    }
}
