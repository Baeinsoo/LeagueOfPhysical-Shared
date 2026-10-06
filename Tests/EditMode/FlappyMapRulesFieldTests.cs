using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// 맵이 추격자·수동 대시를 켜는지. <b>기본은 둘 다 꺼짐</b>이다 — 마커가 없는 맵(지금의
    /// 라이브 맵)에서 룰이 조용히 켜져 있으면 안 된다.
    /// </summary>
    public class FlappyMapRulesFieldTests
    {
        [Test]
        public void 기본은_둘_다_꺼짐이다()
        {
            var field = new FlappyMapRulesField();

            Assert.IsFalse(field.Chaser);
            Assert.IsFalse(field.ManualDash);
        }

        [Test]
        public void Set하면_그_값으로_켜진다()
        {
            var field = new FlappyMapRulesField();

            field.Set(chaser: true, manualDash: true);

            Assert.IsTrue(field.Chaser);
            Assert.IsTrue(field.ManualDash);
        }

        [Test]
        public void 둘을_따로_켤_수_있다()
        {
            var field = new FlappyMapRulesField();

            field.Set(chaser: true, manualDash: false);

            Assert.IsTrue(field.Chaser);
            Assert.IsFalse(field.ManualDash);
        }

        [Test]
        public void Clear하면_기본값으로_되돌아간다()
        {
            //  라운드가 여러 판이면 맵을 다시 로드한다 — 안 거두면 지난 맵의 룰이 새 맵에 남는다.
            var field = new FlappyMapRulesField();
            field.Set(chaser: true, manualDash: true);

            field.Clear();

            Assert.IsFalse(field.Chaser);
            Assert.IsFalse(field.ManualDash);
        }
    }
}
