using NUnit.Framework;

namespace LOP.Tests
{
    public class InputBufferSystemTests
    {
        private InputBufferSystem system;
        private InputBuffer buffer;

        [SetUp]
        public void SetUp()
        {
            system = new InputBufferSystem();
            buffer = new InputBuffer();
        }

        private static InputCommand Cmd(long seq, float h = 0f) => new InputCommand { SequenceNumber = seq, Horizontal = h };

        [Test]
        public void Enqueue_StoresByTick_AndAdvancesExpectedSequence()
        {
            Assert.IsTrue(system.Enqueue(buffer, 10, Cmd(0)));
            Assert.That(buffer.Commands.Count, Is.EqualTo(1));
            Assert.That(buffer.ExpectedNextSequence, Is.EqualTo(1));
        }

        [Test]
        public void Enqueue_DedupsSameTick_AndAlreadyProcessedSequence()
        {
            system.Enqueue(buffer, 10, Cmd(0));
            Assert.IsFalse(system.Enqueue(buffer, 10, Cmd(1)), "같은 틱은 무시");

            system.Consume(buffer, 10);   // seq 0 처리됨 → LastProcessedSequence=0
            Assert.IsFalse(system.Enqueue(buffer, 11, Cmd(0)), "이미 처리된 seq는 무시");
        }

        [Test]
        public void Consume_SetsCurrentAndRemoves_MissLeavesNull()
        {
            system.Enqueue(buffer, 10, Cmd(0, 0.5f));

            var got = system.Consume(buffer, 10);
            Assert.That(got.Horizontal, Is.EqualTo(0.5f));
            Assert.That(buffer.Current, Is.SameAs(got));
            Assert.That(buffer.Commands.Count, Is.EqualTo(0), "소비하면 버퍼에서 빠진다");

            Assert.IsNull(system.Consume(buffer, 11), "없는 틱 = miss");
            Assert.IsNull(buffer.Current);
        }

        [Test]
        public void PruneBefore_DropsStale_ReturnsCount()
        {
            system.Enqueue(buffer, 5, Cmd(0));
            system.Enqueue(buffer, 6, Cmd(1));
            system.Enqueue(buffer, 8, Cmd(2));

            int pruned = system.PruneBefore(buffer, 7);
            Assert.That(pruned, Is.EqualTo(2));
            Assert.That(buffer.Commands.Count, Is.EqualTo(1));
            Assert.IsTrue(buffer.Commands.ContainsKey(8));
        }

        [Test]
        public void TrimToWindow_KeepsMostRecent()
        {
            for (long t = 1; t <= 5; t++) system.Enqueue(buffer, t, Cmd(t - 1));

            system.TrimToWindow(buffer, 3);
            Assert.That(buffer.Commands.Count, Is.EqualTo(3));
            Assert.IsTrue(buffer.Commands.ContainsKey(3));
            Assert.IsTrue(buffer.Commands.ContainsKey(5));
            Assert.IsFalse(buffer.Commands.ContainsKey(1));
        }
    }
}
