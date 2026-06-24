using LOP;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class MovementSystemTests
    {
        const float Tolerance = 1e-4f;

        [Test]
        public void ProcessMovement_NoInput_ReturnsNoMove()
        {
            var result = MovementSystem.ProcessMovement(
                new MovementInput(Vector3.zero, 0f, 0f, 5f));

            Assert.IsFalse(result.hasMove);
        }

        [Test]
        public void ProcessMovement_Forward_VelocityFromSpeed()
        {
            var result = MovementSystem.ProcessMovement(
                new MovementInput(new Vector3(0f, 3f, 0f), 0f, 1f, 5f));

            Assert.IsTrue(result.hasMove);
            Assert.That(result.velocity.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(result.velocity.y, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(result.velocity.z, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void ProcessMovement_PreservesYVelocity()
        {
            var result = MovementSystem.ProcessMovement(
                new MovementInput(new Vector3(0f, -7.5f, 0f), 1f, 0f, 5f));

            Assert.That(result.velocity.y, Is.EqualTo(-7.5f).Within(Tolerance));
        }

        [Test]
        public void ProcessMovement_Rotation_FacesMoveDirection()
        {
            // 오른쪽(+x) 입력 → atan2(1, 0) = 90°
            var result = MovementSystem.ProcessMovement(
                new MovementInput(Vector3.zero, 1f, 0f, 5f));

            Assert.That(result.rotation.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(result.rotation.y, Is.EqualTo(90f).Within(Tolerance));
            Assert.That(result.rotation.z, Is.EqualTo(0f).Within(Tolerance));
        }
    }
}
