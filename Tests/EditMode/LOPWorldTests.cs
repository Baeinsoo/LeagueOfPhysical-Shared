using GameFramework.World;
using LOP;
using NUnit.Framework;

namespace LOP.Tests
{
    public class LOPWorldTests
    {
        private class SpyEffect : AbilityEffect { }
        private class SpyHandler : IAbilityEffectHandler
        {
            public int enterCount;
            public System.Type EffectType => typeof(SpyEffect);
            public void OnActiveEnter(AbilityEffectContext ctx, AbilityEffect effect) => enterCount++;
            public void OnActiveTick(AbilityEffectContext ctx, AbilityEffect effect) { }
        }

        [Test]
        public void Tick_DrivesActiveAbilityEffect_ViaAbsorbedPhase()
        {
            var registry = new EntityRegistry();
            var abilitySystem = new AbilitySystem(new ManaSystem());
            var spy = new SpyHandler();
            var executor = new AbilityEffectExecutor(new IAbilityEffectHandler[] { spy });
            var world = new LOPWorld(registry, new WorldEventBuffer(),
                new MovementSystem(new StatsSystem(), new MotionContributionSystem()),
                abilitySystem, new StatusEffectSystem(new StatsSystem()), executor);

            var entity = new Entity("e1");
            entity.Add(new Abilities());
            entity.Add(new Mana(100));
            entity.Add(new Stats());
            entity.Add(new StatusEffects());
            entity.Add(new Simulated());
            registry.Add(entity);

            abilitySystem.Grant(entity, 1);
            // startup0/active1/recovery0 + SpyEffect 1개 — Active 진입 틱에 OnActiveEnter 1회
            abilitySystem.TryActivate(entity,
                new AbilityData(1, 0, 0, 0, 1, 0, new AbilityEffect[] { new SpyEffect() }), entity, 0);

            world.Tick(0, 0.05f);   // Startup(0)->Active 진입 → driveeffects 페이즈가 OnActiveEnter 호출
            Assert.That(spy.enterCount, Is.EqualTo(1), "흡수된 driveeffects 페이즈가 active 효과 구동");
        }

        [Test]
        public void Tick_ExpiresDurationEffect_ViaMutationSweep()
        {
            var registry = new EntityRegistry();
            var buffer = new WorldEventBuffer();
            var statusEffects = new StatusEffectSystem(new StatsSystem());
            var abilitySystem = new AbilitySystem(new ManaSystem());
            var world = new LOPWorld(registry, buffer, new MovementSystem(new StatsSystem(), new MotionContributionSystem()), abilitySystem, statusEffects, new AbilityEffectExecutor(null));

            var entity = new Entity("e1");
            entity.Add(new Stats());
            entity.Add(new StatusEffects());
            entity.Add(new Simulated());   // Mutation이 Has<Simulated>만 순회
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
            var statusEffects = new StatusEffectSystem(new StatsSystem());
            var world = new LOPWorld(registry, new WorldEventBuffer(),
                new MovementSystem(new StatsSystem(), new MotionContributionSystem()), new AbilitySystem(new ManaSystem()), statusEffects, new AbilityEffectExecutor(null));
            registry.Add(new Entity("bare"));   // StatusEffects/Abilities 없음

            Assert.DoesNotThrow(() => world.Tick(1, 0.05f));   // 가드로 no-op
        }

        [Test]
        public void Tick_SkipsEntitiesWithoutSimulated()
        {
            var registry = new EntityRegistry();
            var statusEffects = new StatusEffectSystem(new StatsSystem());
            var abilitySystem = new AbilitySystem(new ManaSystem());
            var world = new LOPWorld(registry, new WorldEventBuffer(),
                new MovementSystem(new StatsSystem(), new MotionContributionSystem()), abilitySystem, statusEffects, new AbilityEffectExecutor(null));

            var entity = new Entity("e1");
            entity.Add(new Stats());
            entity.Add(new StatusEffects());
            registry.Add(entity);   // Simulated 없음

            statusEffects.Apply(entity,
                new StatusEffectData(1, DurationPolicy.Duration, 5, null, StatusStackPolicy.Refresh, 1), "src", 0);

            world.Tick(5, 0.05f);   // Simulated 없으니 만료 sweep 안 돎
            Assert.That(entity.Get<StatusEffects>().Effects.Count, Is.EqualTo(1), "Simulated 없으면 틱 스킵");
        }

        [Test]
        public void Tick_AdvancesAbilityPhase_ViaMutationSweep()
        {
            var registry = new EntityRegistry();
            var statusEffects = new StatusEffectSystem(new StatsSystem());
            var abilitySystem = new AbilitySystem(new ManaSystem());
            var world = new LOPWorld(registry, new WorldEventBuffer(),
                new MovementSystem(new StatsSystem(), new MotionContributionSystem()), abilitySystem, statusEffects, new AbilityEffectExecutor(null));

            var entity = new Entity("e1");
            entity.Add(new Abilities());
            entity.Add(new Mana(100));
            entity.Add(new Stats());
            entity.Add(new StatusEffects());
            entity.Add(new Simulated());   // Mutation이 Has<Simulated>만 순회
            registry.Add(entity);

            abilitySystem.Grant(entity, 1);
            // startup0/active1/recovery0, 효과 없음 — 페이즈 전진만 검증
            abilitySystem.TryActivate(entity,
                new AbilityData(1, 0, 0, 0, 1, 0, null), entity, 0);
            Assert.That(entity.Get<Abilities>().ActiveAbility.Value.Phase, Is.EqualTo(AbilityPhase.Startup));

            world.Tick(0, 0.05f);   // Startup -> Active
            Assert.That(entity.Get<Abilities>().ActiveAbility.Value.Phase, Is.EqualTo(AbilityPhase.Active));

            world.Tick(1, 0.05f);   // Active -> Recovery
            world.Tick(2, 0.05f);   // Recovery -> Ready
            Assert.That(entity.Get<Abilities>().ActiveAbility, Is.Null);
        }
    }
}
