using NUnit.Framework;

namespace LOP.Tests
{
    public class FlappyShutterCurveTests
    {
        private const float Dt = 0.02f;

        //  주기 2초 = 100틱. 열림 0.4·움직임 0.2 → 닫힘 [0,20) 올라감 [20,40) 열림 [40,80) 내려옴 [80,100).
        private static float Open(long tick, float phase = 0f)
            => FlappyShutterCurve.OpenAt(2f, 0.4f, 0.2f, phase, tick, Dt);

        [Test]
        public void 닫힘_구간은_0이다()
        {
            Assert.AreEqual(0f, Open(0), 1e-4f);
            Assert.AreEqual(0f, Open(10), 1e-4f);
            Assert.AreEqual(0f, Open(19), 1e-4f);
        }

        [Test]
        public void 올라가는_구간은_곧게_0에서_1로()
        {
            Assert.AreEqual(0.25f, Open(25), 1e-3f);
            Assert.AreEqual(0.5f, Open(30), 1e-3f);
            Assert.AreEqual(0.75f, Open(35), 1e-3f);
        }

        [Test]
        public void 열림_구간은_1이다()
        {
            Assert.AreEqual(1f, Open(40), 1e-3f);
            Assert.AreEqual(1f, Open(60), 1e-4f);
            Assert.AreEqual(1f, Open(79), 1e-4f);
        }

        [Test]
        public void 내려오는_구간은_곧게_1에서_0으로()
        {
            Assert.AreEqual(0.75f, Open(85), 1e-3f);
            Assert.AreEqual(0.5f, Open(90), 1e-3f);
            Assert.AreEqual(0.25f, Open(95), 1e-3f);
        }

        [Test]
        public void 한_주기_뒤엔_처음과_같다()
        {
            for (long t = 0; t < 100; t += 7)
            {
                Assert.AreEqual(Open(t, 30f), Open(t + 100, 30f), 1e-4f, $"tick {t}");
            }
        }

        [Test]
        public void 위상_180이면_반주기_밀린다()
        {
            //  180도 = 반 주기 = 50틱 앞당김 → 0틱이 60틱(열림) 자리다.
            Assert.AreEqual(1f, Open(10, 180f), 1e-4f);
            Assert.AreEqual(0.5f, Open(40, 180f), 1e-3f);
        }

        [Test]
        public void 음수_틱도_같은_식이다()
            => Assert.AreEqual(0.5f, Open(-10), 1e-3f);

        [Test]
        public void 주기가_0이하면_닫혀_있다()
        {
            Assert.AreEqual(0f, FlappyShutterCurve.OpenAt(0f, 0.4f, 0.15f, 0f, 30, Dt));
            Assert.AreEqual(0f, FlappyShutterCurve.OpenAt(-1f, 0.4f, 0.15f, 0f, 30, Dt));
        }

        [Test]
        public void 몫이_넘쳐도_0과_1_사이다()
        {
            //  열림 0.8 + 움직임 0.2×2 = 1.2 > 1 — 닫힘이 음수가 되지 않게 잘라야 한다.
            for (long t = 0; t < 100; t++)
            {
                float v = FlappyShutterCurve.OpenAt(2f, 0.8f, 0.2f, 0f, t, Dt);
                Assert.That(v, Is.InRange(0f, 1f), $"tick {t}");
            }
            for (long t = 0; t < 100; t++)
            {
                float v = FlappyShutterCurve.OpenAt(2f, -0.5f, 0.9f, 0f, t, Dt);
                Assert.That(v, Is.InRange(0f, 1f), $"tick {t}");
            }
        }

        [Test]
        public void 아주_큰_틱에서도_오차가_쌓이지_않는다()
        {
            //  FlappyPendulumCurveTests와 같은 방식: 기대값은 Dt가 실제로 들고 있는 값으로 따로 계산한다.
            //  큰 틱 근처의 올라감·내려옴 구간(기울기가 있는 곳)을 골라야 시간 오차가 값에 드러난다.
            for (long t = 100_000_020L; t < 100_000_100L; t += 3)
            {
                AssertMatchesIndependentCalc(t);
            }
        }

        static void AssertMatchesIndependentCalc(long tick)
        {
            double cycles = tick * (double)Dt / 2.0;
            double u = cycles - System.Math.Floor(cycles);
            double expected;
            if (u < 0.2) expected = 0;
            else if (u < 0.4) expected = (u - 0.2) / 0.2;
            else if (u < 0.8) expected = 1;
            else expected = (1.0 - u) / 0.2;

            Assert.AreEqual(expected, Open(tick), 1e-3, $"tick {tick}");
        }
    }
}
