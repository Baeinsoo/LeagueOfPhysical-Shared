using NUnit.Framework;

namespace LOP.Tests
{
    public class SkydiveWindReachTests
    {
        // 스펙 5.5의 값
        const float SpreadFall = 60f, SpreadMove = 12f, SpreadTurn = 22f, SpreadLag = 2.06f;
        const float DiveFall = 90f, DiveMove = 9f, DiveTurn = 6f, DiveLag = 3.10f;

        [Test]
        public void 구간을_다_덮는_강한_순풍은_다이브를_58미터_민다()
        {
            float drift = SkydiveWindReach.DriftDistance(
                windSpeed: 20f, bandHeight: 400f, fallSpeed: DiveFall, lag: DiveLag);

            Assert.AreEqual(57.8f, drift, 0.5f);
        }

        [Test]
        public void 같은_바람이_대자는_113미터_민다()
        {
            float drift = SkydiveWindReach.DriftDistance(
                windSpeed: 20f, bandHeight: 400f, fallSpeed: SpreadFall, lag: SpreadLag);

            Assert.AreEqual(112.8f, drift, 0.5f);
        }

        // 구간보다 짧게 머물면 아직 다 안 실려서, 실린 비율을 시간에 곱한 만큼만 밀린다.
        [Test]
        public void 짧은_구간은_거의_안_민다()
        {
            float drift = SkydiveWindReach.DriftDistance(
                windSpeed: 10f, bandHeight: 40f, fallSpeed: SpreadFall, lag: SpreadLag);

            Assert.AreEqual(1.08f, drift, 0.1f);
        }

        [Test]
        public void 바람이_없으면_안_민다()
        {
            Assert.AreEqual(0f, SkydiveWindReach.DriftDistance(0f, 400f, SpreadFall, SpreadLag), 1e-4f);
            Assert.AreEqual(0f, SkydiveWindReach.DriftDistance(20f, 0f, SpreadFall, SpreadLag), 1e-4f);
        }

        [Test]
        public void 대자는_한_구간에_77미터쯤_간다()
        {
            float reach = SkydiveWindReach.SelfReach(SpreadMove, SpreadTurn, dropHeight: 400f, fallSpeed: SpreadFall);

            Assert.AreEqual(76.8f, reach, 0.5f);
        }

        [Test]
        public void 다이브는_한_구간에_33미터쯤_간다()
        {
            float reach = SkydiveWindReach.SelfReach(DiveMove, DiveTurn, dropHeight: 400f, fallSpeed: DiveFall);

            Assert.AreEqual(33.2f, reach, 0.5f);
        }

        // 순풍이 목표 쪽으로 밀면 자력이 모자라도 닿는다 — 이 코스의 요점(스펙 5.4).
        [Test]
        public void 순풍을_타면_다이브도_60미터를_간다()
        {
            Assert.IsTrue(SkydiveWindReach.CanReach(
                requiredX: 0f, requiredZ: -60f, driftX: 0f, driftZ: -57.8f, selfReach: 33.2f));
        }

        [Test]
        public void 순풍이_없으면_다이브는_60미터를_못_간다()
        {
            Assert.IsFalse(SkydiveWindReach.CanReach(0f, -60f, 0f, 0f, 33.2f));
        }

        // 역풍은 밀린 거리와 필요 이동이 더해진다.
        [Test]
        public void 구간을_다_덮는_역풍은_대자도_못_지나가게_만든다()
        {
            float drift = SkydiveWindReach.DriftDistance(12f, 400f, SpreadFall, SpreadLag);

            Assert.IsFalse(SkydiveWindReach.CanReach(-55f, 0f, drift, 0f, 76.8f));
        }

        [Test]
        public void 짧게_깐_역풍은_대자가_버틴다()
        {
            float drift = SkydiveWindReach.DriftDistance(10f, 150f, SpreadFall, SpreadLag);

            Assert.IsTrue(SkydiveWindReach.CanReach(-55f, 0f, drift, 0f, 76.8f));
        }
    }
}
