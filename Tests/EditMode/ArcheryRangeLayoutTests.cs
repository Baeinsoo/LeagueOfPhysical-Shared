using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 맵 씬이 주는 값이 순수 C#으로 건너오는 통로. <b>클·서가 같은 씬을 읽어 같은 값을 얻어야</b>
    /// 과녁이 같은 자리에 선다 — 그래서 읽는 규칙(순서·검증)을 공유 코드에 둔다.
    /// </summary>
    public class ArcheryRangeLayoutTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned) { Object.DestroyImmediate(go); }
            spawned.Clear();
        }

        private ArcheryLane MakeLane(string name, int order, Vector3 position, float yaw, params float[] standDistances)
        {
            var root = new GameObject(name);
            spawned.Add(root);
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var lane = root.AddComponent<ArcheryLane>();
            lane.Order = order;
            lane.Stands = new Transform[standDistances.Length];
            for (int i = 0; i < standDistances.Length; i++)
            {
                var stand = new GameObject(name + "-stand" + i);
                stand.transform.SetParent(root.transform);
                //  사대 앞으로 그 거리만큼. 로컬 z가 곧 사거리다.
                stand.transform.localPosition = new Vector3(0f, 1.3f, standDistances[i]);
                lane.Stands[i] = stand.transform;
            }
            return lane;
        }

        [Test]
        public void 레인은_Order_순서대로_선다()
        {
            var b = MakeLane("B", 1, new Vector3(4f, 0f, 0f), 0f, 10f, 20f);
            var a = MakeLane("A", 0, new Vector3(0f, 0f, 0f), 0f, 10f, 20f);

            var layout = ArcheryRangeLayout.From(new[] { b, a });

            Assert.AreEqual(2, layout.Lanes.Count);
            Assert.AreEqual(new Vector3(0f, 0f, 0f), layout.Lanes[0].ShooterPosition,
                "Order가 작은 레인이 먼저 와야 한다 — 씬에서 찾아오는 순서는 실행마다 다를 수 있다");
            Assert.AreEqual(new Vector3(4f, 0f, 0f), layout.Lanes[1].ShooterPosition);
        }

        [Test]
        public void 과녁_자리는_세계_좌표로_건너온다()
        {
            MakeLane("A", 0, new Vector3(0f, 0f, 0f), 0f, 12f, 30f);

            var layout = ArcheryRangeLayout.From(Object.FindObjectsByType<ArcheryLane>(
                FindObjectsInactive.Include, FindObjectsSortMode.None));

            Assert.AreEqual(2, layout.StandCount);
            Assert.AreEqual(12f, layout.Lanes[0].Stands[0].z, 1e-4f);
            Assert.AreEqual(30f, layout.Lanes[0].Stands[1].z, 1e-4f);
            Assert.AreEqual(1.3f, layout.Lanes[0].Stands[0].y, 1e-4f, "과녁 높이는 씬이 정한다");
        }

        [Test]
        public void 레인이_돌아가_있으면_과녁도_그_방향으로_선다()
        {
            //  레인을 90도 돌리면 사대 앞은 +x 쪽이다.
            MakeLane("A", 0, Vector3.zero, 90f, 20f);

            var layout = ArcheryRangeLayout.From(Object.FindObjectsByType<ArcheryLane>(
                FindObjectsInactive.Include, FindObjectsSortMode.None));

            Assert.AreEqual(20f, layout.Lanes[0].Stands[0].x, 1e-3f);
            Assert.AreEqual(0f, layout.Lanes[0].Stands[0].z, 1e-3f);
            //  Quaternion.Euler(0,90,0)은 부동소수점 삼각함수라 (1,0,0)에 근접할 뿐 완전히
            //  같지는 않다 — Assert.AreEqual의 정확 일치 대신 거리로 비교한다.
            Assert.Less(Vector3.Distance(Vector3.right, layout.Lanes[0].Forward), 1e-4f,
                "레인이 보는 쪽이 사대에서 과녁 쪽이다");
        }

        [Test]
        public void 레인마다_과녁_자리_수가_다르면_거부한다()
        {
            var a = MakeLane("A", 0, Vector3.zero, 0f, 10f, 20f);
            var b = MakeLane("B", 1, new Vector3(4f, 0f, 0f), 0f, 10f);

            //  같은 순서의 같은 거리를 모두가 본다는 것이 이 맵의 전제다(같은 시험지).
            //  한 레인만 자리가 모자라면 그 사수만 과녁이 안 뜨는데 에러가 안 난다 — 여기서 끊는다.
            Assert.Throws<System.InvalidOperationException>(
                () => ArcheryRangeLayout.From(new[] { a, b }));
        }

        [Test]
        public void 레인이_없으면_빈_레이아웃이다()
        {
            var layout = ArcheryRangeLayout.From(new ArcheryLane[0]);

            Assert.IsTrue(layout.IsEmpty, "원형 맵에는 레인이 없다 — 빈 레이아웃이 정상이다");
            Assert.AreEqual(0, layout.StandCount);
        }
    }
}
