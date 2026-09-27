using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class UnityPhysicsBodyKinematicTests
    {
        private GameObject go;

        [TearDown]
        public void TearDown()
        {
            if (go != null) { Object.DestroyImmediate(go); }
        }

        private UnityPhysicsBody Body(bool kinematic)
        {
            go = new GameObject("kinematic-test");
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = kinematic;
            var col = go.AddComponent<BoxCollider>();
            return new UnityPhysicsBody(rb, col);
        }

        [Test]
        public void 키네마틱으로_바꾸면_물리가_굴리지_않는다()
        {
            var body = Body(kinematic: false);

            body.SetKinematic(true);

            Assert.IsTrue(body.IsKinematic);
        }

        [Test]
        public void 다시_풀면_물리가_굴린다()
        {
            var body = Body(kinematic: true);

            body.SetKinematic(false);

            Assert.IsFalse(body.IsKinematic);
        }
    }
}
