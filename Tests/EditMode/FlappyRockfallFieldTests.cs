using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class FlappyRockfallFieldTests
    {
        private GameObject go;

        [TearDown]
        public void TearDown() { if (go != null) { Object.DestroyImmediate(go); } }

        private FlappyRockfall Make(float phase)
        {
            go = new GameObject("rockfall");
            var r = go.AddComponent<FlappyRockfall>();
            var rock = new GameObject("Rock");
            rock.transform.SetParent(go.transform, false);
            rock.transform.localPosition = new Vector3(0.3f, 5.6f, -0.2f);
            r.Rock = rock.transform;
            r.Period = 2f; r.Speed = 9f; r.Drop = 15.76f; r.Size = 1.2f; r.HiddenRise = 5.6f; r.Phase = phase;
            return r;
        }

        [Test]
        public void 떨어지는_중이면_바위_가운데가_천장_아래로()
        {
            var field = new FlappyRockfallField();
            var r = Make(0f);
            field.Add(r);
            field.PoseForTick(50, 0.02f);   // 9 m 내려옴 → 가운데 −(9 + 0.6)
            Assert.AreEqual(-9.6f, r.Rock.localPosition.y, 1e-3f);
        }

        [Test]
        public void 숨는_동안은_천장_속에()
        {
            var field = new FlappyRockfallField();
            var r = Make(0f);
            field.Add(r);
            field.PoseForTick(95, 0.02f);   // 1.9초 → 다 떨어짐
            Assert.AreEqual(5.6f, r.Rock.localPosition.y, 1e-5f);
        }

        [Test]
        public void x와_z는_건드리지_않는다()
        {
            var field = new FlappyRockfallField();
            var r = Make(0f);
            field.Add(r);
            field.PoseForTick(50, 0.02f);
            Assert.AreEqual(0.3f, r.Rock.localPosition.x, 1e-5f);
            Assert.AreEqual(-0.2f, r.Rock.localPosition.z, 1e-5f);
        }

        [Test]
        public void 같은_틱을_다시_놓으면_같은_자리다()
        {
            var field = new FlappyRockfallField();
            var r = Make(90f);
            field.Add(r);
            field.PoseForTick(33, 0.02f);
            Vector3 first = r.Rock.localPosition;
            field.PoseForTick(90, 0.02f);
            field.PoseForTick(33, 0.02f);
            Assert.AreEqual(first, r.Rock.localPosition);
        }

        [Test]
        public void 같은_낙석을_두_번_넣어도_하나다()
        {
            var field = new FlappyRockfallField();
            var r = Make(0f);
            field.Add(r); field.Add(r);
            Assert.AreEqual(1, field.Count);
        }

        [Test]
        public void 바위가_없으면_건너뛴다()
        {
            var field = new FlappyRockfallField();
            var r = Make(0f);
            r.Rock = null;
            field.Add(r);
            Assert.DoesNotThrow(() => field.PoseForTick(50, 0.02f));
        }
    }
}
