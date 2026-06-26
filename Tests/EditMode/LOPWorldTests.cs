using GameFramework.World;
using LOP;
using NUnit.Framework;

namespace LOP.Tests
{
    public class LOPWorldTests
    {
        [Test]
        public void Tick_ExpiresDurationEffect_ViaMutationSweep()
        {
            var registry = new EntityRegistry();
            var buffer = new WorldEventBuffer();
            var statusEffects = new StatusEffectSystem(new StatsSystem());
            var world = new LOPWorld(registry, buffer, statusEffects);

            var entity = new Entity("e1");
            entity.Add(new Stats());
            entity.Add(new StatusEffects());
            registry.Add(entity);

            // 5틱 지속 효과(모디파이어 없음 — 만료 경로만) 적용
            statusEffects.Apply(entity,
                new StatusEffectData(1, DurationPolicy.Duration, 5, null, StatusStackPolicy.Refresh, 1),
                "src", 0);
            Assert.That(entity.Get<StatusEffects>().Effects.Count, Is.EqualTo(1));

            world.Tick(4, 0.05f);   // 아직 만료 전
            Assert.That(entity.Get<StatusEffects>().Effects.Count, Is.EqualTo(1));

            world.Tick(5, 0.05f);   // Mutation sweep → 만료
            Assert.That(entity.Get<StatusEffects>().Effects.Count, Is.EqualTo(0));
        }

        [Test]
        public void Tick_EntityWithoutStatusEffects_NoThrow()
        {
            var registry = new EntityRegistry();
            var world = new LOPWorld(registry, new WorldEventBuffer(), new StatusEffectSystem(new StatsSystem()));
            registry.Add(new Entity("bare"));   // StatusEffects 없음

            Assert.DoesNotThrow(() => world.Tick(1, 0.05f));   // 가드로 no-op
        }
    }
}
