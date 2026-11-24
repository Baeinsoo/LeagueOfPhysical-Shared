using System;
using NUnit.Framework;

namespace LOP.TriGrab.Tests
{
    public class TriGrabCardsTests
    {
        static readonly TriGrabRuleType[] Types = { TriGrabRuleType.Ink, TriGrabRuleType.Word, TriGrabRuleType.Match };

        [Test]
        public void IsOk_FollowsRule()
        {
            var ink = new TriGrabRule(TriGrabRuleType.Ink, 2, 0);
            Assert.IsTrue(TriGrabCards.IsOk(word: 0, ink: 2, ink));
            Assert.IsFalse(TriGrabCards.IsOk(word: 2, ink: 0, ink));

            var word = new TriGrabRule(TriGrabRuleType.Word, 3, 0);
            Assert.IsTrue(TriGrabCards.IsOk(word: 3, ink: 1, word));
            Assert.IsFalse(TriGrabCards.IsOk(word: 1, ink: 3, word));

            var match = new TriGrabRule(TriGrabRuleType.Match, 0, 1);
            Assert.IsTrue(TriGrabCards.IsOk(word: 2, ink: 2, match));
            Assert.IsFalse(TriGrabCards.IsOk(word: 2, ink: 1, match));
        }

        [Test]
        public void Make_NeverInvalid_And_HasEnoughOk()
        {
            var rng = new Random(12345);
            var probe = new Random(999);
            foreach (var type in Types)
            {
                for (int v = 0; v < 4; v++)
                {
                    var rule = new TriGrabRule(type, v, (v + 1) % 4);
                    int ok = 0;
                    for (int i = 0; i < 1000; i++)
                    {
                        var c = TriGrabCards.Make(rule, rng);
                        Assert.IsTrue(TriGrabCards.Valid(c.word, c.ink));
                        Assert.IsTrue(TriGrabCards.Valid(c.word, c.ink, c.fill, rule), $"{type} {c}");
                        if (TriGrabCards.IsOk(c.word, c.ink, rule)) ok++;
                    }
                    double expected = ExpectedOkShare(rule, probe);
                    Assert.AreEqual(expected, ok / 1000.0, 0.05, $"{type} v={v}");
                }
            }
        }

        // 시안 randCard 확률을 따로 옮겨 적은 몬테카를로(거부 표집 포함). 구현과 독립된 기준값이다.
        static double ExpectedOkShare(TriGrabRule R, Random r)
        {
            int ok = 0, n = 0;
            for (int i = 0; i < 40000; i++)
            {
                int ink = r.Next(4), fill = r.Next(4), wv = r.Next(4);
                if (R.Type == TriGrabRuleType.Match)
                {
                    if (r.NextDouble() < .3) ink = wv;
                    if (r.NextDouble() < .4) fill = wv;
                }
                else
                {
                    if (r.NextDouble() < .3) wv = R.Value;
                    if (r.NextDouble() < .3) ink = R.Value;
                    if (r.NextDouble() < .35) fill = R.Value;
                    if (r.NextDouble() < .12) ink = R.Lure;
                }
                if (fill == ink) continue;
                if (R.Type != TriGrabRuleType.Match && wv == ink) continue;
                n++;
                bool good = R.Type == TriGrabRuleType.Ink ? ink == R.Value
                    : R.Type == TriGrabRuleType.Word ? wv == R.Value : wv == ink;
                if (good) ok++;
            }
            return (double)ok / n;
        }

        [Test]
        public void PickRule_NeverRepeatsType()
        {
            var rng = new Random(7);
            TriGrabRule? prev = null;
            var seen = new bool[3];
            for (int i = 0; i < 300; i++)
            {
                var r = TriGrabCards.PickRule(rng, prev);
                if (prev.HasValue) Assert.AreNotEqual(prev.Value.Type, r.Type);
                Assert.AreNotEqual(r.Value, r.Lure);
                Assert.That(r.Value, Is.InRange(0, 3));
                Assert.That(r.Lure, Is.InRange(0, 3));
                seen[(int)r.Type] = true;
                prev = r;
            }
            Assert.IsTrue(seen[0] && seen[1] && seen[2]);
        }
    }
}
