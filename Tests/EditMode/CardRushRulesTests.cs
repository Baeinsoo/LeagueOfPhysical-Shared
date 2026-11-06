using NUnit.Framework;

namespace LOP.Tests
{
    public class CardRushRulesTests
    {
        //  시안 1 u = 레인 75 u ↔ 2.6 m. 이 시험이 시안 값과 정수 규칙을 묶는다 — 상수를 고치면 여기서 이유를 다시 쓴다.
        private const double MmPerUnit = 2600.0 / 75.0;

        [Test]
        public void 달리기는_시안_52u_초()
            => Assert.AreEqual((int)System.Math.Round(52 * MmPerUnit * CardRushTiming.TickInterval), CardRushRules.RunMmPerTick);

        [Test]
        public void 시간_상수는_초에서_틱_규칙_그대로()
        {
            //  시안 0.45초 = 22.5틱 → 반올림 23. float 0.45f는 0.4499999…라 Ticks(0.45f)가 22를 내므로 십진으로 센다.
            Assert.AreEqual((int)System.Math.Round(0.45m / 0.02m, System.MidpointRounding.AwayFromZero), CardRushRules.CardDwellTicks);
            Assert.AreEqual(CardRushTiming.Ticks(0.4f), CardRushRules.CooldownTicks);
            Assert.AreEqual(CardRushTiming.Ticks(0.28f), CardRushRules.DashTicks);
            Assert.AreEqual(CardRushTiming.Ticks(1f), CardRushRules.BlockTicks);
        }

        [Test]
        public void 대시는_정확히_DashMm을_간다_줄어드는_걸음으로()
        {
            Assert.AreEqual(0, CardRushRules.DashOffset(0));
            Assert.AreEqual(CardRushRules.DashMm, CardRushRules.DashOffset(CardRushRules.DashTicks));
            int prevStep = int.MaxValue;
            for (int k = 1; k <= CardRushRules.DashTicks; k++)
            {
                int step = CardRushRules.DashOffset(k) - CardRushRules.DashOffset(k - 1);
                Assert.Greater(step, 0, $"k={k}");
                Assert.LessOrEqual(step, prevStep, $"k={k} — 감속 곡선");
                prevStep = step;
            }
        }

        [Test]
        public void 장애물_간격_최소는_굴러오는_것이_늦게_안_풀리는_거리()
            => Assert.AreEqual(CardRushRules.RollerRevealMm + CardRushRules.BarricadeStopMm + 500, CardRushRules.MinObstacleGapMm);
    }
}
