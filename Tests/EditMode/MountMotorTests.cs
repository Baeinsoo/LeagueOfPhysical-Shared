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
            sharpTurnDeg: 100f, sharpTurnSpeedFactor: 0.35f, drawSpeedFactor: 0.5f);

        static MountState Run(MountState start, Vector2 desired, bool drawing, int ticks)
        {
            var state = start;
            for (int i = 0; i < ticks; i++)
            {
                state = MountMotor.Step(state, desired, drawing, S, Dt);
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
        public void 멈춰_있어도_돌기는_한다()
        {
            var next = MountMotor.Step(new MountState(0f, 0f), new Vector2(1f, 0f), false, S, Dt);
            Assert.AreEqual(172f * Dt, next.HeadingDeg, 1e-3f);
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
            var one = MountMotor.Step(new MountState(0f, 0f), new Vector2(0f, 1f), false, S, Dt);
            Assert.AreEqual(0.18f, one.Speed, 1e-4f);

            var many = Run(new MountState(0f, 0f), new Vector2(0f, 1f), false, 1000);
            Assert.AreEqual(13f, many.Speed, 0f);
        }

        [Test]
        public void 당기면_목표_속도가_절반()
        {
            var many = Run(new MountState(0f, 0f), new Vector2(0f, 1f), true, 1000);
            Assert.AreEqual(6.5f, many.Speed, 1e-3f);
        }

        [Test]
        public void 급회전이면_감속한다()
        {
            var fast = MountMotor.Step(new MountState(0f, 13f), new Vector2(0f, -1f), false, S, Dt);
            Assert.AreEqual(13f - 16f * Dt, fast.Speed, 1e-4f);

            //  목표가 정확히 13*0.35인지 — 그 속도에서는 가감속 없이 그대로여야 한다.
            var atTarget = MountMotor.Step(new MountState(0f, 4.55f), new Vector2(0f, -1f), false, S, Dt);
            Assert.AreEqual(4.55f, atTarget.Speed, 1e-4f);

            //  양성 대조 — 똑바로 가면 같은 속도에서 가속한다.
            var straight = MountMotor.Step(new MountState(0f, 4.55f), new Vector2(0f, 1f), false, S, Dt);
            Assert.AreEqual(4.55f + 9f * Dt, straight.Speed, 1e-4f);
        }

        [Test]
        public void 스틱을_놓으면_감속하고_방향은_그대로()
        {
            var next = MountMotor.Step(new MountState(30f, 13f), Vector2.zero, false, S, Dt);
            Assert.AreEqual(30f, next.HeadingDeg, 1e-4f);
            Assert.AreEqual(12.68f, next.Speed, 1e-4f);
        }

        [Test]
        public void 뒤로_가지_않는다()
        {
            var state = new MountState(0f, 0.1f);
            var inputs = new[] { Vector2.zero, new Vector2(0f, -1f), new Vector2(-1f, 0f), new Vector2(0.05f, 0.05f) };
            foreach (var input in inputs)
            {
                for (int i = 0; i < 50; i++)
                {
                    state = MountMotor.Step(state, input, i % 2 == 0, S, Dt);
                    Assert.GreaterOrEqual(state.Speed, 0f);
                }
            }

            var v = MountMotor.VelocityOf(new MountState(90f, 2f));
            Assert.AreEqual(2f, v.x, 1e-4f);
            Assert.AreEqual(0f, v.y, 1e-4f);
            Assert.AreEqual(0f, v.z, 1e-4f);
        }
    }
}
