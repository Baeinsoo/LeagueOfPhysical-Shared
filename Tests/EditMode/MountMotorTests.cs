using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class MountMotorTests
    {
        const float Dt = 0.02f;

        static readonly MountSettings S = new MountSettings(
            maxSpeed: 13f, acceleration: 9f, deceleration: 16f,
            turnRateStoppedDeg: 172f, turnRateAtMaxDeg: 92f,
            sharpTurnDeg: 100f, sharpTurnSpeedFactor: 0.35f, drawSpeedFactor: 0.5f, reverseSpeed: 3f);

        static MountState Run(MountState start, Vector2 stick, bool drawing, int ticks)
        {
            var state = start;
            for (int i = 0; i < ticks; i++)
            {
                state = MountMotor.Step(state, stick, drawing, S, Dt);
            }
            return state;
        }

        [Test]
        public void 같은_입력이면_같은_결과()
        {
            var a = Run(new MountState(10f, 3f), new Vector2(0.6f, 0.8f), false, 100);
            var b = Run(new MountState(10f, 3f), new Vector2(0.6f, 0.8f), false, 100);
            Assert.AreEqual(a.HeadingDeg, b.HeadingDeg);
            Assert.AreEqual(a.Speed, b.Speed);
        }

        [Test]
        public void 스틱_위면_말_앞으로_가속한다()
        {
            var one = MountMotor.Step(new MountState(90f, 0f), new Vector2(0f, 1f), false, S, Dt);
            Assert.AreEqual(0.18f, one.Speed, 1e-4f);
            Assert.AreEqual(90f, one.HeadingDeg, 1e-4f);

            var v = MountMotor.VelocityOf(one);
            Assert.AreEqual(0.18f, v.x, 1e-4f);
            Assert.AreEqual(0f, v.z, 1e-4f);
        }

        [Test]
        public void 좌우는_제자리에서도_돈다()
        {
            var right = MountMotor.Step(new MountState(0f, 0f), new Vector2(1f, 0f), false, S, Dt);
            Assert.AreEqual(172f * Dt, right.HeadingDeg, 1e-3f);
            Assert.AreEqual(0.18f, right.Speed, 1e-4f, "옆으로 밀면 돌면서 앞으로도 나간다");

            var left = MountMotor.Step(new MountState(0f, 0f), new Vector2(-1f, 0f), false, S, Dt);
            Assert.AreEqual(-172f * Dt, left.HeadingDeg, 1e-3f);
        }

        [Test]
        public void 빠를수록_덜_돈다()
        {
            var next = MountMotor.Step(new MountState(0f, 13f), new Vector2(1f, 0f), false, S, Dt);
            Assert.AreEqual(92f * Dt, next.HeadingDeg, 1e-3f);
        }

        [Test]
        public void 가속은_최고_속도에서_멈춘다()
        {
            var many = Run(new MountState(0f, 0f), new Vector2(0f, 1f), false, 1000);
            Assert.AreEqual(13f, many.Speed, 0f);
        }

        [Test]
        public void 스틱을_약하게_밀면_그만큼만_빨라진다()
        {
            var many = Run(new MountState(0f, 0f), new Vector2(0f, 0.5f), false, 1000);
            Assert.AreEqual(6.5f, many.Speed, 1e-3f);
        }

        [Test]
        public void 당기면_목표_속도가_절반()
        {
            var many = Run(new MountState(0f, 0f), new Vector2(0f, 1f), true, 1000);
            Assert.AreEqual(6.5f, many.Speed, 1e-3f);
        }

        [Test]
        public void 스틱을_놓으면_감속하고_방향은_그대로()
        {
            var next = MountMotor.Step(new MountState(30f, 13f), Vector2.zero, false, S, Dt);
            Assert.AreEqual(30f, next.HeadingDeg, 1e-4f);
            Assert.AreEqual(12.68f, next.Speed, 1e-4f);
        }

        [Test]
        public void 달리다_아래로_당기면_먼저_멈춘다()
        {
            var state = new MountState(0f, 13f);
            float prev = state.Speed;
            for (int i = 0; i < 100 && state.Speed > 0.1f; i++)
            {
                state = MountMotor.Step(state, new Vector2(0f, -1f), false, S, Dt);
                Assert.Less(state.Speed, prev, "브레이크 중에는 계속 줄어든다");
                Assert.GreaterOrEqual(state.Speed, 0f, "멈추기 전에 뒤로 넘어가면 안 된다");
                prev = state.Speed;
            }
            Assert.LessOrEqual(state.Speed, 0.1f);
        }

        [Test]
        public void 멈춘_뒤_아래로_당기면_천천히_후진한다()
        {
            var state = Run(new MountState(90f, 0f), new Vector2(0f, -1f), false, 1000);
            Assert.AreEqual(-3f, state.Speed, 1e-3f);

            var v = MountMotor.VelocityOf(state);
            Assert.AreEqual(-3f, v.x, 1e-3f, "머리가 +x를 보면 후진은 -x");
        }

        [Test]
        public void 후진_중_위로_밀면_먼저_멈췄다가_앞으로_간다()
        {
            var state = new MountState(0f, -3f);
            state = MountMotor.Step(state, new Vector2(0f, 1f), false, S, Dt);
            Assert.Less(state.Speed, 0f);
            Assert.Greater(state.Speed, -3f);

            state = Run(state, new Vector2(0f, 1f), false, 1000);
            Assert.AreEqual(13f, state.Speed, 1e-3f);
        }

        [Test]
        public void 후진_속도는_당기면_절반()
        {
            var state = Run(new MountState(0f, 0f), new Vector2(0f, -1f), true, 1000);
            Assert.AreEqual(-1.5f, state.Speed, 1e-3f);
        }

        [Test]
        public void 옆으로_밀어도_최고_속도로_돌며_달린다()
        {
            var state = new MountState(0f, 13f);
            for (int i = 0; i < 20; i++)
            {
                state = MountMotor.Step(state, new Vector2(1f, 0f), false, S, Dt);
            }
            Assert.AreEqual(13f, state.Speed, 1e-3f);
            Assert.Greater(state.HeadingDeg, 20f);
        }

        [Test]
        public void 대각선은_최고_속도()
        {
            var state = Run(new MountState(0f, 0f), new Vector2(0.7071f, 0.7071f), false, 1000);
            Assert.AreEqual(13f, state.Speed, 0.01f);
        }

        [Test]
        public void 옆으로_민_작은_흔들림은_브레이크가_아니다()
        {
            var running = MountMotor.Step(new MountState(0f, 13f), new Vector2(1f, -0.05f), false, S, Dt);
            Assert.AreEqual(13f, running.Speed, 1e-4f);

            var standing = Run(new MountState(0f, 0f), new Vector2(1f, -0.05f), false, 50);
            Assert.GreaterOrEqual(standing.Speed, 0f, "서 있을 때 옆으로 밀며 흔들려도 뒤로 기지 않는다");
        }

        [Test]
        public void 아래_쪽_원뿔만_감속한다()
        {
            var braking = MountMotor.Step(new MountState(0f, 13f), new Vector2(0.3f, -1f), false, S, Dt);
            Assert.AreEqual(13f - 16f * Dt, braking.Speed, 1e-4f);

            var sideways = MountMotor.Step(new MountState(0f, 13f), new Vector2(1f, -0.5f), false, S, Dt);
            Assert.AreEqual(13f, sideways.Speed, 1e-4f);
        }

        [Test]
        public void 후진에서_앞으로_넘어갈_때_0을_지나는_틱도_가속_한_가지로_센다()
        {
            var next = MountMotor.Step(new MountState(0f, -0.1f), new Vector2(0f, 1f), false, S, Dt);
            Assert.AreEqual(-0.1f + 9f * Dt, next.Speed, 1e-4f);
        }
    }
}
