using NUnit.Framework;

namespace LOP.Tests
{
    public class CardRushCourseTests
    {
        private static CardRushCourse Make(ulong seed, int steps = 40) => new CardRushCourse(seed, steps, CardRushSections.Default);

        [Test]
        public void 같은_씨앗은_같은_줄_다른_씨앗은_다른_줄()
        {
            var a = Make(12345); var b = Make(12345); var c = Make(54321);
            bool anyDiff = false;
            for (int i = 0; i < 40; i++)
            {
                Assert.AreEqual(a.RequiredColor(i), b.RequiredColor(i));
                anyDiff |= a.RequiredColor(i) != c.RequiredColor(i);
            }
            Assert.IsTrue(anyDiff);
        }

        [Test]
        public void 같은_색_셋째_줄은_없다()
        {
            int runsOfTwo = 0;
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var course = Make(seed);
                for (int i = 2; i < 40; i++)
                {
                    bool two = course.RequiredColor(i - 1) == course.RequiredColor(i - 2);
                    if (two) runsOfTwo++;
                    Assert.IsFalse(two && course.RequiredColor(i) == course.RequiredColor(i - 1), $"seed {seed} step {i}");
                }
            }
            // 양성 대조: 둘째 줄은 허용 — 규칙이 "같은 색 금지"로 과하게 막고 있지 않다.
            Assert.Greater(runsOfTwo, 100);
        }

        [Test]
        public void 뽑는_순서는_계약이다()
        {
            // 구현 첫 통과 때 나온 값을 적어 고정한다(바꾸면 옛 클라와 줄이 갈린다). 적기 전엔 Assert.Fail로 둔다.
            var course = Make(20261102);
            var first8 = new int[8];
            for (int i = 0; i < 8; i++) first8[i] = (int)course.RequiredColor(i);
            CollectionAssert.AreEqual(new[] { 1, 1, 0, 0, 1, 2, 2, 1 }, first8);
            Assert.AreEqual(2, (int)course.StartColor(3, 1));
        }

        [Test]
        public void 구간마다_떠_있는_틱()
        {
            var course = Make(1);
            Assert.AreEqual(20, course.DwellTicks(0));
            Assert.AreEqual(20, course.DwellTicks(14));
            Assert.AreEqual(17, course.DwellTicks(15));
            Assert.AreEqual(17, course.DwellTicks(29));
            Assert.AreEqual(14, course.DwellTicks(30));
            Assert.AreEqual(14, course.DwellTicks(39));
        }

        [Test]
        public void 시작_색은_칸과_시도로_정해진다()
        {
            var a = Make(7); var b = Make(7);
            bool attemptMatters = false;
            for (int step = 0; step < 40; step++)
            {
                Assert.AreEqual(a.StartColor(step, 0), b.StartColor(step, 0));
                attemptMatters |= a.StartColor(step, 0) != a.StartColor(step, 1);
            }
            Assert.IsTrue(attemptMatters);
        }

        [Test]
        public void 틀린_구간_표는_거절한다()
        {
            Assert.IsFalse(CardRushSections.Validate(new[] { new CardRushSection(1, 0.4f) }, out _));
            Assert.IsFalse(CardRushSections.Validate(new[] { new CardRushSection(0, 0.4f), new CardRushSection(0, 0.3f) }, out _));
            Assert.IsFalse(CardRushSections.Validate(new[] { new CardRushSection(0, 0.05f) }, out _));
            Assert.IsTrue(CardRushSections.Validate(CardRushSections.Default, out _));
        }
    }
}
