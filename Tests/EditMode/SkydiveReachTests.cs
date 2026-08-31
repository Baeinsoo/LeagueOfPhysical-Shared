using NUnit.Framework;

namespace LOP.Tests
{
    public class SkydiveReachTests
    {
        const float Tolerance = 0.01f;

        [Test]
        public void 최고속에_닿기_전이면_가속_구간만_적분한다()
        {
            // 낙하 1m를 1m/s로 = 1초. 가속 6이면 최고속 18에 3초 걸리므로 아직 가속 중.
            // 거리 = ½·6·1² = 3
            Assert.AreEqual(3f, SkydiveReach.MaxHorizontal(1f, 1f, moveSpeed: 18f, turnAccel: 6f), Tolerance);
        }

        [Test]
        public void 최고속에_닿은_뒤는_등속으로_이어진다()
        {
            // 낙하 5m를 1m/s로 = 5초. 최고속 18까지 3초(거리 27), 남은 2초는 등속 36.
            Assert.AreEqual(63f, SkydiveReach.MaxHorizontal(5f, 1f, moveSpeed: 18f, turnAccel: 6f), Tolerance);
        }

        [Test]
        public void 대자가_다이브보다_멀리_간다()
        {
            // 선반 간격 150m. 실제 튜닝값으로 — 대자(25 하강/12 최고속/22 가속) vs 다이브(45/18/6).
            float spread = SkydiveReach.MaxHorizontal(150f, 25f, 12f, 22f);
            float dive = SkydiveReach.MaxHorizontal(150f, 45f, 18f, 6f);

            Assert.Greater(spread, dive,
                "천천히 내려가면 옆으로 더 갈 시간이 있다 — 이 관계가 자세 선택의 이유다");
        }

        [Test]
        public void 하강_속도가_0이면_0을_돌려준다()
        {
            // 0으로 나누지 않는다. 호출자가 잘못 넣어도 코스 검사가 죽으면 안 된다.
            Assert.AreEqual(0f, SkydiveReach.MaxHorizontal(150f, 0f, 12f, 22f), Tolerance);
        }
    }
}
