using NUnit.Framework;
using UnityEngine;

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

        [Test]
        public void 알갱이_충전은_기본_꺼짐이고_Clear하면_값도_기본으로_돌아간다()
        {
            var field = new FlappyMapRulesField();
            Assert.IsFalse(field.MoteCharge);
            Assert.AreEqual(1f / 24f, FlappyMapRulesField.DefaultMoteValue, 1e-7f, "10-23 사람 판: 24알 = 1칸");

            field.Set(chaser: false, manualDash: true, moteCharge: true, moteValue: 0.5f, moteReach: 2f);
            Assert.IsTrue(field.MoteCharge);
            Assert.AreEqual(0.5f, field.MoteValue);
            Assert.AreEqual(2f, field.MoteReach);

            field.Clear();

            Assert.IsFalse(field.MoteCharge);
            Assert.AreEqual(FlappyMapRulesField.DefaultMoteValue, field.MoteValue);
            Assert.AreEqual(FlappyMapRulesField.DefaultMoteReach, field.MoteReach);
        }

        [Test]
        public void 잃는_양은_Clear하면_기본으로_돌아간다()
        {
            var field = new FlappyMapRulesField();
            field.Set(chaser: false, manualDash: true, diveCharge: true, stunLoss: 0.5f);
            Assert.AreEqual(0.5f, field.StunLoss);

            field.Clear();

            //  지난 맵의 값이 마커 없는 다음 맵에 남으면 안 된다.
            Assert.AreEqual(FlappyMapRulesField.DefaultStunLoss, field.StunLoss);
        }

        //  박아서 잃는 양은 맵 데이터다 — 마커에 적은 값이 그대로 필드로 가야 한다(10-24, 코드 상수에서 뺌).
        [Test]
        public void 마커의_잃는_양이_필드로_간다()
        {
            var go = new GameObject("MapRules");
            try
            {
                var marker = go.AddComponent<FlappyMapRules>();
                marker.ManualDash = true;
                marker.DiveCharge = true;
                marker.StunLoss = 0.5f;
                var field = new FlappyMapRulesField();

                marker.Construct(field);

                Assert.AreEqual(0.5f, field.StunLoss, 1e-6f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        //  옛 굽기 씬엔 이 칸이 없다 — 그럴 때 유니티는 필드 초기값을 쓰므로 초기값이 지금 규칙(0.3)이어야 한다.
        [Test]
        public void 마커의_잃는_양_기본은_지금_규칙이다()
        {
            var go = new GameObject("MapRules");
            try
            {
                Assert.AreEqual(FlappyMapRulesField.DefaultStunLoss, go.AddComponent<FlappyMapRules>().StunLoss);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
