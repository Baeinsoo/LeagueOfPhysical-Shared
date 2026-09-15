using System.Collections.Generic;
using NUnit.Framework;

namespace LOP.Tests
{
    public class ArcheryRingScoringTests
    {
        //  띠는 "바깥 경계까지"를 뜻한다 — 0.2면 중심에서 반지름의 20%까지가 그 띠다.
        [Test]
        public void 띠는_바깥_경계와_점수를_들고_있다()
        {
            var band = new ArcheryRingBand(0.2f, 10);

            Assert.AreEqual(0.2f, band.OuterRatio, 1e-6f);
            Assert.AreEqual(10, band.Points);
        }

        //  지금 과녁은 "어디를 맞히든 같은 점수"다 — 그게 띠 하나짜리다.
        [Test]
        public void 종류는_모양과_띠를_들고_있다()
        {
            var bands = new List<ArcheryRingBand> { new ArcheryRingBand(1f, 2) };
            var kind = new ArcheryTargetKind(0.3f, 2, 40, false, ArcheryTargetShape.Sphere, bands);

            Assert.AreEqual(ArcheryTargetShape.Sphere, kind.Shape);
            Assert.AreEqual(1, kind.Bands.Count);
            Assert.AreEqual(2, kind.Bands[0].Points);
        }
    }
}
