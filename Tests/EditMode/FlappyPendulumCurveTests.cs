using NUnit.Framework;

namespace LOP.Tests
{
    public class FlappyPendulumCurveTests
    {
        private const float Dt = 0.02f;

        [Test]
        public void 위상_0이면_0틱에_가운데다()
            => Assert.AreEqual(0f, FlappyPendulumCurve.AngleAt(55f, 2f, 0f, 0, Dt), 1e-4f);

        [Test]
        public void 사분주기에_최대_사분삼주기에_최소다()
        {
            //  주기 2초 = 100틱 → ¼ = 25틱, ¾ = 75틱.
            Assert.AreEqual(55f, FlappyPendulumCurve.AngleAt(55f, 2f, 0f, 25, Dt), 1e-3f);
            Assert.AreEqual(-55f, FlappyPendulumCurve.AngleAt(55f, 2f, 0f, 75, Dt), 1e-3f);
        }

        [Test]
        public void 한_주기_뒤엔_처음과_같다()
            => Assert.AreEqual(FlappyPendulumCurve.AngleAt(55f, 2f, 30f, 7, Dt),
                               FlappyPendulumCurve.AngleAt(55f, 2f, 30f, 107, Dt), 1e-3f);

        [Test]
        public void 위상_90이면_0틱에_최대다()
            => Assert.AreEqual(55f, FlappyPendulumCurve.AngleAt(55f, 2f, 90f, 0, Dt), 1e-3f);

        [Test]
        public void 음수_틱도_같은_식이다()
            => Assert.AreEqual(-55f, FlappyPendulumCurve.AngleAt(55f, 2f, 0f, -25, Dt), 1e-3f);

        [Test]
        public void 아주_큰_틱에서도_오차가_쌓이지_않는다()
        {
            //  Dt(0.02f)는 실제로 0.0199999995...라서 "이상적인" 0도/55도는 애초에 못 맞춘다.
            //  여기서 지키려는 건 입력 자체의 반올림을 넘어서는 오차가 안 쌓이는 것 — 그래서
            //  기대값도 이상적인 0.02가 아니라 Dt가 실제로 들고 있는 값으로 다시 계산한다
            //  (FlappyWindmillCurveTests의 아주_큰_틱 테스트와 같은 방식).
            AssertMatchesIndependentCalc(100_000_000L);
            AssertMatchesIndependentCalc(100_000_025L);
        }

        static void AssertMatchesIndependentCalc(long tick)
        {
            double cycles = tick * (double)Dt / 2.0;
            double fraction = cycles - System.Math.Floor(cycles);
            double expected = 55.0 * System.Math.Sin(2.0 * System.Math.PI * fraction);

            float actual = FlappyPendulumCurve.AngleAt(55f, 2f, 0f, tick, Dt);
            Assert.AreEqual(expected, actual, 1e-3);
        }
    }
}
