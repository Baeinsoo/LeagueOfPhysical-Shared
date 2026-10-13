using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>진자에 매달린 수레(Upright)는 스스로 돌지 않고, 축에서 줄 길이만큼 떨어진 원 위를 레벨로 돈다.</summary>
    public class FlappyPendulumUprightTests
    {
        private GameObject go;
        private const float RopeLength = 2f;

        [TearDown]
        public void TearDown() { if (go != null) { Object.DestroyImmediate(go); } }

        private FlappyPendulum Make(float amplitude, float period, float phase, out Transform upright)
        {
            go = new GameObject("pendulum");
            var p = go.AddComponent<FlappyPendulum>();
            p.Amplitude = amplitude; p.Period = period; p.Phase = phase;
            var cart = new GameObject("Upright");
            cart.transform.SetParent(go.transform, false);
            cart.transform.localPosition = new Vector3(0f, -RopeLength, 0f);
            upright = cart.transform;
            p.Upright = upright;
            return p;
        }

        [TestCase(0L)]
        [TestCase(25L)]
        [TestCase(60L)]
        public void Upright가_있으면_항상_레벨이고_줄길이_원_위에_있다(long tick)
        {
            var field = new FlappyPendulumField();
            var p = Make(55f, 2f, 10f, out var upright);
            field.Add(p);

            field.PoseForTick(tick, 0.02f);

            Assert.That(Quaternion.Angle(Quaternion.identity, upright.rotation), Is.LessThan(1e-4f));
            Assert.AreEqual(RopeLength, Vector3.Distance(go.transform.position, upright.position), 1e-4f);
        }

        [Test]
        public void Upright가_비어_있으면_기존_자세_그대로다()
        {
            //  기존 FlappyPendulumFieldTests.틱의_각도로_z축을_대입한다 와 같은 검증 — Upright 없을 때 회귀가 없는지 확인한다.
            var field = new FlappyPendulumField();
            var p = Make(55f, 2f, 0f, out _);
            p.Upright = null;
            field.Add(p);

            field.PoseForTick(25, 0.02f);

            Assert.AreEqual(55f, go.transform.localEulerAngles.z, 1e-2f);
        }
    }
}
