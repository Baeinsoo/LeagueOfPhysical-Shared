using NUnit.Framework;

namespace LOP.Tests
{
    public class FlappyWindmillCurveTests
    {
        const float Tolerance = 1e-3f;
        const float TickSeconds = 0.02f;

        //  각도는 360에서 0으로 돌아오므로 뺄셈으로 비교하면 359.99와 0.01이 "359만큼 다르다"가 된다.
        //  둘이 가리키는 자리가 같은지를 보려면 원 위의 거리를 재야 한다.
        static float Apart(float left, float right)
        {
            float diff = System.Math.Abs(left - right) % 360f;
            return diff > 180f ? 360f - diff : diff;
        }

        [Test]
        public void 틱_0이면_시작각_그대로다()
        {
            Assert.AreEqual(45f, FlappyWindmillCurve.AngleAt(45f, 55f, 0, TickSeconds), Tolerance);
        }

        [Test]
        public void 속도가_0이면_영영_시작각에_머문다()
        {
            Assert.AreEqual(45f, FlappyWindmillCurve.AngleAt(45f, 0f, 1_000_000, TickSeconds), Tolerance);
        }

        [Test]
        public void 한_바퀴_걸리는_시간만큼_지나면_다시_시작각이다()
        {
            //  36도/초면 10초(=500틱)에 한 바퀴다.
            float angle = FlappyWindmillCurve.AngleAt(20f, 36f, 500, TickSeconds);
            Assert.Less(Apart(angle, 20f), 0.01f);
        }

        [Test]
        public void 같은_틱을_두_번_물으면_같은_답이다()
        {
            float first = FlappyWindmillCurve.AngleAt(17f, 55f, 12_345, TickSeconds);
            float second = FlappyWindmillCurve.AngleAt(17f, 55f, 12_345, TickSeconds);
            Assert.AreEqual(first, second);
        }

        [Test]
        public void 음수_틱에도_답이_있고_범위_안이다()
        {
            //  되감기가 출발 전 틱을 물을 수 있다. 예외도, 음수 각도도 나오면 안 된다.
            float angle = FlappyWindmillCurve.AngleAt(0f, 55f, -1_000, TickSeconds);
            Assert.GreaterOrEqual(angle, 0f);
            Assert.Less(angle, 360f);
        }

        [Test]
        public void 반대_방향은_같은_틱에서_거울상이다()
        {
            float forward = FlappyWindmillCurve.AngleAt(0f, 55f, 137, TickSeconds);
            float backward = FlappyWindmillCurve.AngleAt(0f, -55f, 137, TickSeconds);
            Assert.Less(Apart(forward + backward, 0f), Tolerance);
        }

        [Test]
        public void 각도는_늘_0_이상_360_미만이다()
        {
            //  시작각이 한 바퀴 밖이고 속도가 음수여도 약속한 범위 안이어야 한다.
            for (long tick = -5_000; tick <= 5_000; tick += 37)
            {
                float angle = FlappyWindmillCurve.AngleAt(-720.5f, -55f, tick, TickSeconds);
                Assert.GreaterOrEqual(angle, 0f);
                Assert.Less(angle, 360f);
            }
        }

        [Test]
        public void 아주_큰_틱에서도_정확한_값을_준다()
        {
            //  10만 틱(2000초)은 55도/초면 11만 도다. 틱마다 float로 더해 가면 그 크기에서
            //  한 눈금이 0.008도라 오차가 0.1도 단위로 쌓인다 — 곱으로 한 번에 구해야 하는 이유다.
            //  기준값은 decimal로 따로 센다(구현과 같은 식을 다시 쓰지 않는다).
            const long tick = 100_000;
            decimal exactTickSeconds = (decimal)(double)TickSeconds;   // float 0.02가 실제로 가진 값
            decimal expected = 55m * (exactTickSeconds * tick) % 360m;

            float angle = FlappyWindmillCurve.AngleAt(0f, 55f, tick, TickSeconds);
            Assert.Less(Apart(angle, (float)expected), Tolerance);
        }

        [Test]
        public void 틱마다_더해_가면_같은_정확도가_안_나온다()
        {
            //  위 테스트가 무엇을 지키는지 여기서 못박는다 — 누적 방식은 같은 허용치를 못 맞춘다.
            //  (이게 실패하면 위 테스트는 아무것도 안 지키는 초록이라는 뜻이다.)
            const long tick = 100_000;
            decimal exactTickSeconds = (decimal)(double)TickSeconds;
            decimal expected = 55m * (exactTickSeconds * tick) % 360m;

            float accumulated = 0f;
            for (long i = 0; i < tick; i++)
            {
                accumulated += 55f * TickSeconds;
            }
            accumulated %= 360f;

            Assert.Greater(Apart(accumulated, (float)expected), Tolerance);
        }
    }
}
