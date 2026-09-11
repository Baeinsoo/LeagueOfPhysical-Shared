using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class ArcheryHitTestTests
    {
        [Test]
        public void 한가운데를_지나면_맞는다()
        {
            bool hit = ArcheryHitTest.SegmentHitsSphere(
                new Vector3(-5f, 0f, 0f), new Vector3(5f, 0f, 0f),
                Vector3.zero, 1f, out float t);

            Assert.IsTrue(hit);
            //  −5에서 출발해 반지름 1인 구의 앞면(−1)에 닿는다 → 10 중 4 = 0.4
            Assert.AreEqual(0.4f, t, 1e-4f);
        }

        [Test]
        public void 스쳐_지나가면_안_맞는다()
        {
            bool hit = ArcheryHitTest.SegmentHitsSphere(
                new Vector3(-5f, 2f, 0f), new Vector3(5f, 2f, 0f),
                Vector3.zero, 1f, out _);
            Assert.IsFalse(hit);
        }

        [Test]
        public void 선분이_구에_닿기_전에_끝나면_안_맞는다()
        {
            bool hit = ArcheryHitTest.SegmentHitsSphere(
                new Vector3(-5f, 0f, 0f), new Vector3(-2f, 0f, 0f),
                Vector3.zero, 1f, out _);
            Assert.IsFalse(hit);
        }

        [Test]
        public void 구를_이미_지나쳐_버린_선분은_안_맞는다()
        {
            bool hit = ArcheryHitTest.SegmentHitsSphere(
                new Vector3(2f, 0f, 0f), new Vector3(5f, 0f, 0f),
                Vector3.zero, 1f, out _);
            Assert.IsFalse(hit);
        }

        // 한 틱이 20ms라 빠른 화살(65m/s)은 한 틱에 1.3m를 간다 — 지름 0.5m짜리 작은 과녁을
        // 점으로 검사하면 통째로 뚫고 지나간다. 선분으로 재는 이유가 이것이다.
        [Test]
        public void 한_틱에_통과해_버릴_작은_과녁도_잡는다()
        {
            bool hit = ArcheryHitTest.SegmentHitsSphere(
                new Vector3(0f, 0f, -0.7f), new Vector3(0f, 0f, 0.7f),
                Vector3.zero, 0.25f, out float t);
            Assert.IsTrue(hit);
            Assert.That(t, Is.InRange(0f, 1f));
        }

        [Test]
        public void 시작점이_이미_구_안이면_바로_맞는다()
        {
            bool hit = ArcheryHitTest.SegmentHitsSphere(
                Vector3.zero, new Vector3(5f, 0f, 0f), Vector3.zero, 1f, out float t);
            Assert.IsTrue(hit);
            Assert.AreEqual(0f, t, 1e-6f);
        }

        [Test]
        public void 길이가_0인_선분은_그_점이_구_안일_때만_맞는다()
        {
            Assert.IsTrue(ArcheryHitTest.SegmentHitsSphere(
                Vector3.zero, Vector3.zero, Vector3.zero, 1f, out _));
            Assert.IsFalse(ArcheryHitTest.SegmentHitsSphere(
                new Vector3(9f, 0f, 0f), new Vector3(9f, 0f, 0f), Vector3.zero, 1f, out _));
        }
    }
}
