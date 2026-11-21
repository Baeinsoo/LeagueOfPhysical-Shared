using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>3차 주문 — 모양 축, 두 축 주문, 뒤집기, 속임 색, 라운드별 진행, 결승 2배.</summary>
    public class TriGrabRuleTests
    {
        static TriGrabCard Card(int word, int ink, int shape) => new TriGrabCard { Id = 1, Word = word, Ink = ink, Shape = shape };

        const int Circle = 0, Triangle = 1, Square = 2;
        const int WordCircle = 3, WordTriangle = 4, WordSquare = 5;

        [Test]
        public void 쓰인_말은_색_이름_셋과_모양_이름_셋()
        {
            Assert.AreEqual("빨강", TriGrabWords.Name(0));
            Assert.AreEqual("파랑", TriGrabWords.Name(2));
            Assert.AreEqual("원", TriGrabWords.Name(WordCircle));
            Assert.AreEqual("세모", TriGrabWords.Name(WordTriangle));
            Assert.AreEqual("네모", TriGrabWords.Name(WordSquare));
        }

        [Test]
        public void 모양_주문은_카드_모양만_본다()
        {
            var rule = new TriGrabRule(TriGrabRuleType.Shape, Triangle);
            Assert.IsTrue(rule.Matches(Card(WordSquare, 0, Triangle), 0), "쓰인 말이 '네모'여도 세모 카드면 정답");
            Assert.IsFalse(rule.Matches(Card(WordTriangle, 0, Square), 0), "'세모'라고 쓰인 네모 카드는 오답");
        }

        [Test]
        public void 글자와_모양이_같은_주문은_모양_이름이_쓰인_카드만_본다()
        {
            var rule = new TriGrabRule(TriGrabRuleType.ShapeMatch, 0);
            Assert.IsTrue(rule.Matches(Card(WordCircle, 2, Circle), 0));
            Assert.IsFalse(rule.Matches(Card(WordCircle, 2, Square), 0));
            Assert.IsFalse(rule.Matches(Card(0, 0, Circle), 0), "색 이름 카드는 모양과 같을 수 없다");
        }

        [Test]
        public void 글자와_색이_같은_주문은_모양_이름_카드를_안_센다()
        {
            var rule = new TriGrabRule(TriGrabRuleType.Match, 0);
            Assert.IsTrue(rule.Matches(Card(1, 1, Circle), 0), "양성 대조");
            // 말 번호 3(원)과 글자색 번호가 우연히 겹치지 않게, 색 이름이 아니면 무조건 오답.
            for (int ink = 0; ink < 3; ink++) Assert.IsFalse(rule.Matches(Card(WordCircle, ink, Circle), 0));
        }

        [Test]
        public void 두_축_주문은_그_모양_중에서만_고른다()
        {
            var rule = new TriGrabRule(TriGrabRuleType.Ink, 0, Triangle, false, -1);
            Assert.IsTrue(rule.Matches(Card(2, 0, Triangle), 0));
            Assert.IsFalse(rule.Matches(Card(2, 0, Square), 0), "글자색은 맞아도 모양이 다르면 오답");
            Assert.IsFalse(rule.Matches(Card(2, 1, Triangle), 0));
        }

        [Test]
        public void 뒤집힌_두_축_주문은_모양은_맞고_조건은_아닌_카드()
        {
            var rule = new TriGrabRule(TriGrabRuleType.Ink, 0, Square, true, -1);
            Assert.IsTrue(rule.Matches(Card(0, 1, Square), 0));
            Assert.IsFalse(rule.Matches(Card(1, 0, Square), 0), "빨간 글씨 네모는 오답");
            Assert.IsFalse(rule.Matches(Card(0, 1, Circle), 0), "뒤집혀도 모양 거르기는 그대로");
        }

        [Test]
        public void 글자와_색이_다른_주문은_색_이름_카드에서만_고른다()
        {
            var rule = new TriGrabRule(TriGrabRuleType.Match, 0, Triangle, true, -1);
            Assert.IsTrue(rule.Matches(Card(0, 2, Triangle), 0), "양성 대조 — '빨강'을 파란 글씨로 쓴 세모");
            Assert.IsFalse(rule.Matches(Card(1, 1, Triangle), 0), "글자와 색이 같으면 오답");
            for (int ink = 0; ink < 3; ink++)
                Assert.IsFalse(rule.Matches(Card(WordTriangle, ink, Triangle), 0), "'세모'라고 쓰인 카드는 색과 같을 수도 다를 수도 없다");
        }

        [Test]
        public void 기본_주문값은_거르기도_속임도_없다()
        {
            var rule = default(TriGrabRule);
            Assert.AreEqual(-1, rule.ShapeFilter);
            Assert.AreEqual(-1, rule.DecoyInk);
            Assert.IsFalse(rule.IsCompound);
            Assert.AreEqual(2, new TriGrabRule(TriGrabRuleType.Ink, 0, 2, false, 1).ShapeFilter, "양성 대조");
            Assert.AreEqual(1, new TriGrabRule(TriGrabRuleType.Ink, 0, 2, false, 1).DecoyInk);
        }

        [Test]
        public void 모든_주문_조합에서_정답_카드와_오답_카드가_만들어진다()
        {
            var m = new TriGrabMatch(3);
            m.AddPlayer("a");
            int checkedRules = 0;
            for (int round = 1; round <= TriGrabConfig.TotalRounds; round++)
                for (int type = 0; type < 5; type++)
                    for (int value = 0; value < TriGrabWords.Count; value++)
                        for (int shape = -1; shape < 3; shape++)
                            for (int neg = 0; neg < 2; neg++)
                            {
                                var rt = (TriGrabRuleType)type;
                                if (rt != TriGrabRuleType.Word && value >= 3) continue;
                                if (neg == 1 && shape < 0) continue;
                                if (shape >= 0 && (rt == TriGrabRuleType.Shape || rt == TriGrabRuleType.ShapeMatch)) continue;
                                m.State.Round = round;
                                m.State.Rule = new TriGrabRule(rt, value, shape, neg == 1, -1);
                                checkedRules++;
                                for (int i = 0; i < 30; i++)
                                {
                                    var good = m.MakeCardForTest(true, 1000);
                                    bool ever = false;
                                    for (int ink = 0; ink < 3; ink++) ever |= m.State.Rule.MatchesWithInk(good, ink);
                                    Assert.IsTrue(good.Cycles ? ever : m.State.Rule.Matches(good, 1000), $"정답 카드 — {m.State.Rule.KoreanText} 라운드 {round}");
                                }
                            }
            Assert.Greater(checkedRules, 200, "양성 대조 — 조합을 다 훑었다");
        }

        [Test]
        public void 일라운드_모양_주문의_정답_카드에도_모양_이름이_쓰인다()
        {
            var m = new TriGrabMatch(4);
            m.AddPlayer("a");
            m.State.Round = 1;
            m.State.Rule = new TriGrabRule(TriGrabRuleType.Shape, Triangle);
            int shapeWords = 0;
            for (int i = 0; i < 300; i++)
            {
                var c = m.MakeCardForTest(true, 0);
                Assert.IsTrue(m.State.Rule.Matches(c, 0));
                if (TriGrabWords.IsShapeWord(c.Word)) shapeWords++;
            }
            Assert.Greater(shapeWords, 60, "모양 이름이 쓰였다고 늘 오답이면 그게 공략법이 된다");
        }

        [Test]
        public void 속임_색은_판정에_안_쓰인다()
        {
            var plain = new TriGrabRule(TriGrabRuleType.Ink, 0);
            var decoy = new TriGrabRule(TriGrabRuleType.Ink, 0, -1, false, 2);
            for (int ink = 0; ink < 3; ink++)
                Assert.AreEqual(plain.Matches(Card(1, ink, 0), 0), decoy.Matches(Card(1, ink, 0), 0));
            Assert.AreEqual("빨강", decoy.ColorToken);
            Assert.AreEqual(2, decoy.ColorTokenInk, "주문 글의 '빨강'을 파랑으로 칠한다");
            Assert.AreEqual(0, plain.ColorTokenInk);
            Assert.IsNull(new TriGrabRule(TriGrabRuleType.Shape, 1).ColorToken, "모양 주문엔 칠할 색 이름이 없다");
        }

        [Test]
        public void 주문_한국어_글_3차()
        {
            Assert.AreEqual("세모 카드", new TriGrabRule(TriGrabRuleType.Shape, Triangle).KoreanText);
            Assert.AreEqual("글자와 모양이 같은 카드", new TriGrabRule(TriGrabRuleType.ShapeMatch, 0).KoreanText);
            Assert.AreEqual("'세모'라고 쓰인 카드", new TriGrabRule(TriGrabRuleType.Word, WordTriangle).KoreanText);
            Assert.AreEqual("'원'이라고 쓰인 카드", new TriGrabRule(TriGrabRuleType.Word, WordCircle).KoreanText);
            Assert.AreEqual("글자색이 빨강인 세모", new TriGrabRule(TriGrabRuleType.Ink, 0, Triangle, false, -1).KoreanText);
            Assert.AreEqual("글자색이 파랑이 아닌 네모", new TriGrabRule(TriGrabRuleType.Ink, 2, Square, true, -1).KoreanText);
            Assert.AreEqual("'노랑'이라고 쓰이지 않은 원", new TriGrabRule(TriGrabRuleType.Word, 1, Circle, true, -1).KoreanText);
            Assert.AreEqual("글자와 색이 다른 세모", new TriGrabRule(TriGrabRuleType.Match, 0, Triangle, true, -1).KoreanText);
            Assert.AreEqual("글자와 색이 같은 원", new TriGrabRule(TriGrabRuleType.Match, 0, Circle, false, -1).KoreanText);
        }

        [Test]
        public void 결승_라운드는_점수_2배()
        {
            Assert.AreEqual(10, TriGrabConfig.RoundPlacePoints(1, 1));
            Assert.AreEqual(9, TriGrabConfig.RoundPlacePoints(4, 2));
            Assert.AreEqual(20, TriGrabConfig.RoundPlacePoints(TriGrabConfig.TotalRounds, 1));
            Assert.AreEqual(18, TriGrabConfig.RoundPlacePoints(TriGrabConfig.TotalRounds, 2));
        }

        // 봇 4명으로 판을 끝까지 돌리며 라운드마다 주문을 모은다.
        static List<(int round, TriGrabRule rule)> Orders(int seeds)
        {
            var list = new List<(int, TriGrabRule)>();
            for (int seed = 1; seed <= seeds; seed++)
            {
                var m = new TriGrabMatch(seed);
                foreach (var id in new[] { "a", "b" }) m.AddPlayer(id);
                m.Start(0);
                int seen = 0;
                for (long t = 0; t < TriGrabConfig.MatchCapTicks && m.State.Phase != TriGrabPhase.Over; t++)
                {
                    if (m.State.Round != seen)
                    {
                        seen = m.State.Round;
                        list.Add((seen, m.State.Rule));
                    }
                    m.Tick(t);
                }
            }
            return list;
        }

        [Test]
        public void 라운드가_갈수록_주문이_어려워진다()
        {
            int r1 = 0, compoundLate = 0, negated = 0, decoyR3 = 0, tokenR3 = 0, shapeKinds = 0;
            foreach (var (round, rule) in Orders(60))
            {
                if (round == 1)
                {
                    r1++;
                    Assert.IsFalse(rule.IsCompound, "1라운드는 한 축");
                    Assert.AreEqual(-1, rule.DecoyInk, "1라운드는 속이지 않는다");
                    Assert.AreNotEqual(TriGrabRuleType.Match, rule.Type);
                    Assert.AreNotEqual(TriGrabRuleType.ShapeMatch, rule.Type);
                    if (rule.Type == TriGrabRuleType.Word) Assert.Less(rule.Color, 3, "1라운드 쓰인 말 주문은 색 이름");
                }
                else if (round <= 3)
                {
                    Assert.IsFalse(rule.IsCompound);
                    Assert.IsFalse(rule.Negate);
                    if (rule.Type == TriGrabRuleType.Shape || rule.Type == TriGrabRuleType.ShapeMatch) shapeKinds++;
                    if (round == 3 && rule.ColorToken != null) { tokenR3++; if (rule.DecoyInk >= 0) decoyR3++; }
                }
                else
                {
                    Assert.IsTrue(rule.IsCompound, $"라운드 {round}는 두 축");
                    Assert.AreNotEqual(TriGrabRuleType.Shape, rule.Type);
                    Assert.AreNotEqual(TriGrabRuleType.ShapeMatch, rule.Type);
                    if (rule.Type == TriGrabRuleType.Word) Assert.Less(rule.Color, 3);
                    compoundLate++;
                    if (rule.Negate) { negated++; Assert.AreEqual(TriGrabConfig.TotalRounds, round, "뒤집기는 결승에만"); }
                }
                if (rule.DecoyInk >= 0)
                {
                    Assert.IsNotNull(rule.ColorToken, "칠할 색 이름이 있을 때만 속인다");
                    Assert.AreNotEqual(rule.Color, rule.DecoyInk, "속임 색은 진짜 색과 다르다");
                }
            }
            Assert.AreEqual(60, r1, "양성 대조 — 판마다 1라운드");
            Assert.AreEqual(120, compoundLate, "4라운드와 결승");
            Assert.Greater(negated, 15, "결승은 반쯤 뒤집힌다");
            Assert.Greater(shapeKinds, 20, "2·3라운드엔 모양 주문도 나온다");
            Assert.Greater(tokenR3, 10);
            Assert.AreEqual(tokenR3, decoyR3, "3라운드는 색 이름이 있으면 늘 속인다");
        }

        [Test]
        public void 두_축_주문에서도_정답_카드가_충분히_뜬다()
        {
            int rounds = 0, validAtLive = 0, nearMiss = 0;
            for (int seed = 1; seed <= 40; seed++)
            {
                var m = new TriGrabMatch(seed);
                m.AddPlayer("a"); m.AddPlayer("b");
                m.Start(0);
                bool counted = false;
                for (long t = 0; t < TriGrabConfig.MatchCapTicks && m.State.Phase != TriGrabPhase.Over; t++)
                {
                    m.Tick(t);
                    if (m.State.Phase == TriGrabPhase.Prep) counted = false;
                    if (m.State.Phase != TriGrabPhase.Live || counted || !m.State.Rule.IsCompound) continue;
                    counted = true;
                    rounds++;
                    foreach (var s in m.State.Slots)
                    {
                        if (!s.Card.HasValue) continue;
                        var c = s.Card.Value;
                        if (m.State.Rule.Matches(c, t)) validAtLive++;
                        else if (c.Shape == m.State.Rule.ShapeFilter) nearMiss++;
                    }
                }
            }
            Assert.AreEqual(80, rounds, "양성 대조 — 판마다 두 축 라운드 둘");
            Assert.Greater(validAtLive / (double)rounds, 1.4, "카드가 뒤집힐 때 정답이 평균 1.4장 넘게");
            Assert.Greater(nearMiss / (double)rounds, 1.5, "모양은 맞는데 조건이 틀린 헷갈림 카드도 섞인다");
        }

        [Test]
        public void 결승에서_정답을_내면_2배_점수가_쌓인다()
        {
            var m = new TriGrabMatch(5);
            m.AddPlayer("a"); m.AddPlayer("b"); m.AddPlayer("c");
            m.Start(0);
            long t = 0;
            for (; m.State.Round < TriGrabConfig.TotalRounds && t < TriGrabConfig.MatchCapTicks; t++) m.Tick(t);
            for (; m.State.Phase != TriGrabPhase.Live; t++) m.Tick(t);
            Assert.IsTrue(TriGrabConfig.IsFinalRound(m.State.Round));

            // 정답 카드를 몸 바로 앞(슬롯 0)에 놓고 회전판을 멈춘다.
            m.State.Clock = new TriGrabRoundClock { BaseAngle0 = 0f, Omega = 0f, LiveStartTick = t, FreezeTick = long.MaxValue };
            int fire = -1;
            for (int s = 0; s < 3 && fire < 0; s++)
                for (int w = 0; w < 6 && fire < 0; w++)
                    for (int ink = 0; ink < 3; ink++)
                    {
                        var c = new TriGrabCard { Id = 9000, Word = w, Ink = ink, Shape = s };
                        if (m.State.Rule.Matches(c, t)) { m.State.Slots[0].Card = c; fire = 1; break; }
                    }
            Assert.AreEqual(1, fire, "결승 주문에 맞는 카드를 만들 수 있다");
            for (int i = 1; i < m.State.Slots.Length; i++) { m.State.Slots[i].Card = null; m.State.Slots[i].EmptyUntilTick = long.MaxValue; }

            int before = m.State.Players["a"].Score;
            var body = new Vector2(8f, 0f);
            Assert.IsTrue(m.TryFire("a", body, TriGrabMath.YawFromDir(new Vector2(-1f, 0f)), t));
            for (long k = t; k < t + 40; k++) m.Tick(k);
            Assert.AreEqual(1, m.State.Players["a"].RoundPlace);
            Assert.AreEqual(20, m.State.Players["a"].RoundPoints);
            Assert.AreEqual(before + 20, m.State.Players["a"].Score);
        }
    }
}
