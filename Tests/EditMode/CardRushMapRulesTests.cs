using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class CardRushMapRulesTests
    {
        private static CardRushMapRulesField Field(float laneSpacing)
        {
            var field = new CardRushMapRulesField();
            field.Set(CardRushTheme.DoorDash, laneSpacing, Vector3.zero, Vector3.forward);
            return field;
        }

        [Test]
        public void 필드_검증은_레인_간격이_0보다_커야_통과()
        {
            Assert.IsTrue(Field(2.6f).Validate(out var ok), ok);
            Assert.IsTrue(Field(0.01f).Validate(out _));
            Assert.IsFalse(Field(0f).Validate(out var e0)); Assert.IsNotEmpty(e0);
            Assert.IsFalse(Field(-1f).Validate(out var eNeg)); Assert.IsNotEmpty(eNeg);
            Assert.IsFalse(Field(float.NaN).Validate(out _), "NaN도 막는다");
        }

        [Test]
        public void 비운_필드는_규칙이_없다()
        {
            var field = Field(2.6f);
            Assert.IsTrue(field.HasRules);
            field.Clear();
            Assert.IsFalse(field.HasRules);
            Assert.IsFalse(field.Validate(out _));
        }

        [Test]
        public void 레인_출발점()
        {
            var field = new CardRushMapRulesField();
            var origin = new Vector3(3f, 1f, -5f);
            var forward = new Vector3(1f, 0.2f, 1f);
            field.Set(CardRushTheme.DoorDash, 2.6f, origin, forward);
            for (int lane = 0; lane < CardRushLanes.MaxLanes; lane++)
            {
                var want = CardRushLanes.LaneOrigin(origin, forward, 2.6f, lane);
                Assert.AreEqual(0f, Vector3.Distance(want, field.LaneOrigin(lane)), 1e-5f, $"lane {lane}");
            }
        }

        [Test]
        public void 마커_기본값은_초기값에서_온다()
        {
            //  번들은 직렬화 칸을 초기값까지 싣는다 — 초기값이 곧 맵의 기본이라 계약으로 묶어 둔다.
            var go = new GameObject("card-rush-rules-test");
            try
            {
                var rules = go.AddComponent<CardRushMapRules>();
                Assert.AreEqual(CardRushTheme.StoneBridge, rules.theme);
                Assert.AreEqual(2.6f, rules.laneSpacing, 1e-6f);

                var field = new CardRushMapRulesField();
                rules.Construct(field);
                Assert.IsTrue(field.HasRules);
                Assert.IsTrue(field.Validate(out var error), error);
                Assert.AreEqual(2.6f, field.LaneSpacing, 1e-6f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void 설정_시간()
        {
            var config = Config(0.22f, 0.4f, 4);
            Assert.AreEqual(6000, config.MatchDurationTicks);
            Assert.AreEqual(4, config.MinLanes);
            Assert.AreEqual(0.06f, config.BotHesitateChance, 1e-6f);
            Assert.Throws<System.ArgumentException>(() => Config(0.5f, 0.4f, 4));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Config(0.22f, 0.4f, 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => Config(0.22f, 0.4f, 9));
        }

        private static CardRushConfig Config(float reactMin, float reactMax, int minLanes)
            => new CardRushConfig(120, minLanes, 140, 11, 19, 0.5f, 28, 48, 1.2f, 1.39f, 0.7f, 2.77f, 1f,
                                  reactMin, reactMax, 0.04f, 0.06f, 0.5f);
    }
}
