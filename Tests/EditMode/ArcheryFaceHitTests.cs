using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class ArcheryFaceHitTests
    {
        //  판은 원점에 서서 −z 쪽(사수 쪽)을 본다. 사수는 z가 음수인 자리에 있다.
        static readonly Vector3 Center = Vector3.zero;
        static readonly Vector3 Facing = new Vector3(0f, 0f, -1f);
        const float Radius = 0.4f;

        static ArcheryTarget Face(float radius = Radius)
        {
            return new ArcheryTarget(
                waveIndex: 0, slotIndex: 0,
                origin: Center, riseSpeed: 0f, spawnTick: 0L,
                radius: radius, points: 1, isTrap: false,
                shape: ArcheryTargetShape.Face, bands: null, facing: Facing);
        }

        [Test]
        public void 정면에서_한가운데로_오면_맞는다()
        {
            bool hit = ArcheryHitTest.SegmentHitsFace(
                new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, 1f), Center, Facing, Radius, out float t);

            Assert.IsTrue(hit);
            Assert.AreEqual(0.5f, t, 1e-4f, "가운데 지점에서 평면을 지난다");
        }

        //  뒤에서 온 화살은 통과한다 — 안 그러면 과녁 뒤로 넘어간 화살이 되돌아 맞는 꼴이 된다.
        [Test]
        public void 뒤에서_온_화살은_안_맞는다()
        {
            bool hit = ArcheryHitTest.SegmentHitsFace(
                new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, -1f), Center, Facing, Radius, out _);

            Assert.IsFalse(hit);
        }

        [Test]
        public void 판_반지름_밖으로_지나면_안_맞는다()
        {
            bool hit = ArcheryHitTest.SegmentHitsFace(
                new Vector3(0.5f, 0f, -1f), new Vector3(0.5f, 0f, 1f), Center, Facing, Radius, out _);

            Assert.IsFalse(hit);
        }

        //  평면에 못 미치고 멈춘 화살은 아직 안 맞은 것이다(다음 틱에 판정된다).
        [Test]
        public void 평면에_못_닿으면_안_맞는다()
        {
            bool hit = ArcheryHitTest.SegmentHitsFace(
                new Vector3(0f, 0f, -2f), new Vector3(0f, 0f, -1f), Center, Facing, Radius, out _);

            Assert.IsFalse(hit);
        }

        //  판과 나란히 가는 화살은 평면을 지나지 않는다.
        [Test]
        public void 판과_나란히_가면_안_맞는다()
        {
            bool hit = ArcheryHitTest.SegmentHitsFace(
                new Vector3(-1f, 0f, -0.5f), new Vector3(1f, 0f, -0.5f), Center, Facing, Radius, out _);

            Assert.IsFalse(hit);
        }

        //  맞은 자리는 중심에서 얼마나 벗어났는지를 반지름으로 나눈 값이다.
        [Test]
        public void 맞은_자리가_중심에서_얼마나_벗어났는지_준다()
        {
            var target = Face();

            ArcheryHitTest.SegmentHitsTarget(
                new Vector3(0.2f, 0f, -1f), new Vector3(0.2f, 0f, 1f), Center, target,
                out _, out float offset);

            //  0.2m 벗어났고 반지름이 0.4m이므로 절반이다.
            Assert.AreEqual(0.5f, offset, 1e-4f);
        }

        [Test]
        public void 정중앙은_맞은_자리가_0이다()
        {
            var target = Face();

            ArcheryHitTest.SegmentHitsTarget(
                new Vector3(0f, 0f, -1f), new Vector3(0f, 0f, 1f), Center, target,
                out _, out float offset);

            Assert.AreEqual(0f, offset, 1e-4f);
        }

        //  공은 예전 판정 그대로 돈다 — 옆에서 와도 맞는다.
        [Test]
        public void 공은_옆에서_와도_맞는다()
        {
            var sphere = new ArcheryTarget(
                waveIndex: 0, slotIndex: 0,
                origin: Center, riseSpeed: 0f, spawnTick: 0L,
                radius: Radius, points: 1, isTrap: false,
                shape: ArcheryTargetShape.Sphere, bands: null, facing: Vector3.zero);

            bool hit = ArcheryHitTest.SegmentHitsTarget(
                new Vector3(-1f, 0f, 0f), new Vector3(1f, 0f, 0f), Center, sphere, out _, out _);

            Assert.IsTrue(hit);
        }
    }
}
