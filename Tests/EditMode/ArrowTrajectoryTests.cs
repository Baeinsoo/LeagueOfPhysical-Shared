using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class ArrowTrajectoryTests
    {
        const float Tolerance = 1e-3f;

        [Test]
        public void 정면을_보면_앞으로_향한다()
        {
            var direction = ArrowTrajectory.DirectionFrom(0f, 0f);

            Assert.AreEqual(0f, direction.x, Tolerance);
            Assert.AreEqual(0f, direction.y, Tolerance);
            Assert.AreEqual(1f, direction.z, Tolerance);
        }

        [Test]
        public void 좌우_각도는_y축_회전이다()
        {
            var direction = ArrowTrajectory.DirectionFrom(90f, 0f);

            Assert.AreEqual(1f, direction.x, Tolerance);
            Assert.AreEqual(0f, direction.y, Tolerance);
            Assert.AreEqual(0f, direction.z, Tolerance);
        }

        [Test]
        public void 위아래_각도가_양수면_위를_본다()
        {
            var direction = ArrowTrajectory.DirectionFrom(0f, 90f);

            Assert.AreEqual(0f, direction.x, Tolerance);
            Assert.AreEqual(1f, direction.y, Tolerance);
            Assert.AreEqual(0f, direction.z, Tolerance);
        }

        [Test]
        public void 방향은_길이가_1이다()
        {
            var direction = ArrowTrajectory.DirectionFrom(37f, 21f);

            Assert.AreEqual(1f, direction.magnitude, Tolerance);
        }

        [Test]
        public void 수평으로_쏘면_중력만큼_처진다()
        {
            var shot = new ArcheryShot("a", 0, Vector3.zero, new Vector3(0f, 0f, 40f));

            var position = ArrowTrajectory.PositionAt(shot, 1f);

            Assert.AreEqual(40f, position.z, Tolerance);                          // 40 × 1
            Assert.AreEqual(-0.5f * ArrowTrajectory.Gravity, position.y, Tolerance); // -½gt²
        }

        [Test]
        public void 발사_직후에는_출발점_그대로다()
        {
            var origin = new Vector3(3f, 2f, 1f);
            var shot = new ArcheryShot("a", 0, origin, new Vector3(0f, 0f, 40f));

            var position = ArrowTrajectory.PositionAt(shot, 0f);

            Assert.AreEqual(origin.x, position.x, Tolerance);
            Assert.AreEqual(origin.y, position.y, Tolerance);
            Assert.AreEqual(origin.z, position.z, Tolerance);
        }

        [Test]
        public void 세로_속도가_중력만큼_줄어든다()
        {
            var shot = new ArcheryShot("a", 0, Vector3.zero, new Vector3(0f, 10f, 40f));

            var velocity = ArrowTrajectory.VelocityAt(shot, 1f);

            Assert.AreEqual(40f, velocity.z, Tolerance);
            Assert.AreEqual(10f - ArrowTrajectory.Gravity, velocity.y, Tolerance);
        }
    }
}
