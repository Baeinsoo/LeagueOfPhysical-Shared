using NUnit.Framework;

namespace LOP.Tests
{
    public class CardRushTimingTests
    {
        [TestCase(0.40f, 20)] [TestCase(0.33f, 17)] [TestCase(0.27f, 14)]
        [TestCase(0.30f, 15)] [TestCase(0.60f, 30)] [TestCase(0.001f, 1)]
        //  float32 몫이 정확히 x.5로 떨어지는 값들 — 몫을 float에 담고 기본(ToEven) 반올림하면 한 틱 모자란다.
        [TestCase(0.17f, 9)] [TestCase(0.37f, 19)] [TestCase(0.57f, 29)]
        public void 초를_틱으로(float seconds, int ticks) => Assert.AreEqual(ticks, CardRushTiming.Ticks(seconds));
    }
}
