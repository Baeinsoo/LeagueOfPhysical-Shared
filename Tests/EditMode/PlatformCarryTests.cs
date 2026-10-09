using GameFramework;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class PlatformCarryTests
    {
        const float Tolerance = 1e-3f;

        static GameFramework.World.Transform Body(Vector3 position)
            => new GameFramework.World.Transform { Position = position.ToNumerics(), Rotation = Quaternion.identity.ToNumerics() };

        [Test]
        public void 판이_돌면_위에_선_몸도_축을_따라_돌고_방향도_돈다()
        {
            var body = Body(new Vector3(10f, 1f, 0f));
            var before = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
            var after = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 90f, 0f), Vector3.one);

            PlatformCarry.Apply(body, before, after);

            Vector3 p = body.Position.ToUnity();
            Assert.AreEqual(0f, p.x, Tolerance);
            Assert.AreEqual(1f, p.y, Tolerance);
            Assert.AreEqual(-10f, p.z, Tolerance);
            Assert.AreEqual(90f, body.Rotation.ToUnity().eulerAngles.y, 0.01f);
        }

        [Test]
        public void 오래_같이_돌아도_몸_방향_값의_길이가_1로_유지된다()
        {
            //  매 틱 회전을 곱해 쌓으므로 곱셈 오차가 누적된다(리뷰 1·2차). 한 판(5분 = 15000틱)보다 넉넉히 돈다.
            var body = Body(new Vector3(10f, 0f, 0f));
            var before = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
            var after = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0.7f, 0f), Vector3.one);
            for (int i = 0; i < 100000; i++) { PlatformCarry.Apply(body, before, after); }

            var q = body.Rotation;
            float length = Mathf.Sqrt(q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W);
            Assert.AreEqual(1f, length, 1e-6f);
        }

        [Test]
        public void 판이_밀리면_같이_밀리고_방향은_그대로다()
        {
            var body = Body(new Vector3(3f, 0f, 4f));
            var before = Matrix4x4.TRS(new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 30f, 0f), Vector3.one);
            var after = Matrix4x4.TRS(new Vector3(2f, 0f, 0f), Quaternion.Euler(0f, 30f, 0f), Vector3.one);

            PlatformCarry.Apply(body, before, after);

            Vector3 p = body.Position.ToUnity();
            Assert.AreEqual(5f, p.x, Tolerance);
            Assert.AreEqual(4f, p.z, Tolerance);
            Assert.AreEqual(0f, Quaternion.Angle(Quaternion.identity, body.Rotation.ToUnity()), 0.01f);
        }
    }
}
