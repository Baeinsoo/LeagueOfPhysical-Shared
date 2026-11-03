using NUnit.Framework;

namespace LOP.Tests
{
    public class CardRushProgressTests
    {
        [Test]
        public void 칸이_많으면_크고_같은_칸이면_먼저_도착이_크다()
        {
            Assert.Greater(CardRushProgress.ForRanking(10, 7000, 7500), CardRushProgress.ForRanking(9, 100, 7500));
            Assert.Greater(CardRushProgress.ForRanking(39, 6999, 7500), CardRushProgress.ForRanking(39, 7000, 7500));
            Assert.AreEqual(0.5f, CardRushProgress.ForRanking(0, 0, 7500), 1e-6f);
        }
    }
}
