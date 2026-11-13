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
            Assert.AreEqual(CardRushTiming.Ticks(5f), CardRushRules.LockHoldTicks, "잡은 카드 5초");
            Assert.AreEqual(CardRushTiming.Ticks(0.8f), CardRushRules.JumpTicks, "점프 0.8초");
        }

        [Test]
        public void 투사체는_15m_초_점프는_3m_초로_2_4m()
        {
            Assert.AreEqual(15000, CardRushRules.ShotMmPerTick * 50);
            Assert.AreEqual(3000, CardRushRules.JumpMmPerTick * 50);
            Assert.AreEqual(2400, CardRushRules.JumpMmPerTick * CardRushRules.JumpTicks);
        }

        [Test]
        public void 점프_높이는_끝점_0_가운데_최고()
        {
            Assert.AreEqual(0, CardRushRules.JumpHeightAt(0));
            Assert.AreEqual(0, CardRushRules.JumpHeightAt(CardRushRules.JumpTicks));
            Assert.AreEqual(0, CardRushRules.JumpHeightAt(-3), "범위 밖은 땅");
            Assert.AreEqual(0, CardRushRules.JumpHeightAt(CardRushRules.JumpTicks + 5));
            Assert.AreEqual(CardRushRules.JumpHeightMm, CardRushRules.JumpHeightAt(CardRushRules.JumpTicks / 2));
            for (int k = 1; k < CardRushRules.JumpTicks; k++)
            {
                Assert.Greater(CardRushRules.JumpHeightAt(k), 0, $"k={k}");
                Assert.AreEqual(CardRushRules.JumpHeightAt(k), CardRushRules.JumpHeightAt(CardRushRules.JumpTicks - k), $"k={k} 좌우 같다");
            }
        }

        [Test]
        public void 땅에서_한_틱에_가는_거리는_구덩이_폭보다_짧다()
        {
            //  대시 첫 걸음이 가장 크다 — 그래도 구덩이를 한 틱에 건너뛰면 안 된다.
            Assert.Less(CardRushRules.DashOffset(1), CardRushRules.PitWidthMinMm);
            Assert.Less(CardRushRules.RunMmPerTick, CardRushRules.PitWidthMinMm);
        }

        [Test]
        public void 가장_짧은_경고와_가장_긴_구역에서도_대시로_빠져나간다()
        {
            //  경고 동안 달린 거리 + 대시가 달리기보다 더 가는 거리 ≥ 앞 거리 + 길이 + 판정 여유 (spec §2).
            int reach = CardRushRules.RunMmPerTick * CardRushRules.ZoneWarnEndTicks
                        + CardRushRules.DashMm - CardRushRules.DashTicks * CardRushRules.RunMmPerTick;
            Assert.GreaterOrEqual(reach, CardRushRules.ZoneAheadMaxMm + CardRushRules.ZoneLengthMaxMm + CardRushRules.ZoneMarginMm);
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
        public void 장애물_간격_최소는_9390이고_투사체_사거리보다_길다()
        {
            Assert.AreEqual(9390, CardRushRules.MinObstacleGapMm);
            Assert.AreEqual(CardRushRules.RollerRevealMm + CardRushRules.BarricadeStopMm + 500 + CardRushRules.PitWidthMaxMm, CardRushRules.MinObstacleGapMm);
            Assert.Greater(CardRushRules.MinObstacleGapMm, CardRushRules.ShotRangeMm + CardRushRules.ShotStartAheadMm);
        }
    }
}
