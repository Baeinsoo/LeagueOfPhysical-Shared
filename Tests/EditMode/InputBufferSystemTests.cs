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

        //  입력 예측 — 유실로 빈 틱을 마지막으로 받은 커맨드로 메운다.
        //  0으로 메우면 "제동하라"는 능동적으로 틀린 지시가 된다(한 틱은 20ms라, 그 사이 손을 뗐을
        //  확률보다 계속 누르고 있을 확률이 압도적이다).

        [Test]
        public void PredictMissing_RepeatsLastReceivedMovement()
        {
            system.Enqueue(buffer, 10, Cmd(0, 0.8f));
            system.Consume(buffer, 10);

            var predicted = system.PredictMissing(buffer, maxTicks: 8);

            Assert.That(predicted.Horizontal, Is.EqualTo(0.8f));
            Assert.That(buffer.Current, Is.SameAs(predicted));
        }

        [Test]
        public void PredictMissing_RepeatsLastReceivedPostureAndGlide()
        {
            // Posture/Glide도 Horizontal/Vertical과 같은 축의 연속값이다 — 유실 틱에 접히면(Glide=false)
            // 하강속도가 튀고 스태미나 소모가 그 틱만 빠져 서버·클라 잔고가 갈라진다.
            system.Enqueue(buffer, 10, new InputCommand { SequenceNumber = 0, Posture = 1f, Glide = true });
            system.Consume(buffer, 10);

            var predicted = system.PredictMissing(buffer, maxTicks: 8);

            Assert.That(predicted.Posture, Is.EqualTo(1f), "자세는 이어 쓴다");
            Assert.IsTrue(predicted.Glide, "활공은 이어 쓴다 — 접히면 안 된다");
        }

        [Test]
        public void PredictMissing_DoesNotRepeatJumpOrAbility()
        {
            system.Enqueue(buffer, 10, new InputCommand { SequenceNumber = 0, Horizontal = 0.8f, Jump = true, AbilityId = 7 });
            system.Consume(buffer, 10);

            var predicted = system.PredictMissing(buffer, maxTicks: 8);

            Assert.That(predicted.Horizontal, Is.EqualTo(0.8f), "이동은 이어 쓴다");
            Assert.IsFalse(predicted.Jump, "1회성 액션은 반복하면 두 번 발동한다");
            Assert.That(predicted.AbilityId, Is.EqualTo(0));
        }

        [Test]
        public void PredictMissing_FallsBackToNeutral_AfterMaxTicks()
        {
            system.Enqueue(buffer, 10, new InputCommand { SequenceNumber = 0, Horizontal = 0.8f, Posture = 1f, Glide = true });
            system.Consume(buffer, 10);

            for (int i = 0; i < 3; i++)
            {
                var predicted = system.PredictMissing(buffer, maxTicks: 3);
                Assert.That(predicted.Horizontal, Is.EqualTo(0.8f));
                Assert.That(predicted.Posture, Is.EqualTo(1f));
                Assert.IsTrue(predicted.Glide);
            }

            var neutral = system.PredictMissing(buffer, maxTicks: 3);
            Assert.That(neutral.Horizontal, Is.EqualTo(0f),
                "상한을 넘으면 중립 — 끊긴 캐릭터가 영영 달리면 안 된다");
            Assert.That(neutral.Posture, Is.EqualTo(0f), "자세도 상한을 넘으면 중립으로 떨어진다");
            Assert.IsFalse(neutral.Glide, "활공도 상한을 넘으면 접힌다");
        }

        [Test]
        public void PredictMissing_NeutralWhenNothingReceivedYet()
        {
            Assert.That(system.PredictMissing(buffer, maxTicks: 8).Horizontal, Is.EqualTo(0f));
        }

        [Test]
        public void Consume_ResetsPredictionRun()
        {
            system.Enqueue(buffer, 10, Cmd(0, 0.8f));
            system.Consume(buffer, 10);
            system.PredictMissing(buffer, maxTicks: 3);
            system.PredictMissing(buffer, maxTicks: 3);

            system.Enqueue(buffer, 11, Cmd(1, 0.4f));
            system.Consume(buffer, 11);   // 진짜 커맨드가 오면 예측 연속 카운트는 리셋

            for (int i = 0; i < 3; i++)
            {
                Assert.That(system.PredictMissing(buffer, maxTicks: 3).Horizontal, Is.EqualTo(0.4f));
            }
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
