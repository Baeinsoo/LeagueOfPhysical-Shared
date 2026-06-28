using LOP;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class MovementSystemTests
    {
        const float Tolerance = 1e-4f;

        private static MovementResult Move(Vector3 cur, float h, float v,
            float speed = 5f, float maxAccel = 100f, float dt = 0.1f)
            => MovementSystem.ProcessMovement(new MovementInput(cur, h, v, speed, maxAccel, dt));

        [Test]
        public void NoInput_LeavesHorizontalUnchanged()
        {
            // 방향 입력이 없으면 좌우/앞뒤 속도는 그대로(멈춤은 drag가 처리). 위아래 속도도 보존.
            var r = Move(new Vector3(5f, -7.5f, 0f), 0f, 0f);

            Assert.That(r.velocity.x, Is.EqualTo(5f).Within(Tolerance));
            Assert.That(r.velocity.y, Is.EqualTo(-7.5f).Within(Tolerance));
            Assert.IsFalse(r.hasRotation);
        }

        [Test]
        public void Forward_AcceleratesTowardSpeed_PreservesY()
        {
            // 멈춰 있다가 앞으로 입력 → 한 번에 최대 속도. 위아래 속도(3)는 그대로.
            var r = Move(new Vector3(0f, 3f, 0f), 0f, 1f);

            Assert.IsTrue(r.hasRotation);
            Assert.That(r.velocity.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(r.velocity.y, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(r.velocity.z, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void DirectionChange_BrakesPerpendicular_NoDrift()
        {
            // 오른쪽으로 8 가다가 위로 입력 → 오른쪽 속도(8)가 사라지고 위로만(5). 옆으로 안 미끄러짐.
            var r = Move(new Vector3(8f, 0f, 0f), 0f, 1f, maxAccel: 200f);

            Assert.That(r.velocity.x, Is.EqualTo(0f).Within(Tolerance), "방향전환 시 직각 관성이 남으면 안 됨(드리프트)");
            Assert.That(r.velocity.z, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void OverSpeed_BrakesTowardMoveSpeed()
        {
            // 최대 속도(5)보다 빠른 8에서 같은 방향 입력 → 목표인 5로 줄어든다.
            var r = Move(new Vector3(-8f, 0f, 0f), -1f, 0f);

            Assert.That(r.velocity.x, Is.EqualTo(-5f).Within(Tolerance));
        }

        [Test]
        public void PreservesYVelocity()
        {
            var r = Move(new Vector3(0f, -7.5f, 0f), 1f, 0f);

            Assert.That(r.velocity.y, Is.EqualTo(-7.5f).Within(Tolerance));
        }

        [Test]
        public void Rotation_FacesMoveDirection()
        {
            // 오른쪽(+x) 입력 → 오른쪽(90도)을 바라봄
            var r = Move(Vector3.zero, 1f, 0f);

            Assert.IsTrue(r.hasRotation);
            Assert.That(r.rotation.y, Is.EqualTo(90f).Within(Tolerance));
        }

        [Test]
        public void Reverse_AcceleratesOppositeInstant()
        {
            // 왼쪽으로 8 가다가 오른쪽 입력 → 한 번에 오른쪽 5로 바뀜(반응 빠를 때).
            var r = Move(new Vector3(-8f, 0f, 0f), 1f, 0f, maxAccel: 200f);

            Assert.That(r.velocity.x, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void AccelCap_LimitsChangePerTick()
        {
            // 반응 빠르기를 작게(20) 주면 → 한 틱에 2까지만 (최대 5에는 아직 못 미침).
            var r = Move(Vector3.zero, 0f, 1f, maxAccel: 20f);

            Assert.That(r.velocity.z, Is.EqualTo(2f).Within(Tolerance));
        }
    }
}
