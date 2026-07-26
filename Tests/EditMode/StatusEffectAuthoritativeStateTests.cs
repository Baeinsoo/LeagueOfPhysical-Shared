using System.Collections.Generic;
using NUnit.Framework;
using GameFramework.World;

namespace LOP.Tests.EditMode
{
    public class StatusEffectAuthoritativeStateTests
    {
        private const int SlowId = 100;
        private const int HasteId = 101;

        private static StatusEffectData Data(int id, float value) => new StatusEffectData(
            id, DurationPolicy.Duration, 60,
            new[] { new StatusModifierSpec((int)EntityStatType.MoveSpeed, value, ModifierType.PercentAdd) },
            StatusStackPolicy.Refresh, 1);

        private static StatusEffectData? Resolve(int id)
        {
            if (id == SlowId) { return Data(SlowId, -0.3f); }
            if (id == HasteId) { return Data(HasteId, 0.3f); }
            return null;
        }

        private static Entity MakeActor()
        {
            var e = new Entity("me");
            e.Add(new StatusEffects());
            var stats = new Stats();
            stats.BaseStats[(int)EntityStatType.MoveSpeed] = 10f;
            e.Add(stats);
            return e;
        }

        private static (StatusEffectSystem sys, StatsSystem stats) Build()
        {
            var statsSystem = new StatsSystem();
            return (new StatusEffectSystem(statsSystem), statsSystem);
        }

        private static List<ActiveEffect> Server(params int[] ids)
        {
            var list = new List<ActiveEffect>();
            foreach (int id in ids)
            {
                list.Add(new ActiveEffect(id, 200, 1, "server", "se:" + id));
            }
            return list;
        }

        [Test]
        public void AddsEffectTheClientDidNotPredict()
        {
            var (sys, statsSystem) = Build();
            var me = MakeActor();

            sys.ApplyAuthoritativeState(me, Server(SlowId), Resolve);

            Assert.IsTrue(me.Get<StatusEffects>().Effects.Exists(e => e.EffectId == SlowId));
            Assert.AreEqual(7f, statsSystem.GetValue(me.Get<Stats>(), (int)EntityStatType.MoveSpeed), 0.001f);
        }

        [Test]
        public void RemovesEffectTheServerNoLongerHas()
        {
            var (sys, statsSystem) = Build();
            var me = MakeActor();
            sys.Apply(me, Data(SlowId, -0.3f), "server", 0);

            sys.ApplyAuthoritativeState(me, Server(), Resolve);

            Assert.IsFalse(me.Get<StatusEffects>().Effects.Exists(e => e.EffectId == SlowId));
            // 모디파이어까지 떨어져 이동속도가 원래대로 돌아와야 한다.
            Assert.AreEqual(10f, statsSystem.GetValue(me.Get<Stats>(), (int)EntityStatType.MoveSpeed), 0.001f);
        }

        [Test]
        public void KeepsEffectBothSidesAgreeOnWithoutDoublingModifier()
        {
            var (sys, statsSystem) = Build();
            var me = MakeActor();
            sys.Apply(me, Data(HasteId, 0.3f), "me", 0);      // 클라가 예측해둔 헤이스트

            sys.ApplyAuthoritativeState(me, Server(HasteId), Resolve);

            Assert.AreEqual(1, me.Get<StatusEffects>().Effects.Count);
            // 13f — 모디파이어가 두 번 붙었다면 16f가 된다.
            Assert.AreEqual(13f, statsSystem.GetValue(me.Get<Stats>(), (int)EntityStatType.MoveSpeed), 0.001f);
        }

        [Test]
        public void AddsAndRemovesInOneCall()
        {
            var (sys, _) = Build();
            var me = MakeActor();
            sys.Apply(me, Data(HasteId, 0.3f), "me", 0);

            sys.ApplyAuthoritativeState(me, Server(SlowId), Resolve);

            var effects = me.Get<StatusEffects>().Effects;
            Assert.IsTrue(effects.Exists(e => e.EffectId == SlowId));
            Assert.IsFalse(effects.Exists(e => e.EffectId == HasteId));
        }

        [Test]
        public void UnknownEffectIdIsSkipped()
        {
            var (sys, _) = Build();
            var me = MakeActor();

            Assert.DoesNotThrow(() => sys.ApplyAuthoritativeState(me, Server(999), Resolve));
            Assert.AreEqual(0, me.Get<StatusEffects>().Effects.Count);
        }
    }
}
