using System.Collections.Generic;
using NUnit.Framework;

namespace LOP.Tests.EditMode
{
    public class StatusEffectReconcileGateTests
    {
        private const int SlowId = 100;
        private const int HasteId = 101;
        private const int UnknownId = 999;

        private static StatusEffectData Data(int id) => new StatusEffectData(
            id, DurationPolicy.Duration, 60, null, StatusStackPolicy.Refresh, 1);

        private static StatusEffectData? Resolve(int id)
        {
            if (id == SlowId) { return Data(SlowId); }
            if (id == HasteId) { return Data(HasteId); }
            return null;
        }

        private static ActiveEffect Effect(int id, long expireTick, int stackCount) =>
            new ActiveEffect(id, expireTick, stackCount, "server", "se:" + id);

        [Test]
        public void IdenticalLists_NoReconcile()
        {
            var predicted = new List<ActiveEffect> { Effect(SlowId, 200, 1) };
            var server = new List<ActiveEffect> { Effect(SlowId, 200, 1) };

            Assert.IsFalse(StatusEffectReconcileGate.ShouldReconcile(predicted, server, Resolve));
        }

        [Test]
        public void IdOnlyOnServer_Resolvable_NeedsReconcile()
        {
            var predicted = new List<ActiveEffect>();
            var server = new List<ActiveEffect> { Effect(SlowId, 200, 1) };

            Assert.IsTrue(StatusEffectReconcileGate.ShouldReconcile(predicted, server, Resolve));
        }

        [Test]
        public void IdOnlyOnServer_Unresolvable_Ignored()
        {
            var predicted = new List<ActiveEffect>();
            var server = new List<ActiveEffect> { Effect(UnknownId, 200, 1) };

            Assert.IsFalse(StatusEffectReconcileGate.ShouldReconcile(predicted, server, Resolve));
        }

        [Test]
        public void IdOnlyOnClient_NeedsReconcile()
        {
            var predicted = new List<ActiveEffect> { Effect(SlowId, 200, 1) };
            var server = new List<ActiveEffect>();

            Assert.IsTrue(StatusEffectReconcileGate.ShouldReconcile(predicted, server, Resolve));
        }

        [Test]
        public void SameId_DifferentExpireTick_NeedsReconcile()
        {
            // 몬스터가 쿨다운 없이 때려 슬로우가 계속 갱신되는 상황 재현 — id 집합은 그대로,
            // 서버 만료틱만 밀린다.
            var predicted = new List<ActiveEffect> { Effect(SlowId, 200, 1) };
            var server = new List<ActiveEffect> { Effect(SlowId, 260, 1) };

            Assert.IsTrue(StatusEffectReconcileGate.ShouldReconcile(predicted, server, Resolve));
        }

        [Test]
        public void SameId_DifferentStackCount_NeedsReconcile()
        {
            var predicted = new List<ActiveEffect> { Effect(SlowId, 200, 1) };
            var server = new List<ActiveEffect> { Effect(SlowId, 200, 2) };

            Assert.IsTrue(StatusEffectReconcileGate.ShouldReconcile(predicted, server, Resolve));
        }

        [Test]
        public void NullLists_NoThrow_NoReconcile()
        {
            Assert.DoesNotThrow(() => StatusEffectReconcileGate.ShouldReconcile(null, null, Resolve));
            Assert.IsFalse(StatusEffectReconcileGate.ShouldReconcile(null, null, Resolve));
        }

        [Test]
        public void EmptyLists_NoReconcile()
        {
            var predicted = new List<ActiveEffect>();
            var server = new List<ActiveEffect>();

            Assert.IsFalse(StatusEffectReconcileGate.ShouldReconcile(predicted, server, Resolve));
        }
    }
}
