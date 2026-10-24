using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class MountedArcheryHitTestTests
    {
        [Test]
        public void 기수를_관통하면_맞는다()
        {
            bool hit = MountedArcheryHitTest.SegmentHits(
                new Vector3(-5f, 2.4f, 0f), new Vector3(5f, 2.4f, 0f), Vector3.zero, 0f, out float along);

            Assert.IsTrue(hit);
            Assert.AreEqual(0.438f, along, 0.01f);   // 접점 x = -0.62 → (5-0.62)/10
        }

        [Test]
        public void 빠른_화살이_한_틱에_몸을_건너뛰어도_잡힌다()
        {
            Assert.IsTrue(MountedArcheryHitTest.SegmentHits(
                new Vector3(-50f, 1.4f, 0.6f), new Vector3(50f, 1.4f, 0.6f), Vector3.zero, 0f, out _));
        }

        [Test]
        public void 말_머리는_머리_방향을_따라_돈다()
        {
            var from = new Vector3(1.35f, 0f, 0f);
            var to = new Vector3(1.35f, 5f, 0f);

            Assert.IsTrue(MountedArcheryHitTest.SegmentHits(from, to, Vector3.zero, 90f, out _));
            Assert.IsFalse(MountedArcheryHitTest.SegmentHits(from, to, Vector3.zero, 0f, out _));
        }

        [Test]
        public void 옆을_스치면_안_맞는다()
        {
            Assert.IsFalse(MountedArcheryHitTest.SegmentHits(
                new Vector3(-5f, 4f, 0f), new Vector3(5f, 4f, 0f), Vector3.zero, 0f, out _));
        }
    }
}
