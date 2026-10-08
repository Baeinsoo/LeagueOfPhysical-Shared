using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class FlappyCorridorLineTests
    {
        private GameObject go;
        private FlappyCorridorLine line;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("CorridorLine");
            line = go.AddComponent<FlappyCorridorLine>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
        }

        [Test]
        public void 점이_없으면_0이다()
        {
            Assert.AreEqual(0f, line.CenterAt(5f), 1e-4f);
        }

        [Test]
        public void 두_점_사이를_선형_보간한다()
        {
            line.Points = new[] { new Vector2(0f, 0f), new Vector2(10f, 10f) };

            Assert.AreEqual(5f, line.CenterAt(5f), 1e-4f);
            Assert.AreEqual(2f, line.CenterAt(2f), 1e-4f);
        }

        [Test]
        public void 왼쪽_끝_밖은_첫_점_값이다()
        {
            line.Points = new[] { new Vector2(0f, 3f), new Vector2(10f, 10f) };

            Assert.AreEqual(3f, line.CenterAt(-5f), 1e-4f);
        }

        [Test]
        public void 오른쪽_끝_밖은_마지막_점_값이다()
        {
            line.Points = new[] { new Vector2(0f, 3f), new Vector2(10f, 10f) };

            Assert.AreEqual(10f, line.CenterAt(100f), 1e-4f);
        }

        [Test]
        public void 세_점_이상도_순서대로_보간한다()
        {
            line.Points = new[] { new Vector2(0f, 0f), new Vector2(10f, 10f), new Vector2(20f, 0f) };

            Assert.AreEqual(10f, line.CenterAt(10f), 1e-4f);
            Assert.AreEqual(5f, line.CenterAt(15f), 1e-4f);
        }
    }
}
