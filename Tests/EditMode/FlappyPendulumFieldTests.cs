using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class FlappyPendulumFieldTests
    {
        private GameObject go;

        [TearDown]
        public void TearDown() { if (go != null) { Object.DestroyImmediate(go); } }

        private FlappyPendulum Make(float amplitude, float period, float phase)
        {
            go = new GameObject("pendulum");
            var p = go.AddComponent<FlappyPendulum>();
            p.Amplitude = amplitude; p.Period = period; p.Phase = phase;
            return p;
        }

        [Test]
        public void 틱의_각도로_z축을_대입한다()
        {
            var field = new FlappyPendulumField();
            field.Add(Make(55f, 2f, 0f));
            field.PoseForTick(25, 0.02f);
            Assert.AreEqual(55f, go.transform.localEulerAngles.z, 1e-2f);
        }

        [Test]
        public void 같은_틱을_다시_놓으면_같은_자리다()
        {
            //  롤백 재생: 37틱 → 5틱 → 37틱으로 되돌아와도 자세가 같아야 한다(누적이면 어긋난다).
            var field = new FlappyPendulumField();
            field.Add(Make(55f, 2.5f, 10f));
            field.PoseForTick(37, 0.02f);
            Quaternion first = go.transform.localRotation;
            field.PoseForTick(5, 0.02f);
            field.PoseForTick(37, 0.02f);
            Assert.That(Quaternion.Angle(first, go.transform.localRotation), Is.LessThan(1e-3f));
        }

        [Test]
        public void 같은_진자를_두_번_넣어도_하나다()
        {
            var field = new FlappyPendulumField();
            var p = Make(55f, 2f, 0f);
            field.Add(p); field.Add(p);
            Assert.AreEqual(1, field.Count);
        }
    }
}
