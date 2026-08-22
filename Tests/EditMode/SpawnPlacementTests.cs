using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class SpawnPlacementTests
    {
        private readonly List<GameObject> created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in created)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            created.Clear();
        }

        private SpawnPoint Marker(string name, int order, Vector3 position)
        {
            var go = new GameObject(name);
            created.Add(go);
            go.transform.position = position;
            var point = go.AddComponent<SpawnPoint>();
            point.Order = order;
            return point;
        }

        [Test]
        public void 찾은_순서와_무관하게_Order_순으로_세운다()
        {
            // 씬에서 찾아오는 순서는 보장되지 않는다 — 일부러 뒤섞어 넣는다
            var points = new List<SpawnPoint>
            {
                Marker("C", 3, new Vector3(0f, 4f, 0f)),
                Marker("A", 1, new Vector3(0f, -6f, 0f)),
                Marker("B", 2, new Vector3(0f, -1f, 0f)),
            };

            var slots = SpawnPlacement.Arrange(points);

            Assert.AreEqual(3, slots.Count);
            Assert.AreEqual(-6f, slots[0].y, 1e-4f);
            Assert.AreEqual(-1f, slots[1].y, 1e-4f);
            Assert.AreEqual(4f, slots[2].y, 1e-4f);
        }

        [Test]
        public void Order가_같으면_이름으로_갈라_순서가_흔들리지_않는다()
        {
            var forward = new List<SpawnPoint>
            {
                Marker("beta", 1, new Vector3(0f, 2f, 0f)),
                Marker("alpha", 1, new Vector3(0f, 1f, 0f)),
            };

            var slots = SpawnPlacement.Arrange(forward);

            Assert.AreEqual(1f, slots[0].y, 1e-4f);   // alpha
            Assert.AreEqual(2f, slots[1].y, 1e-4f);   // beta
        }

        [Test]
        public void 마커가_없으면_빈_목록을_돌려준다()
        {
            Assert.IsEmpty(SpawnPlacement.Arrange(new List<SpawnPoint>()));
        }

        [Test]
        public void 사라진_마커는_건너뛴다()
        {
            // 씬이 바뀌는 도중에 부르면 목록에 파괴된 오브젝트가 섞일 수 있다
            var alive = Marker("alive", 1, new Vector3(0f, 5f, 0f));
            var doomed = Marker("doomed", 2, new Vector3(0f, 9f, 0f));
            Object.DestroyImmediate(doomed.gameObject);

            var slots = SpawnPlacement.Arrange(new List<SpawnPoint> { alive, doomed });

            Assert.AreEqual(1, slots.Count);
            Assert.AreEqual(5f, slots[0].y, 1e-4f);
        }
    }
}
