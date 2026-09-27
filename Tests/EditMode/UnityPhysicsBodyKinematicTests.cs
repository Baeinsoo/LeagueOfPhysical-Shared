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

        private Rigidbody rb;

        private UnityPhysicsBody Body(bool kinematic)
        {
            go = new GameObject("kinematic-test");
            rb = go.AddComponent<Rigidbody>();
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
        public void 굳힌_몸은_아무것과도_부딪히지_않는다()
        {
            //  판치기에서 판 옆에 치운 동전이 판 위 동전을 막는 벽이 되면 안 된다.
            var body = Body(kinematic: false);

            body.SetKinematic(true);

            Assert.IsFalse(rb.detectCollisions);
        }

        [Test]
        public void 다시_풀면_부딪힌다()
        {
            var body = Body(kinematic: false);
            body.SetKinematic(true);

            body.SetKinematic(false);

            Assert.IsTrue(rb.detectCollisions);
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
