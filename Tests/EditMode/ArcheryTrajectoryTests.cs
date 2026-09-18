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
    
        //  조준기 핀이 이 값으로 놓인다. 시험할 것은 하나뿐이다 — **그 각도로 쏘면 정말 맞는가.**
        //  각도만 비교하면 공식을 그대로 베껴 쓴 것이라 틀려도 같이 틀린다. 그래서 답을 궤적
        //  계산(ArcheryTrajectory.PositionAt)에 되먹여, 화살이 실제로 그 자리를 지나는지 본다.
        [TestCase(12f, 0f, 65f)]
        [TestCase(45f, 0f, 65f)]
        [TestCase(90f, 0f, 65f)]
        [TestCase(90f, -0.1f, 65f)]     // 과녁 중심이 눈높이보다 10cm 아래(실제 사거리 맵)
        [TestCase(30f, 1.5f, 40f)]      // 위쪽에 있는 과녁 + 느린 화살
        public void 조준기가_준_각도로_쏘면_그_자리를_지난다(float distance, float heightOffset, float speed)
        {
            float pitch = ArcheryTrajectory.LaunchPitchDegrees(distance, heightOffset, speed);
            Assert.That(pitch, Is.Not.NaN, "닿을 수 있는 거리인데 NaN이 나왔다");

            var shot = new ArcheryShot("archer", 0L, Vector3.zero,
                                       ArcheryTrajectory.DirectionFrom(0f, pitch) * speed);

            //  수평으로 distance만큼 간 시각을 찾아 그때 높이를 본다.
            float horizontalSpeed = speed * Mathf.Cos(pitch * Mathf.Deg2Rad);
            float arrival = distance / horizontalSpeed;
            Vector3 at = ArcheryTrajectory.PositionAt(shot, arrival);

            Assert.AreEqual(distance, at.z, 1e-3f, "수평 거리가 어긋난다");
            Assert.AreEqual(heightOffset, at.y, 1e-2f, "그 거리에서 과녁 높이를 안 지난다");
        }

        [Test]
        public void 두_각도_중_낮은_쪽을_준다()
        {
            //  같은 자리를 높이 띄워서도 맞힐 수 있다. 조준기는 빨리 가는 낮은 쪽을 줘야 한다 —
            //  45도를 넘으면 사거리가 오히려 줄어드는 구간이라 핀 순서까지 뒤집힌다.
            float pitch = ArcheryTrajectory.LaunchPitchDegrees(90f, 0f, 65f);
            Assert.Less(pitch, 45f, "높은 포물선 해를 골랐다");
            Assert.Greater(pitch, 0f, "수평 과녁인데 내려 쏘라고 한다");
        }

        [Test]
        public void 멀수록_더_올려_쏘라고_한다()
        {
            float near = ArcheryTrajectory.LaunchPitchDegrees(12f, 0f, 65f);
            float mid = ArcheryTrajectory.LaunchPitchDegrees(45f, 0f, 65f);
            float far = ArcheryTrajectory.LaunchPitchDegrees(90f, 0f, 65f);
            Assert.Less(near, mid);
            Assert.Less(mid, far);
        }

        [Test]
        public void 닿을_수_없는_거리는_NaN이다()
        {
            //  25m/s(살짝 당긴 활)로 300m는 못 간다 — 핀을 그리면 안 되는 자리다.
            Assert.That(ArcheryTrajectory.LaunchPitchDegrees(300f, 0f, 25f), Is.NaN);
            Assert.That(ArcheryTrajectory.LaunchPitchDegrees(0f, 0f, 65f), Is.NaN);
            Assert.That(ArcheryTrajectory.LaunchPitchDegrees(30f, 0f, 0f), Is.NaN);
        }
    }
}
