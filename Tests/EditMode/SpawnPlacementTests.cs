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
            // 이름 순서를 Order와 **거꾸로** 매긴다 — 이름으로 정렬하는 구현이 통과해 버리면
            // 이 테스트는 아무것도 지키지 못한다. 찾아오는 순서도 일부러 뒤섞는다.
            var points = new List<SpawnPoint>
            {
                Marker("A", 3, new Vector3(0f, 4f, 0f)),
                Marker("C", 1, new Vector3(0f, -6f, 0f)),
                Marker("B", 2, new Vector3(0f, -1f, 0f)),
            };

            var slots = SpawnPlacement.Arrange(points);

            Assert.AreEqual(3, slots.Count);
            Assert.AreEqual(-6f, slots[0].y, 1e-4f);
            Assert.AreEqual(-1f, slots[1].y, 1e-4f);
            Assert.AreEqual(4f, slots[2].y, 1e-4f);
        }

        [Test]
        public void Order가_같으면_이름을_바이트_순서로_갈라_순서가_흔들리지_않는다()
        {
            // 대문자 'B'(66)가 소문자 'a'(97)보다 앞인 것은 **바이트 순서**로 볼 때뿐이다.
            // 언어권 규칙으로 비교하면 'a'가 먼저 온다 — 그래서 이 쌍이라야 둘을 구분한다.
            // (언어권 비교는 실행 환경의 지역 설정에 따라 달라질 수 있어 시뮬에는 못 쓴다.)
            var points = new List<SpawnPoint>
            {
                Marker("a", 1, new Vector3(0f, 1f, 0f)),
                Marker("B", 1, new Vector3(0f, 2f, 0f)),
            };

            var slots = SpawnPlacement.Arrange(points);

            Assert.AreEqual(2f, slots[0].y, 1e-4f);   // B
            Assert.AreEqual(1f, slots[1].y, 1e-4f);   // a
        }

        [Test]
        public void 마커가_없으면_빈_목록을_돌려준다()
        {
            Assert.IsEmpty(SpawnPlacement.Arrange(new List<SpawnPoint>()));
        }

        [Test]
        public void 목록_자체가_null이어도_빈_목록을_돌려준다()
        {
            Assert.IsEmpty(SpawnPlacement.Arrange(null));
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
