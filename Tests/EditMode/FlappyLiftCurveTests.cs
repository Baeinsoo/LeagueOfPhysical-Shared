using NUnit.Framework;

namespace LOP.Tests
{
    public class FlappyLiftCurveTests
    {
        private const float Dt = 0.02f;

        //  진폭 2 m · 주기 2초 = 100틱.
        private static float Offset(long tick, float phase = 0f)
            => FlappyLiftCurve.OffsetAt(2f, 2f, phase, tick, Dt);

        [Test]
        public void 영틱_위상0은_가운데다() => Assert.AreEqual(0f, Offset(0), 1e-5f);

        [Test]
        public void 사분_주기에_꼭대기_사분삼에_바닥()
        {
            Assert.AreEqual(2f, Offset(25), 1e-4f);
            Assert.AreEqual(0f, Offset(50), 1e-4f);
            Assert.AreEqual(-2f, Offset(75), 1e-4f);
        }

        [Test]
        public void 위상_90이면_0틱에_꼭대기() => Assert.AreEqual(2f, Offset(0, 90f), 1e-4f);

        [Test]
        public void 한_주기_뒤엔_처음과_같다()
        {
            for (long t = 0; t < 100; t += 7)
            {
                Assert.AreEqual(Offset(t, 60f), Offset(t + 100, 60f), 1e-4f, $"tick {t}");
            }
        }

        [Test]
        public void 음수_틱도_같은_식이다() => Assert.AreEqual(-2f, Offset(-25), 1e-4f);

        [Test]
        public void 주기가_0이하면_움직이지_않는다()
        {
            Assert.AreEqual(0f, FlappyLiftCurve.OffsetAt(2f, 0f, 90f, 30, Dt));
            Assert.AreEqual(0f, FlappyLiftCurve.OffsetAt(2f, -1f, 90f, 30, Dt));
        }

        [Test]
        public void 같은_틱은_언제_물어도_같은_값이다()
        {
            //  롤백이 지난 틱을 다시 물어도 그때 값 — 사이에 다른 틱을 물어도 안 바뀐다.
            float first = Offset(1234, 30f);
            Offset(5, 30f);
            Assert.AreEqual(first, Offset(1234, 30f));
        }

        [Test]
        public void 아주_큰_틱에서도_오차가_쌓이지_않는다()
        {
            //  기대값은 Dt가 실제로 들고 있는 값(float 0.02)으로 따로 셈한다 — FlappyShutterCurveTests와 같은 방식.
            for (long t = 100_000_000L; t < 100_000_100L; t += 7)
            {
                double cycles = t * (double)Dt / 2.0;
                double frac = cycles - System.Math.Floor(cycles);
                Assert.AreEqual(2.0 * System.Math.Sin(2.0 * System.Math.PI * frac), Offset(t), 1e-3, $"tick {t}");
            }
        }
    }
}
