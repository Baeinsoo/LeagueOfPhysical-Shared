using NUnit.Framework;

namespace LOP.Tests
{
    public class CardRushLanesTests
    {
        [Test]
        public void 레인_출발점은_가운데를_기준으로_좌우()
        {
            var o0 = CardRushLanes.LaneOrigin(UnityEngine.Vector3.zero, UnityEngine.Vector3.forward, 2.6f, 0);
            var o7 = CardRushLanes.LaneOrigin(UnityEngine.Vector3.zero, UnityEngine.Vector3.forward, 2.6f, 7);
            Assert.AreEqual(-9.1f, o0.x, 1e-4f); Assert.AreEqual(9.1f, o7.x, 1e-4f); Assert.AreEqual(0f, o0.z, 1e-4f);
        }

        [Test]
        public void LaneIndexOf는_출발점과_앞으로_간_자리에서_같은_레인()
        {
            //  비스듬한 맵 — 옆 거리만 보고 앞뒤(간 거리)·높이는 버려야 한다.
            var rules = new CardRushMapRulesField();
            var forward = new UnityEngine.Vector3(1f, 0f, 2f);
            rules.Set(CardRushTheme.StoneBridge, 2.6f, new UnityEngine.Vector3(5f, 1f, -3f), forward);
            for (int lane = 0; lane < CardRushLanes.MaxLanes; lane++)
            {
                var origin = rules.LaneOrigin(lane);
                Assert.AreEqual(lane, CardRushLanes.LaneIndexOf(rules, origin), $"레인 {lane} 출발점");
                var ahead = CardRushLanes.PositionAt(origin, forward, 50_000) + UnityEngine.Vector3.up * 0.7f;
                Assert.AreEqual(lane, CardRushLanes.LaneIndexOf(rules, ahead), $"레인 {lane} 앞 50 m");
            }
            Assert.AreEqual(0, CardRushLanes.LaneIndexOf(new CardRushMapRulesField(), UnityEngine.Vector3.one), "규칙이 없으면 0");
        }

        [Test]
        public void LaneIndexOf는_반_칸_경계에서_가깝고_레인_밖은_끝_레인()
        {
            var rules = new CardRushMapRulesField();
            rules.Set(CardRushTheme.StoneBridge, 2.6f, UnityEngine.Vector3.zero, UnityEngine.Vector3.forward);
            var right = UnityEngine.Vector3.right;   // 앞 = +z면 오른쪽 = +x
            Assert.AreEqual(2, CardRushLanes.LaneIndexOf(rules, rules.LaneOrigin(2) + right * (2.6f * 0.49f)), "반 칸 조금 못 미치면 그 레인");
            Assert.AreEqual(3, CardRushLanes.LaneIndexOf(rules, rules.LaneOrigin(2) + right * (2.6f * 0.51f)), "반 칸 넘으면 옆 레인");
            Assert.AreEqual(2, CardRushLanes.LaneIndexOf(rules, rules.LaneOrigin(3) - right * (2.6f * 0.51f)));
            Assert.AreEqual(7, CardRushLanes.LaneIndexOf(rules, rules.LaneOrigin(7) + right * (2.6f * 8f)), "오른쪽 밖은 7");
            Assert.AreEqual(0, CardRushLanes.LaneIndexOf(rules, rules.LaneOrigin(0) - right * (2.6f * 8f)), "왼쪽 밖은 0");
            var noSpacing = new CardRushMapRulesField();
            noSpacing.Set(CardRushTheme.StoneBridge, 0f, UnityEngine.Vector3.zero, UnityEngine.Vector3.forward);
            Assert.AreEqual(0, CardRushLanes.LaneIndexOf(noSpacing, new UnityEngine.Vector3(5f, 0f, 0f)), "레인 간격 0이면 나누지 않고 0");
        }
    }
}
