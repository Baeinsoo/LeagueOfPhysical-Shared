using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    // 입력에 실린 어빌리티는 월드가 읽어서 발동시킨다 — 넷코드가 아니라.
    public class LOPWorldInputActivationTests
    {
        private const int AbilityId = 1;

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

        // 효과 없이 페이즈만 도는 최소 어빌리티. 발동 성공 여부만 볼 것이라 효과는 필요 없다.
        private static AbilityData? Resolve(int id)
            => id == AbilityId
                ? (AbilityData?)new AbilityData(AbilityId, 10, 0, 2, 3, 2, new AbilityEffect[0])
                : null;

        private static LOPWorld MakeWorld(EntityRegistry registry)
        {
            var eventBuffer = new WorldEventBuffer();
            return new LOPWorld(registry, eventBuffer,
                new MovementSystem(new StatsSystem(), new MotionContributionSystem()),
                new AbilitySystem(new ManaSystem()), new StatusEffectSystem(new StatsSystem()),
                new AbilityEffectExecutor(null), new KinematicMoveSystem(new FakeQuery(), ~0),
                new SpyBridge(),
                new AbilityActivator(new AbilitySystem(new ManaSystem()), Resolve, registry, eventBuffer));
        }

        private static Entity MakeEntity(string id, bool simulated, InputCommand command)
        {
            var e = new Entity(id);
            if (simulated)
            {
                e.Add(new Simulated());
            }
            e.Add(new GameFramework.World.Transform());
            e.Add(new Velocity());
            var abilities = new Abilities();
            // 발동은 보유(부여)를 전제한다 — 부여가 없으면 CanActivate가 막아 발동 자체를 볼 수 없다.
            abilities.Granted[AbilityId] = new GrantedAbility(AbilityId, 0, 0);
            e.Add(abilities);
            e.Add(new StatusEffects());
            e.Add(new Stats());
            e.Add(new Mana(100));
            var buffer = new InputBuffer { Current = command };
            e.Add(buffer);
            return e;
        }

        [Test]
        public void Tick_입력에_어빌리티가_실려_있으면_발동한다()
        {
            var registry = new EntityRegistry();
            var entity = MakeEntity("a", true, new InputCommand { AbilityId = AbilityId });
            registry.Add(entity);

            MakeWorld(registry).Tick(1, 0.02f);

            Assert.IsNotNull(entity.Get<Abilities>().Activation);
        }

        [Test]
        public void Tick_어빌리티가_0이면_아무것도_발동하지_않는다()
        {
            var registry = new EntityRegistry();
            var entity = MakeEntity("a", true, new InputCommand());
            registry.Add(entity);

            MakeWorld(registry).Tick(1, 0.02f);

            Assert.IsNull(entity.Get<Abilities>().Activation);
        }

        [Test]
        public void Tick_Simulated가_없으면_발동하지_않는다()
        {
            // 클라에서 남의 캐릭이 입력을 들고 있어도 내가 대신 굴리면 안 된다.
            var registry = new EntityRegistry();
            var entity = MakeEntity("other", false, new InputCommand { AbilityId = AbilityId });
            registry.Add(entity);

            MakeWorld(registry).Tick(1, 0.02f);

            Assert.IsNull(entity.Get<Abilities>().Activation);
        }
    }
}
