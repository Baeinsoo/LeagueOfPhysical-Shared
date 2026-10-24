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
                new Vector3(-5f, 2.1f, -0.1f), new Vector3(5f, 2.1f, -0.1f), Vector3.zero, 0f, out float along);

            Assert.IsTrue(hit);
            Assert.AreEqual(0.465f, along, 0.01f);   // 접점 x = -0.35 → (5-0.35)/10
        }

        //  판정은 그린 몸을 넘지 않는다 — 넘으면 "빗나갔는데 맞았다"가 된다.
        [Test]
        public void 기수_머리_위_허공은_안_맞는다()
        {
            Assert.IsFalse(MountedArcheryHitTest.SegmentHits(
                new Vector3(-5f, 2.6f, -0.1f), new Vector3(5f, 2.6f, -0.1f), Vector3.zero, 0f, out _));
        }

        [Test]
        public void 말_옆구리_바깥은_안_맞는다()
        {
            //  말 몸 그림은 폭 0.85 m(옆으로 0.425 m)다.
            Assert.IsFalse(MountedArcheryHitTest.SegmentHits(
                new Vector3(0.6f, 0f, 0f), new Vector3(0.6f, 3f, 0f), Vector3.zero, 0f, out _));
        }

        [Test]
        public void 말_몸_그림의_끝자락은_맞는다()
        {
            //  말 몸 그림은 길이 2 m(앞뒤로 1 m)다.
            Assert.IsTrue(MountedArcheryHitTest.SegmentHits(
                new Vector3(-5f, 1.4f, 0.9f), new Vector3(5f, 1.4f, 0.9f), Vector3.zero, 0f, out _));
            Assert.IsTrue(MountedArcheryHitTest.SegmentHits(
                new Vector3(-5f, 1.4f, -0.9f), new Vector3(5f, 1.4f, -0.9f), Vector3.zero, 0f, out _));
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
