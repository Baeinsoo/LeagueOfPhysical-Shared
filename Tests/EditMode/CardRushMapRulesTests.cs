using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class CardRushMapRulesTests
    {
        private static CardRushMapRulesField Field(int stepCount, CardRushSection[] sections)
        {
            var field = new CardRushMapRulesField();
            field.Set(CardRushTheme.StoneBridge, stepCount, 2.2f, 2.6f, sections, Vector3.zero, Vector3.forward);
            return field;
        }

        [Test]
        public void 필드_검증()
        {
            Assert.IsTrue(Field(40, CardRushSections.Default).Validate(out var ok), ok);
            Assert.IsTrue(Field(1, CardRushSections.Default).Validate(out _));
            Assert.IsTrue(Field(200, CardRushSections.Default).Validate(out _));
            Assert.IsFalse(Field(0, CardRushSections.Default).Validate(out var e0)); Assert.IsNotEmpty(e0);
            Assert.IsFalse(Field(201, CardRushSections.Default).Validate(out var e201)); Assert.IsNotEmpty(e201);
            Assert.IsFalse(Field(40, new[] { new CardRushSection(5, 0.4f) }).Validate(out var es)); Assert.IsNotEmpty(es);
            Assert.IsFalse(Field(40, new CardRushSection[0]).Validate(out _));
        }

        [Test]
        public void 비운_필드는_규칙이_없다()
        {
            var field = Field(40, CardRushSections.Default);
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
            field.Set(CardRushTheme.DoorDash, 40, 2.2f, 2.6f, CardRushSections.Default, origin, forward);
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
                Assert.AreEqual(40, rules.stepCount);
                Assert.AreEqual(2.2f, rules.stepSpacing, 1e-6f);
                Assert.AreEqual(2.6f, rules.laneSpacing, 1e-6f);
                CollectionAssert.AreEqual(CardRushSections.Default, rules.sections);
                Assert.AreNotSame(CardRushSections.Default, rules.sections, "기본 표를 나눠 쓰면 한 마커를 고칠 때 다른 마커도 바뀐다");

                var field = new CardRushMapRulesField();
                rules.Construct(field);
                Assert.IsTrue(field.HasRules);
                Assert.IsTrue(field.Validate(out var error), error);
                Assert.AreEqual(40, field.StepCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void 설정_시간()
        {
            var config = new CardRushConfig(120, 0.3f, 0.6f, 4, 0.22f, 0.4f, 0.04f, 0.06f);
            Assert.AreEqual(15, config.Timing.MoveTicks);
            Assert.AreEqual(30, config.Timing.StunTicks);
            Assert.AreEqual(6000, config.MatchDurationTicks);
            Assert.AreEqual(4, config.MinLanes);
            Assert.AreEqual(0.06f, config.BotMissChance, 1e-6f);
            Assert.Throws<System.ArgumentException>(() => new CardRushConfig(120, 0.3f, 0.6f, 4, 0.5f, 0.4f, 0.04f, 0.06f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new CardRushConfig(120, 0.3f, 0.6f, 0, 0.22f, 0.4f, 0.04f, 0.06f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new CardRushConfig(120, 0.3f, 0.6f, 9, 0.22f, 0.4f, 0.04f, 0.06f));
        }
    }
}
