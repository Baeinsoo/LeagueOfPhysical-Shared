using NUnit.Framework;

namespace LOP.Tests
{
    public class SkydiveWindReachTests
    {
        // 스펙 5.5의 값
        const float SpreadFall = 60f, SpreadMove = 12f, SpreadTurn = 22f, SpreadLag = 2.06f;
        const float DiveFall = 90f, DiveMove = 9f, DiveTurn = 6f, DiveLag = 3.10f;

        [Test]
        public void 구간을_다_덮는_강한_순풍은_밴드_안에서만_다이브를_58미터_민다()
        {
            // tailHeight 0 — 볼륨 바로 아래가 다음 구간이라 꼬리가 펼칠 자리가 없을 때의 값.
            float drift = SkydiveWindReach.DriftDistance(
                windSpeed: 20f, bandHeight: 400f, fallSpeed: DiveFall, lag: DiveLag, tailHeight: 0f);

            Assert.AreEqual(57.8f, drift, 0.5f);
        }

        [Test]
        public void 같은_바람이_밴드_안에서만_대자는_113미터_민다()
        {
            float drift = SkydiveWindReach.DriftDistance(
                windSpeed: 20f, bandHeight: 400f, fallSpeed: SpreadFall, lag: SpreadLag, tailHeight: 0f);

            Assert.AreEqual(112.8f, drift, 0.5f);
        }

        // 구간보다 짧게 머물면 아직 다 안 실려서, 실린 비율을 시간에 곱한 만큼만 밀린다.
        [Test]
        public void 짧은_구간은_거의_안_민다()
        {
            float drift = SkydiveWindReach.DriftDistance(
                windSpeed: 10f, bandHeight: 40f, fallSpeed: SpreadFall, lag: SpreadLag, tailHeight: 0f);

            Assert.AreEqual(1.08f, drift, 0.1f);
        }

        [Test]
        public void 바람이_없으면_안_민다()
        {
            Assert.AreEqual(0f, SkydiveWindReach.DriftDistance(0f, 400f, SpreadFall, SpreadLag, 0f), 1e-4f);
            Assert.AreEqual(0f, SkydiveWindReach.DriftDistance(20f, 0f, SpreadFall, SpreadLag, 0f), 1e-4f);
        }

        // 볼륨을 벗어난 뒤로도 lag초에 걸쳐 실린 바람이 빠지며 계속 민다. 그 꼬리가 다 펼칠
        // 자리(tailHeight)가 있으면 램프인 손해(w·lag/2)와 램프아웃 꼬리(w·lag/2)가 정확히
        // 상쇄돼, 총 밀린 거리는 그냥 "풍속 × 통과시간"이 된다.
        [Test]
        public void 꼬리가_다_펼칠_자리가_있으면_풍속과_통과시간의_곱과_같다()
        {
            float time = 400f / DiveFall;
            float drift = SkydiveWindReach.DriftDistance(
                windSpeed: 20f, bandHeight: 400f, fallSpeed: DiveFall, lag: DiveLag, tailHeight: 300f);

            Assert.AreEqual(20f * time, drift, 0.5f);
        }

