using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class ArcheryTrajectoryTests
    {
        const float Tolerance = 1e-3f;

        [Test]
        public void 정면을_보면_앞으로_향한다()
        {
            var direction = ArcheryTrajectory.DirectionFrom(0f, 0f);

            Assert.AreEqual(0f, direction.x, Tolerance);
            Assert.AreEqual(0f, direction.y, Tolerance);
            Assert.AreEqual(1f, direction.z, Tolerance);
        }

        [Test]
        public void 좌우_각도는_y축_회전이다()
        {
            var direction = ArcheryTrajectory.DirectionFrom(90f, 0f);

            Assert.AreEqual(1f, direction.x, Tolerance);
            Assert.AreEqual(0f, direction.y, Tolerance);
            Assert.AreEqual(0f, direction.z, Tolerance);
        }

        [Test]
        public void 위아래_각도가_양수면_위를_본다()
        {
            var direction = ArcheryTrajectory.DirectionFrom(0f, 90f);

            Assert.AreEqual(0f, direction.x, Tolerance);
            Assert.AreEqual(1f, direction.y, Tolerance);
            Assert.AreEqual(0f, direction.z, Tolerance);
        }

        [Test]
        public void 방향은_길이가_1이다()
        {
            var direction = ArcheryTrajectory.DirectionFrom(37f, 21f);

            Assert.AreEqual(1f, direction.magnitude, Tolerance);
        }

        [Test]
        public void 수평으로_쏘면_중력만큼_처진다()
        {
            var shot = new ArcheryShot("a", 0, Vector3.zero, new Vector3(0f, 0f, 40f));

            var position = ArcheryTrajectory.PositionAt(shot, 1f);

            Assert.AreEqual(40f, position.z, Tolerance);                          // 40 × 1
            Assert.AreEqual(-0.5f * ArcheryTrajectory.Gravity, position.y, Tolerance); // -½gt²
        }

        [Test]
        public void 발사_직후에는_출발점_그대로다()
        {
            var origin = new Vector3(3f, 2f, 1f);
            var shot = new ArcheryShot("a", 0, origin, new Vector3(0f, 0f, 40f));

            var position = ArcheryTrajectory.PositionAt(shot, 0f);

            Assert.AreEqual(origin.x, position.x, Tolerance);
            Assert.AreEqual(origin.y, position.y, Tolerance);
            Assert.AreEqual(origin.z, position.z, Tolerance);
        }

        [Test]
        public void 세로_속도가_중력만큼_줄어든다()
        {
            var shot = new ArcheryShot("a", 0, Vector3.zero, new Vector3(0f, 10f, 40f));

            var velocity = ArcheryTrajectory.VelocityAt(shot, 1f);

            Assert.AreEqual(40f, velocity.z, Tolerance);
            Assert.AreEqual(10f - ArcheryTrajectory.Gravity, velocity.y, Tolerance);
        }

        [Test]
        public void 바람이_없으면_바람_넣기_전_식과_같은_자리다()
        {
            //  바람을 넣기 전의 식을 그대로 옮겨 적었다. 곱하는 순서가 달라 끝자리가 다를 수 있어 오차를 둔다.
            var shot = new ArcheryShot("a", 0, new Vector3(1f, 2f, 3f), new Vector3(4f, 5f, 60f));
            for (float t = 0f; t < 3f; t += 0.1f)
            {
                Vector3 old = shot.Origin + shot.Velocity * t
                            + new Vector3(0f, -0.5f * ArcheryTrajectory.Gravity * t * t, 0f);
                Vector3 now = ArcheryTrajectory.PositionAt(shot, t);
                Assert.AreEqual(old.x, now.x, 1e-4f);
                Assert.AreEqual(old.y, now.y, 1e-4f);
                Assert.AreEqual(old.z, now.z, 1e-4f);
            }
        }

        [Test]
        public void 바람은_반_a_t제곱만큼_민다()
        {
            var shot = new ArcheryShot("a", 0, Vector3.zero, new Vector3(0f, 0f, 60f), new Vector3(10f, 0f, 0f));
            var p = ArcheryTrajectory.PositionAt(shot, 0.5f);
            Assert.AreEqual(0.5f * 10f * 0.25f, p.x, 1e-5f);
            Assert.AreEqual(10f * 0.5f, ArcheryTrajectory.VelocityAt(shot, 0.5f).x, 1e-5f);
        }

        [Test]
        public void WithWind는_바람만_바꾼다()
        {
            var shot = new ArcheryShot("a", 7, Vector3.one, Vector3.forward);
            var windy = shot.WithWind(Vector3.right);
            Assert.AreEqual("a", windy.ShooterId);
            Assert.AreEqual(7, windy.FireTick);
            Assert.AreEqual(Vector3.one, windy.Origin);
            Assert.AreEqual(Vector3.forward, windy.Velocity);
            Assert.AreEqual(Vector3.right, windy.Wind);
        }

    }
}
