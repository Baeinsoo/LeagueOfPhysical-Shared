using System.Numerics;
using NUnit.Framework;

namespace LOP.Tests
{
    public class PanchigiStrikeKernelTests
    {
        private static StrikeTuning Tuning(float falloffRate = 1f)
            => new StrikeTuning(forceMultiplier: 10f, horizontalForceMultiplier: 4f, falloffRate: falloffRate);

        private static StrikeInput Strike(Vector3 point, float dragX = 1f, float dragZ = 0f, float hold = 0.5f)
            => new StrikeInput(point, new Vector3(dragX, 0f, dragZ), hold);

        [Test]
        public void 살아남은_샘플이_없으면_임펄스는_0()
        {
            var impulse = PanchigiStrikeKernel.ComputeImpulse(
                Strike(Vector3.Zero), Tuning(), new Vector3[4], liveCount: 0, totalSamples: 4);

            Assert.AreEqual(Vector3.Zero, impulse);
        }

        [Test]
        public void 전부_살아남고_타격점이_샘플과_겹치면_감쇠가_없다()
        {
            var samples = new[] { Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero };

            var impulse = PanchigiStrikeKernel.ComputeImpulse(
                Strike(Vector3.Zero, dragX: 1f, hold: 0.5f), Tuning(), samples, 4, 4);

            //  덮임 = 1이므로 힘벡터 그대로: (1*4, 0.5*10, 0*4)
            Assert.AreEqual(4f, impulse.X, 1e-4f);
            Assert.AreEqual(5f, impulse.Y, 1e-4f);
            Assert.AreEqual(0f, impulse.Z, 1e-4f);
        }

        [Test]
        public void 타격점이_멀수록_약해진다()
        {
            var samples = new[] { Vector3.Zero };

            float near = PanchigiStrikeKernel.ComputeImpulse(
                Strike(new Vector3(0.1f, 0f, 0f)), Tuning(), samples, 1, 1).Length();
            float far = PanchigiStrikeKernel.ComputeImpulse(
                Strike(new Vector3(3f, 0f, 0f)), Tuning(), samples, 1, 1).Length();

            Assert.Less(far, near);
        }

        [Test]
        public void 높이_차이는_감쇠에_영향을_주지_않는다()
        {
            //  falloff는 판 위 평면 거리로만 잰다 — 동전이 떠 있어도 세기가 안 변해야 한다.
            var flat = new[] { Vector3.Zero };
            var raised = new[] { new Vector3(0f, 5f, 0f) };

            float a = PanchigiStrikeKernel.ComputeImpulse(Strike(Vector3.Zero), Tuning(), flat, 1, 1).Length();
            float b = PanchigiStrikeKernel.ComputeImpulse(Strike(Vector3.Zero), Tuning(), raised, 1, 1).Length();

            Assert.AreEqual(a, b, 1e-4f);
        }

        [Test]
        public void 샘플_개수를_늘려도_세기가_변하지_않는다()
        {
            //  이게 원본이 못 하던 것 — gridDivisions가 세기까지 바꿔 튜닝이 불가능했다.
            var four = new[] { Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero };
            var eight = new Vector3[8];   // 전부 Vector3.Zero

            float a = PanchigiStrikeKernel.ComputeImpulse(Strike(Vector3.Zero), Tuning(), four, 4, 4).Length();
            float b = PanchigiStrikeKernel.ComputeImpulse(Strike(Vector3.Zero), Tuning(), eight, 8, 8).Length();

            Assert.AreEqual(a, b, 1e-4f);
        }

        [Test]
        public void 절반만_살아남으면_세기도_절반이다()
        {
            var samples = new[] { Vector3.Zero, Vector3.Zero, Vector3.Zero, Vector3.Zero };

            float full = PanchigiStrikeKernel.ComputeImpulse(Strike(Vector3.Zero), Tuning(), samples, 4, 4).Length();
            float half = PanchigiStrikeKernel.ComputeImpulse(Strike(Vector3.Zero), Tuning(), samples, 2, 4).Length();

            Assert.AreEqual(full * 0.5f, half, 1e-4f);
        }

        [Test]
        public void 누른_시간이_0이면_수직_성분이_없다()
        {
            var samples = new[] { Vector3.Zero };

            var impulse = PanchigiStrikeKernel.ComputeImpulse(
                Strike(Vector3.Zero, dragX: 1f, hold: 0f), Tuning(), samples, 1, 1);

            Assert.AreEqual(0f, impulse.Y, 1e-4f);
            Assert.Greater(impulse.X, 0f);
        }

        [Test]
        public void 같은_입력이면_같은_결과다()
        {
            var samples = new[] { new Vector3(0.1f, 0f, 0.2f) };

            var a = PanchigiStrikeKernel.ComputeImpulse(Strike(Vector3.Zero), Tuning(), samples, 1, 1);
            var b = PanchigiStrikeKernel.ComputeImpulse(Strike(Vector3.Zero), Tuning(), samples, 1, 1);

            Assert.AreEqual(a, b);
        }

        [Test]
        public void 샘플_개수가_0_이하면_예외()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                PanchigiStrikeKernel.ComputeImpulse(Strike(Vector3.Zero), Tuning(), new Vector3[1], 1, 0));
        }

        [Test]
        public void 샘플은_전부_발자국_원_안에_깔린다()
        {
            var center = new Vector3(2f, 0.5f, -3f);
            const float radius = 0.15f;
            var buffer = new Vector3[13];

            PanchigiStrikeKernel.BuildSamples(center, radius, buffer);

            foreach (var p in buffer)
            {
                float dx = p.X - center.X;
                float dz = p.Z - center.Z;
                Assert.LessOrEqual(System.MathF.Sqrt(dx * dx + dz * dz), radius + 1e-4f);
                Assert.AreEqual(center.Y, p.Y, 1e-4f, "샘플은 동전과 같은 높이에 깔린다");
            }
        }

        [Test]
        public void 샘플_배치는_결정론적이다()
        {
            var a = new Vector3[13];
            var b = new Vector3[13];

            PanchigiStrikeKernel.BuildSamples(Vector3.Zero, 0.15f, a);
            PanchigiStrikeKernel.BuildSamples(Vector3.Zero, 0.15f, b);

            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void 샘플이_하나여도_동작한다()
        {
            var buffer = new Vector3[1];

            PanchigiStrikeKernel.BuildSamples(Vector3.Zero, 0.15f, buffer);

            Assert.LessOrEqual(buffer[0].Length(), 0.15f + 1e-4f);
        }

        [Test]
        public void 샘플_버퍼가_비어_있으면_예외()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                PanchigiStrikeKernel.BuildSamples(Vector3.Zero, 0.15f, new Vector3[0]));
        }
    }
}
