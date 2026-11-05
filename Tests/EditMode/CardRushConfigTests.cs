using NUnit.Framework;

namespace LOP.Tests
{
    public class CardRushConfigTests
    {
        //  시작 값(spec §6.1) — 인수 하나만 바꿔 검증을 본다.
        private static CardRushConfig Make(float track = 140f, float obstacleMin = 11f, float obstacleMax = 19f, float barricadeShare = 0.5f,
                                           float zoneMin = 28f, float zoneMax = 48f, float reactMin = 0.22f, float reactMax = 0.40f,
                                           float rollKnock = 1.39f)
            => new CardRushConfig(matchDurationSeconds: 120f, minLanes: 4, trackLengthM: track,
                                  obstacleGapMinM: obstacleMin, obstacleGapMaxM: obstacleMax, barricadeShare: barricadeShare,
                                  zoneGapMinM: zoneMin, zoneGapMaxM: zoneMax, barricadeWaitSeconds: 1.2f,
                                  rollKnockM: rollKnock, rollStunSeconds: 0.7f, zoneKnockM: 2.77f, zoneStunSeconds: 1.0f,
                                  botReactMinSeconds: reactMin, botReactMaxSeconds: reactMax, botReactExtraSeconds: 0.20f,
                                  botHesitateChance: 0.02f, botFreeDashChance: 0.5f);

        [Test]
        public void 시작_값은_mm와_틱으로_한_번_바뀐다()
        {
            var t = Make().Tuning;
            Assert.AreEqual(140000, t.TrackMm);
            Assert.AreEqual(11000, t.ObstacleGapMinMm);
            Assert.AreEqual(19000, t.ObstacleGapMaxMm);
            Assert.AreEqual(500, t.BarricadePermille);
            Assert.AreEqual(28000, t.ZoneGapMinMm);
            Assert.AreEqual(48000, t.ZoneGapMaxMm);
            Assert.AreEqual(60, t.BarricadeWaitTicks);
            Assert.AreEqual(1390, t.RollKnockMm);
            Assert.AreEqual(35, t.RollStunTicks);
            Assert.AreEqual(2770, t.ZoneKnockMm);
            Assert.AreEqual(50, t.ZoneStunTicks);
        }

        [Test]
        public void 설정_값은_그대로_남는다()
        {
            var c = Make();
            Assert.AreEqual(6000, c.MatchDurationTicks);
            Assert.AreEqual(4, c.MinLanes);
            Assert.AreEqual(0.20f, c.BotReactExtraSeconds, 1e-6f);
            Assert.AreEqual(0.02f, c.BotHesitateChance, 1e-6f);
            Assert.AreEqual(0.5f, c.BotFreeDashChance, 1e-6f);
        }

        [Test]
        public void 장애물_간격_최소가_7_99m보다_작으면_예외()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Make(obstacleMin: 7.98f));
            Assert.DoesNotThrow(() => Make(obstacleMin: 7.99f));
        }

        [Test]
        public void 코스_길이는_40_190m()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Make(track: 39f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Make(track: 191f));
            Assert.DoesNotThrow(() => Make(track: 40f));
            Assert.DoesNotThrow(() => Make(track: 190f));
        }

        [Test]
        public void 간격_최대가_최소보다_작으면_예외()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Make(obstacleMin: 11f, obstacleMax: 10f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Make(zoneMin: 28f, zoneMax: 27f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Make(zoneMin: 4.9f, zoneMax: 48f));
        }

        [Test]
        public void 바리케이드_비율은_0_1()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Make(barricadeShare: 1.1f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Make(barricadeShare: -0.1f));
        }

        [Test]
        public void 벌은_0보다_커야()
            => Assert.Throws<System.ArgumentOutOfRangeException>(() => Make(rollKnock: 0f));

        [Test]
        public void 봇_반응_최소가_최대보다_크면_예외()
            => Assert.Throws<System.ArgumentException>(() => Make(reactMin: 0.5f, reactMax: 0.4f));
    }
}
