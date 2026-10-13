using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class FlappyLiftFieldTests
    {
        private GameObject go;

        [TearDown]
        public void TearDown() { if (go != null) { Object.DestroyImmediate(go); } }

        private FlappyLift Make(float phase)
        {
            go = new GameObject("lift");
            var lift = go.AddComponent<FlappyLift>();
            var cage = new GameObject("Cage");
            cage.transform.SetParent(go.transform, false);
            cage.transform.localPosition = new Vector3(0.2f, 0f, -0.1f);
            lift.Cage = cage.transform;
            lift.Amplitude = 2f; lift.Period = 2f; lift.Phase = phase;
            return lift;
        }

        [Test]
        public void 이_틱의_높이로_케이지를_옮긴다()
        {
            var field = new FlappyLiftField();
            var lift = Make(0f);
            field.Add(lift);
            field.PoseForTick(25, 0.02f);
            Assert.AreEqual(2f, lift.Cage.localPosition.y, 1e-3f);
            field.PoseForTick(75, 0.02f);
            Assert.AreEqual(-2f, lift.Cage.localPosition.y, 1e-3f);
        }

        [Test]
        public void x와_z는_건드리지_않는다()
        {
            var field = new FlappyLiftField();
            var lift = Make(0f);
            field.Add(lift);
            field.PoseForTick(25, 0.02f);
            Assert.AreEqual(0.2f, lift.Cage.localPosition.x, 1e-5f);
            Assert.AreEqual(-0.1f, lift.Cage.localPosition.z, 1e-5f);
        }

        [Test]
        public void 같은_틱을_다시_놓으면_같은_자리다()
        {
            var field = new FlappyLiftField();
            var lift = Make(60f);
            field.Add(lift);
            field.PoseForTick(33, 0.02f);
            Vector3 first = lift.Cage.localPosition;
            field.PoseForTick(5, 0.02f);
            field.PoseForTick(33, 0.02f);
            Assert.AreEqual(first, lift.Cage.localPosition);
        }

        [Test]
        public void 같은_승강기를_두_번_넣어도_하나다()
        {
            var field = new FlappyLiftField();
            var lift = Make(0f);
            field.Add(lift); field.Add(lift);
            Assert.AreEqual(1, field.Count);
        }

        [Test]
        public void 케이지가_없으면_건너뛴다()
        {
            var field = new FlappyLiftField();
            var lift = Make(0f);
            lift.Cage = null;
            field.Add(lift);
            Assert.DoesNotThrow(() => field.PoseForTick(25, 0.02f));
        }
    }
}
