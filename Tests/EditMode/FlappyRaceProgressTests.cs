using System.Collections.Generic;
using NUnit.Framework;

namespace LOP.Tests
{
    public class FlappyRaceProgressTests
    {
        private const float FinishX = 100f;

        private static FlappyRaceProgress Progress() => new FlappyRaceProgress(FinishX);

        [Test]
        public void 결승선을_넘으면_통과다()
        {
            Assert.IsTrue(Progress().HasFinished(100.5f));
        }

        [Test]
        public void 결승선_위에_정확히_서면_통과다()
        {
            //  선을 밟은 순간이 통과다. 넘어야만 통과로 하면 한 틱 차이로 판이 안 끝날 수 있다.
            Assert.IsTrue(Progress().HasFinished(FinishX));
        }

        [Test]
        public void 결승선_앞이면_통과가_아니다()
        {
            Assert.IsFalse(Progress().HasFinished(99.99f));
        }

        [Test]
        public void 전원이_넘으면_판이_끝난다()
        {
            Assert.IsTrue(Progress().AllFinished(new List<float> { 100f, 120f, 631f }));
        }

        [Test]
        public void 하나라도_안_넘었으면_안_끝난다()
        {
            Assert.IsFalse(Progress().AllFinished(new List<float> { 631f, 631f, 99.9f }));
        }

        [Test]
        public void 남은_새가_없으면_끝났다고_하지_않는다()
        {
            //  스폰 전에는 목록이 비는데, 그때 true를 주면 판이 시작하자마자 끝난다.
            Assert.IsFalse(Progress().AllFinished(new List<float>()));
        }
    }
}
