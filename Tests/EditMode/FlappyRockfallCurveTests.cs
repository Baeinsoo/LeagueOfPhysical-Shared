using NUnit.Framework;

namespace LOP.Tests
{
    public class FlappyRockfallCurveTests
    {
        private const float Dt = 0.02f;

        //  광산 값: 주기 2초(100틱), 9 m/s, 낙하 거리 15.76(통로 14.56 + 바위 1.2) → 1.751초 동안 떨어지고 나머지는 숨는다.
        private static bool Falls(long tick, out float fallen, float phase = 0f)
            => FlappyRockfallCurve.FallingAt(2f, 9f, 15.76f, phase, tick, Dt, out fallen);

        [Test]
        public void 주기_시작에_천장에서_떨어지기_시작한다()
        {
            Assert.IsTrue(Falls(0, out float fallen));
            Assert.AreEqual(0f, fallen, 1e-4f);
        }

        [Test]
        public void 초속_9로_곧게_떨어진다()
        {
            Assert.IsTrue(Falls(50, out float fallen));     // 1.0초
            Assert.AreEqual(9f, fallen, 1e-3f);
            Assert.IsTrue(Falls(87, out fallen));           // 1.74초 — 아직 바닥 위를 지나는 중
            Assert.AreEqual(15.66f, fallen, 1e-3f);
        }

        [Test]
        public void 다_떨어진_뒤엔_숨는다()
        {
            Assert.IsFalse(Falls(88, out float fallen));    // 1.76초 → 15.84 > 15.76
            Assert.AreEqual(0f, fallen);
            Assert.IsFalse(Falls(99, out _));
        }

        [Test]
        public void 다음_주기엔_다시_천장에서()
        {
            Assert.IsTrue(Falls(100, out float fallen));
            Assert.AreEqual(0f, fallen, 1e-3f);
        }

        [Test]
        public void 위상_90도는_반_초_앞선다()
        {
            //  줄마다 +0.5초(주기 2초의 1/4 = 90°) — 시안 ph = i·0.5.
            Assert.IsTrue(Falls(0, out float fallen, 90f));
            Assert.AreEqual(4.5f, fallen, 1e-3f);
        }

        [Test]
        public void 음수_틱도_같은_식이다()
        {
            Assert.IsTrue(Falls(-50, out float fallen));
            Assert.AreEqual(9f, fallen, 1e-3f);
        }

        [Test]
        public void 주기나_속도가_0이하면_안_떨어진다()
        {
            Assert.IsFalse(FlappyRockfallCurve.FallingAt(0f, 9f, 15.76f, 0f, 10, Dt, out _));
            Assert.IsFalse(FlappyRockfallCurve.FallingAt(2f, 0f, 15.76f, 0f, 10, Dt, out _));
        }

        [Test]
        public void 아주_큰_틱에서도_오차가_쌓이지_않는다()
        {
            //  정수 틱 나눗셈이라 큰 틱에서도 소수부 오차가 없다 — 공식을 다시 계산해 맞춰보는(동어반복) 오라클 대신
            //  "주기 배수에서는 늘 천장에서 시작", "같은 주기 내 위치면 작은 틱과 완전히 같은 값"을 독립적으로 확인한다.
            const long ticksPerPeriod = 100; // 주기 2초 / 틱 0.02초
            long hugeMultiple = ticksPerPeriod * 500_000_000L; // 5억 번째 주기 시작 — 정확히 주기 배수
            Assert.IsTrue(Falls(hugeMultiple, out float fallenAtStart));
            Assert.AreEqual(0f, fallenAtStart, 0f);

            long hugeBase = 123_456_789_017L; // 주기 배수가 아닌 임의의 큰 틱
            for (long k = 0; k < 7; k++)
            {
                long bigTick = hugeBase + k;
                long samePositionSmallTick = ((bigTick % ticksPerPeriod) + ticksPerPeriod) % ticksPerPeriod;
                bool bigFalls = Falls(bigTick, out float bigFallen);
                bool smallFalls = Falls(samePositionSmallTick, out float smallFallen);
                Assert.AreEqual(smallFalls, bigFalls, $"tick {bigTick}");
                Assert.AreEqual(smallFallen, bigFallen, 0f, $"tick {bigTick}");
            }
        }
    }
}
