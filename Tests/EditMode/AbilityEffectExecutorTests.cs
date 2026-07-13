using GameFramework.World;
using LOP;
using NUnit.Framework;

namespace LOP.Tests
{
    public class AbilityEffectExecutorTests
    {
        /// <summary>호출 횟수만 세는 가짜 핸들러(디스패치·cadence 검증용).</summary>
        private class CountingHandler<T> : AbilityEffectHandler<T> where T : AbilityEffect
        {
            public int EnterCount;
            public int TickCount;
            protected override void OnActiveEnter(AbilityEffectContext ctx, T effect) => EnterCount++;
            protected override void OnActiveTick(AbilityEffectContext ctx, T effect) => TickCount++;
        }

        private static AbilityEffectContext Ctx() => new AbilityEffectContext(null, null, 0, 0);

        private class IndexCapturingHandler<T> : AbilityEffectHandler<T> where T : AbilityEffect
        {
            public readonly System.Collections.Generic.List<int> EnterIndices = new System.Collections.Generic.List<int>();
            protected override void OnActiveEnter(AbilityEffectContext ctx, T effect) => EnterIndices.Add(ctx.EffectIndex);
        }

        [Test]
        public void Dispatch_PassesEffectListIndexToHandler()
        {
            var damage = new IndexCapturingHandler<DamageEffect>();
            var executor = new AbilityEffectExecutor(new IAbilityEffectHandler[] { damage });
            var effects = new AbilityEffect[]
            {
                new DamageEffect(10, 1f, 90f),
                new DamageEffect(10, 1f, 90f),
                new DamageEffect(10, 1f, 90f),
            };

            executor.OnActiveEnter(new AbilityEffectContext(null, null, 0, 0), effects);

            Assert.That(damage.EnterIndices, Is.EqualTo(new[] { 0, 1, 2 }));
        }

        [Test]
        public void Dispatch_RoutesEachEffectToItsTypeHandler()
        {
            var motion = new CountingHandler<MotionEffect>();
            var damage = new CountingHandler<DamageEffect>();
            var executor = new AbilityEffectExecutor(new IAbilityEffectHandler[] { motion, damage });
            var effects = new AbilityEffect[] { new MotionEffect(5f), new DamageEffect(10, 1f, 90f) };

            executor.OnActiveEnter(Ctx(), effects);
            executor.OnActiveTick(Ctx(), effects);

            Assert.That(motion.EnterCount, Is.EqualTo(1));
            Assert.That(damage.EnterCount, Is.EqualTo(1));
            Assert.That(motion.TickCount, Is.EqualTo(1));
            Assert.That(damage.TickCount, Is.EqualTo(1));
        }

        [Test]
        public void Dispatch_SkipsUnregisteredType()
        {
            // motion만 등록 — StatusEffectApplyEffect는 핸들러 없음(서버권위 데미지를 클라가 무시하는 것과 같은 경로).
            var motion = new CountingHandler<MotionEffect>();
            var executor = new AbilityEffectExecutor(new IAbilityEffectHandler[] { motion });
            var effects = new AbilityEffect[] { new StatusEffectApplyEffect(1), new MotionEffect(5f) };

            Assert.DoesNotThrow(() => executor.OnActiveEnter(Ctx(), effects));
            Assert.That(motion.EnterCount, Is.EqualTo(1), "등록된 타입만 처리, 미등록은 무시");
        }

        [Test]
        public void NullEffects_NoThrow()
        {
            var executor = new AbilityEffectExecutor(new IAbilityEffectHandler[0]);
            Assert.DoesNotThrow(() => executor.OnActiveEnter(Ctx(), null));
            Assert.DoesNotThrow(() => executor.OnActiveTick(Ctx(), null));
        }

        [Test]
        public void DriveActiveEntity_MotionCadence_FiresEachActiveTick()
        {
            var mana = new ManaSystem();
            var motion = new CountingHandler<MotionEffect>();
            var executor = new AbilityEffectExecutor(new IAbilityEffectHandler[] { motion });
            var system = new AbilitySystem(mana);

            var e = new Entity("caster");
            e.Add(new Abilities());
            system.Grant(e, 7);

            // id7, startup0/active3/recovery0, MotionEffect 1개, 코스트 0.
            var data = new AbilityData(7, 0, 0, 0, 3, 0, new AbilityEffect[] { new MotionEffect(5f) });
            system.TryActivate(e, data, e, 0);

            // 런타임처럼: 매 틱 페이즈 전진(Tick) 후 host가 executor 구동(DriveActiveEntity).
            for (long t = 0; t <= 3; t++)
            {
                system.Tick(e, t);
                executor.DriveActiveEntity(e, t);
            }
            // t0: Startup->Active, t0==StartupEndTick → enter(1)+tick(1) / t1,t2: Active → tick / t3: Active->Recovery → 없음

            Assert.That(motion.EnterCount, Is.EqualTo(1), "진입 1회");
            Assert.That(motion.TickCount, Is.EqualTo(3), "active 3틱 동안 매 틱 push (거동 보존)");
        }
    }
}
