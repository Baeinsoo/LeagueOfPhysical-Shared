using System.Numerics;
using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    public class AttackSectorTests
    {
        private static Transform Caster(Vector3 pos, Quaternion rot)
            => new Transform { Position = pos, Rotation = rot };

        [Test]
        public void FrontWithinRangeAndAngle_IsInside()
        {
            var caster = Caster(Vector3.Zero, Quaternion.Identity);   // forward = +Z
            Assert.IsTrue(AttackSector.Contains(caster, new Vector3(0, 0, 3), 5f, 90f));
        }

        [Test]
        public void Behind_IsOutside()
        {
            var caster = Caster(Vector3.Zero, Quaternion.Identity);
            Assert.IsFalse(AttackSector.Contains(caster, new Vector3(0, 0, -3), 5f, 90f));
        }

        [Test]
        public void BeyondRange_IsOutside()
        {
            var caster = Caster(Vector3.Zero, Quaternion.Identity);
            Assert.IsFalse(AttackSector.Contains(caster, new Vector3(0, 0, 10), 5f, 90f));
        }

        [Test]
        public void AtHalfAngleBoundary_IsInside()
        {
            var caster = Caster(Vector3.Zero, Quaternion.Identity);
            // 45° off forward, angle 90 → half-angle 45 → 경계 포함.
            Assert.IsTrue(AttackSector.Contains(caster, new Vector3(3, 0, 3), 5f, 90f));
        }

        [Test]
        public void JustOutsideHalfAngle_IsOutside()
        {
            var caster = Caster(Vector3.Zero, Quaternion.Identity);
            // 90° off forward(옆), angle 90 → half 45 → 밖.
            Assert.IsFalse(AttackSector.Contains(caster, new Vector3(3, 0, 0), 5f, 90f));
        }

        [Test]
        public void RotationFlipsResult()
        {
            var caster = Caster(Vector3.Zero, Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)System.Math.PI)); // 180° → forward -Z
            Assert.IsFalse(AttackSector.Contains(caster, new Vector3(0, 0, 3), 5f, 90f));   // 이제 뒤
            Assert.IsTrue(AttackSector.Contains(caster, new Vector3(0, 0, -3), 5f, 90f));   // 이제 앞
        }
    }
}
