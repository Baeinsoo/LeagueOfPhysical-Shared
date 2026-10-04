using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class FlappyShutterFieldTests
    {
        private GameObject go;

        [TearDown]
        public void TearDown() { if (go != null) { Object.DestroyImmediate(go); } }

        private FlappyShutter Make(float travel, float phase)
        {
            go = new GameObject("shutter");
            var s = go.AddComponent<FlappyShutter>();
            var door = new GameObject("Door");
            door.transform.SetParent(go.transform, false);
            door.transform.localPosition = new Vector3(0.2f, 0f, -0.1f);
            s.Door = door.transform;
            s.Travel = travel; s.Period = 2f; s.OpenShare = 0.4f; s.MoveShare = 0.2f; s.Phase = phase;
            return s;
        }

        [Test]
        public void 열린_만큼_문을_올린다()
        {
            var field = new FlappyShutterField();
            var s = Make(4f, 0f);
            field.Add(s);
            field.PoseForTick(30, 0.02f);   // 올라감 한가운데 → 0.5
            Assert.AreEqual(2f, s.Door.localPosition.y, 1e-3f);
            field.PoseForTick(60, 0.02f);   // 열림
            Assert.AreEqual(4f, s.Door.localPosition.y, 1e-3f);
            field.PoseForTick(10, 0.02f);   // 닫힘
            Assert.AreEqual(0f, s.Door.localPosition.y, 1e-4f);
        }

        [Test]
        public void x와_z는_건드리지_않는다()
        {
            var field = new FlappyShutterField();
            var s = Make(4f, 0f);
            field.Add(s);
            field.PoseForTick(30, 0.02f);
            Assert.AreEqual(0.2f, s.Door.localPosition.x, 1e-5f);
            Assert.AreEqual(-0.1f, s.Door.localPosition.z, 1e-5f);
        }

        [Test]
        public void 같은_틱을_다시_놓으면_같은_자리다()
        {
            //  롤백 재생: 33틱 → 5틱 → 33틱으로 되돌아와도 같아야 한다(누적이면 어긋난다).
            var field = new FlappyShutterField();
            var s = Make(5.3f, 10f);
            field.Add(s);
            field.PoseForTick(33, 0.02f);
            Vector3 first = s.Door.localPosition;
            field.PoseForTick(5, 0.02f);
            field.PoseForTick(33, 0.02f);
            Assert.AreEqual(first, s.Door.localPosition);
        }

        [Test]
        public void 같은_셔터를_두_번_넣어도_하나다()
        {
            var field = new FlappyShutterField();
            var s = Make(4f, 0f);
            field.Add(s); field.Add(s);
            Assert.AreEqual(1, field.Count);
        }

        [Test]
        public void 문이_없으면_건너뛴다()
        {
            var field = new FlappyShutterField();
            var s = Make(4f, 0f);
            s.Door = null;
            field.Add(s);
            Assert.DoesNotThrow(() => field.PoseForTick(30, 0.02f));
        }
    }
}
