using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    public class PredictedAbilityStateHistoryTests
    {
        private static PredictedAbilityState State()
        {
            var e = new Entity("x");
            e.Add(new Abilities());
            e.Add(new Mana(100));
            e.Add(new Stats());
            e.Add(new StatusEffects());
            return PredictedAbilityState.Capture(e);
        }

        [Test]
        public void Record_ThenTryGet_ReturnsSameState()
        {
            var h = new PredictedAbilityStateHistory(4);
            var s = State();
            h.Record(10, s);

            Assert.IsTrue(h.TryGet(10, out var got));
            Assert.AreSame(s, got);
        }

        [Test]
        public void TryGet_UnknownTick_ReturnsFalse()
        {
            var h = new PredictedAbilityStateHistory(4);
            h.Record(10, State());
            Assert.IsFalse(h.TryGet(9, out _));
        }

        [Test]
        public void Exceeding_Capacity_EvictsOldestTick()
        {
            var h = new PredictedAbilityStateHistory(4);
            for (long t = 0; t <= 4; t++) h.Record(t, State());

            Assert.IsFalse(h.TryGet(0, out _), "가장 오래된 tick 0은 밀려나야 한다");
            Assert.IsTrue(h.TryGet(1, out _));
            Assert.IsTrue(h.TryGet(4, out _));
        }

        [Test]
        public void UnrecordedTickThatMapsToDefaultSlot_ReturnsFalse()
        {
            var h = new PredictedAbilityStateHistory(4);
            h.Record(3, State());   // latest=3, 윈도우가 tick 0 포함
            Assert.IsFalse(h.TryGet(0, out _), "기록 안 된 tick 0은 sentinel과 구분돼 false");
        }
    }
}
