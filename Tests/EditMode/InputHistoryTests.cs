using NUnit.Framework;

namespace LOP.Tests
{
    public class InputHistoryTests
    {
        private static InputCommand Cmd(long seq) => new InputCommand { SequenceNumber = seq };

        [Test]
        public void Record_ThenTryGet_ReturnsSameCommand()
        {
            var history = new InputHistory(4);
            var c = Cmd(10);
            history.Record(10, c);

            Assert.IsTrue(history.TryGet(10, out var got));
            Assert.AreSame(c, got);
        }

        [Test]
        public void TryGet_UnknownTick_ReturnsFalse()
        {
            var history = new InputHistory(4);
            history.Record(10, Cmd(10));
            Assert.IsFalse(history.TryGet(9, out _));
        }

        [Test]
        public void Empty_TryGet_ReturnsFalse()
        {
            var history = new InputHistory(4);
            Assert.IsFalse(history.TryGet(0, out _));
        }

        [Test]
        public void Exceeding_Capacity_EvictsOldestTick()
        {
            var history = new InputHistory(4);
            for (long t = 0; t <= 4; t++) history.Record(t, Cmd(t));

            Assert.IsFalse(history.TryGet(0, out _), "가장 오래된 tick 0은 밀려나야 한다");
            Assert.IsTrue(history.TryGet(1, out _));
            Assert.IsTrue(history.TryGet(4, out _));
        }

        [Test]
        public void UnrecordedTickThatMapsToDefaultSlot_ReturnsFalse()
        {
            var history = new InputHistory(4);
            history.Record(3, Cmd(3));   // latest=3, 윈도우가 tick 0 포함
            Assert.IsFalse(history.TryGet(0, out _), "기록 안 된 tick 0은 sentinel과 구분돼 false");
        }
    }
}
