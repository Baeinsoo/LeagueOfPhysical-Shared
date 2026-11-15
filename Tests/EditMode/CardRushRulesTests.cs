using NUnit.Framework;

namespace LOP.Tests
{
    public class CardRushRulesTests
    {
        //  시안 1 u = 레인 75 u ↔ 2.6 m. 이 시험이 시안 값과 정수 규칙을 묶는다 — 상수를 고치면 여기서 이유를 다시 쓴다.
        private const double MmPerUnit = 2600.0 / 75.0;

        //  4.4차: 속도감 — 달리기 2.4 → 3.0 m/s(1.25배), 대시는 구덩이를 넘을 만큼 길게(5.2 m), 골드 러시는 달리기의 1.6배.
        [Test]
        public void 달리기는_초속_3m_대시는_5_2m_러시는_4_8m()
        {
            Assert.AreEqual(3000, CardRushRules.RunMmPerTick * 50, "초속 3.0 m");
            Assert.AreEqual(5200, CardRushRules.DashMm);
            Assert.AreEqual(4800, CardRushRules.RushMmPerTick * 50, "초속 4.8 m");
            Assert.Greater(CardRushRules.RushMmPerTick, CardRushRules.RunMmPerTick, "러시가 달리기보다 빠르다");
        }

        [Test]
        public void 시간_상수는_초에서_틱_규칙_그대로()
        {
            //  시안 0.45초 = 22.5틱 → 반올림 23. float 0.45f는 0.4499999…라 Ticks(0.45f)가 22를 내므로 십진으로 센다.
            Assert.AreEqual((int)System.Math.Round(0.45m / 0.02m, System.MidpointRounding.AwayFromZero), CardRushRules.CardDwellTicks);
            Assert.AreEqual(CardRushTiming.Ticks(0.4f), CardRushRules.CooldownTicks);
            Assert.AreEqual(CardRushTiming.Ticks(0.28f), CardRushRules.DashTicks);
            Assert.AreEqual(CardRushTiming.Ticks(5f), CardRushRules.LockHoldTicks, "잡은 카드 5초");
            Assert.AreEqual(CardRushTiming.Ticks(3f), CardRushRules.GoblinLifeTicks, "고블린은 3초 산다");
            Assert.AreEqual(CardRushTiming.Ticks(3f), CardRushRules.RushMaxTicks, "바로 잡으면 러시 3초");
            Assert.AreEqual(CardRushTiming.Ticks(1f), CardRushRules.RushMinTicks, "마지막에 잡으면 러시 1초");
        }

        [Test]
        public void 투사체는_15m_초()
        {
            Assert.AreEqual(15000, CardRushRules.ShotMmPerTick * 50);
        }

        [Test]
        public void 대시는_가장_넓은_구덩이보다_멀리_간다()
        {
            Assert.Greater(CardRushRules.DashMm, CardRushRules.PitWidthMaxMm + CardRushRules.PitBackMm, "다시 시도하는 자리에서 대시해도 넘는다");
        }

        [Test]
        public void 땅에서_한_틱에_가는_거리는_구덩이_폭보다_짧다()
        {
            //  대시 첫 걸음이 가장 크다 — 그래도 구덩이를 한 틱에 건너뛰면 안 된다.
            Assert.Less(CardRushRules.DashOffset(1), CardRushRules.PitWidthMinMm);
            //  고블린이 비키는 거리는 바리케이드 정지선보다 한 틱 걸음 이상 앞이어야 비킨 틱에 정지선을 못 넘는다.
            Assert.Greater(CardRushRules.GoblinYieldMm - CardRushRules.BarricadeStopMm, CardRushRules.DashOffset(1));
            Assert.Greater(CardRushRules.GoblinYieldMm - CardRushRules.BarricadeStopMm, CardRushRules.RushMmPerTick);
            Assert.Less(CardRushRules.RunMmPerTick, CardRushRules.PitWidthMinMm);
            Assert.Less(CardRushRules.RushMmPerTick, CardRushRules.PitWidthMinMm, "러시 중에도 한 틱에 구덩이를 건너뛰지 않는다");
        }

        [Test]
        public void 구덩이에서_다시_달리면_대시를_고를_41틱이_넘게_남는다()
        {
            //  달리기만 빨라지고 되돌림 거리가 그대로면 파랑을 다시 고를 시간이 줄어든다 — 2500 / 60 = 41.
            Assert.GreaterOrEqual(CardRushRules.PitBackMm / CardRushRules.RunMmPerTick, 41);
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
        public void 장애물_간격_최소는_대시_한_번_더하기_800이다()
        {
            Assert.AreEqual(CardRushRules.DashMm + 800, CardRushRules.MinObstacleGapMm);
            Assert.AreEqual(6000, CardRushRules.MinObstacleGapMm);
            Assert.Greater(CardRushRules.MinObstacleGapMm, CardRushRules.DashMm, "구덩이 앞에서 대시해 내려앉아도 다음 장애물 앞");
            Assert.GreaterOrEqual(CardRushTestTuning.Default.ObstacleGapMinMm, CardRushRules.MinObstacleGapMm);
        }

        [Test]
        public void 고블린이_사는_동안_달려도_다음_장애물에_닿지_않는다()
        {
            int runReach = CardRushRules.GoblinLifeTicks * CardRushRules.RunMmPerTick - CardRushRules.GoblinRevealMm;
            Assert.Less(runReach, CardRushRules.GoblinGapMm);
            Assert.Greater(CardRushRules.GoblinMmPerTick, 0);
            Assert.Less(CardRushRules.GoblinMmPerTick, CardRushRules.RunMmPerTick, "고블린은 달리기보다 느려 계속 가까워진다");
        }
    }
}
