using NUnit.Framework;

namespace LOP.Tests
{
    public class CardRushTimingTests
    {
        [TestCase(0.40f, 20)] [TestCase(0.33f, 17)] [TestCase(0.27f, 14)]
        [TestCase(0.30f, 15)] [TestCase(0.60f, 30)] [TestCase(0.001f, 1)]
        public void 초를_틱으로(float seconds, int ticks) => Assert.AreEqual(ticks, CardRushTiming.Ticks(seconds));
    }
}