        // 꼬리가 다음 구간 경계에 잘리면, 밴드 안에서만 잰 값보다는 크고 꼬리가 다 펼친
        // 값(풍속×통과시간)보다는 작은 어딘가에 떨어진다.
        [Test]
        public void 꼬리가_잘리면_밴드_안_값과_풍속시간_사이에_있다()
        {
            float inBandOnly = SkydiveWindReach.DriftDistance(
                windSpeed: 20f, bandHeight: 400f, fallSpeed: DiveFall, lag: DiveLag, tailHeight: 0f);
            float fullTail = 20f * (400f / DiveFall);

            float drift = SkydiveWindReach.DriftDistance(
                windSpeed: 20f, bandHeight: 400f, fallSpeed: DiveFall, lag: DiveLag, tailHeight: 50f);

            Assert.Greater(drift, inBandOnly);
            Assert.Less(drift, fullTail);
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

        // 짧은 구간에서는 최고 속도에 닿기도 전에 끝난다 — 특히 다이브는 옆으로 가장 굼떠서
        // 그 구간 내내 가속만 하다 만다. 나머지 테스트가 전부 400m 구간이라 이 갈래를 안 지난다.
        [Test]
        public void 짧은_구간에서는_최고_속도에_닿지도_못한다()
        {
            // 통과 1.0초, 최고 속도까지 1.5초 → 내내 가속만 한다: 0.5 × 6 × 1.0² = 3.0
            float reach = SkydiveWindReach.SelfReach(DiveMove, DiveTurn, dropHeight: 90f, fallSpeed: DiveFall);

            Assert.AreEqual(3.0f, reach, 0.01f);
        }

        // 순풍이 목표 쪽으로 밀면 자력이 모자라도 닿는다 — 이 코스의 요점(스펙 5.4).
        [Test]
        public void 순풍을_타면_다이브도_60미터를_간다()
        {
            float shortfall = SkydiveWindReach.Shortfall(
                requiredX: 0f, requiredZ: -60f, driftX: 0f, driftZ: -57.8f, selfReach: 33.2f);

            //  밀려 내려간 자리에서 남은 이동은 2.2m뿐이라 33.2m 사거리가 남아돈다.
            Assert.AreEqual(-31.0f, shortfall, 0.1f);
        }

        [Test]
        public void 순풍이_없으면_다이브는_60미터를_못_간다()
        {
            float shortfall = SkydiveWindReach.Shortfall(0f, -60f, 0f, 0f, 33.2f);

            Assert.AreEqual(26.8f, shortfall, 0.1f);
        }

        // 역풍은 밀린 거리와 필요 이동이 더해진다.
        [Test]
        public void 구간을_다_덮는_역풍은_대자도_못_지나가게_만든다()
        {
            float drift = SkydiveWindReach.DriftDistance(12f, 400f, SpreadFall, SpreadLag, 0f);

            Assert.Greater(SkydiveWindReach.Shortfall(-55f, 0f, drift, 0f, 76.8f), 0f);
        }

        [Test]
        public void 짧게_깐_역풍은_대자가_버틴다()
        {
            float drift = SkydiveWindReach.DriftDistance(10f, 150f, SpreadFall, SpreadLag, 0f);

            Assert.LessOrEqual(SkydiveWindReach.Shortfall(-55f, 0f, drift, 0f, 76.8f), 0f);
        }

        // ── 옛 SkydiveReach에서 넘어온 넷 ────────────────────────────────────
        //  SkydiveReach.MaxHorizontal은 SelfReach와 대수적으로 같은 사본이었고 프로덕션
        //  사용처가 사라져 지웠다. 그 클래스가 재던 성질은 여기로 옮긴다 — 사본이 사라졌다고
        //  커버리지까지 같이 사라지면 안 된다.

        const float Tolerance = 0.01f;

        [Test]
        public void 최고속에_닿기_전이면_가속_구간만_적분한다()
        {
            // 낙하 1m를 1m/s로 = 1초. 가속 6이면 최고속 18에 3초 걸리므로 아직 가속 중.
            // 거리 = ½·6·1² = 3
            Assert.AreEqual(3f, SkydiveWindReach.SelfReach(18f, 6f, dropHeight: 1f, fallSpeed: 1f), Tolerance);
        }

        [Test]
        public void 최고속에_닿은_뒤는_등속으로_이어진다()
        {
            // 낙하 5m를 1m/s로 = 5초. 최고속 18까지 3초(거리 27), 남은 2초는 등속 36.
            Assert.AreEqual(63f, SkydiveWindReach.SelfReach(18f, 6f, dropHeight: 5f, fallSpeed: 1f), Tolerance);
        }

        [Test]
        public void 대자가_다이브보다_멀리_간다()
        {
            //  선반 간격 150m에서 실제 튜닝값으로. 옛 테스트는 "대자가 더 멀다"만 봤는데,
            //  둘 다 0을 돌려주는 구현도 그 단언을 통과한다 — 값을 못 박는다.
            //  대자: 150/25=6초, 램프 12/22=0.5455초 → 12·(6−0.2727) = 68.727
            //  다이브: 150/45=3.333초, 램프 18/6=3초 → 18·(3.3333−1.5) = 33.0
            float spread = SkydiveWindReach.SelfReach(12f, 22f, dropHeight: 150f, fallSpeed: 25f);
            float dive = SkydiveWindReach.SelfReach(18f, 6f, dropHeight: 150f, fallSpeed: 45f);

            Assert.AreEqual(68.727f, spread, Tolerance);
            Assert.AreEqual(33f, dive, Tolerance);
            Assert.Greater(spread, dive,
                "천천히 내려가면 옆으로 더 갈 시간이 있다 — 이 관계가 자세 선택의 이유다");
        }

        [Test]
        public void 하강_속도가_0이면_0을_돌려준다()
        {
            // 0으로 나누지 않는다. 호출자가 잘못 넣어도 코스 검사가 죽으면 안 된다.
            Assert.AreEqual(0f, SkydiveWindReach.SelfReach(12f, 22f, dropHeight: 150f, fallSpeed: 0f), Tolerance);
        }
    }
}
