using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class DoorFieldTests
    {
        private static DoorVolume Place(Vector3 position)
        {
            var go = new GameObject("door-test");
            go.transform.position = position;
            var volume = go.AddComponent<DoorVolume>();
            volume.HalfWidth = 5f;
            volume.HalfDepth = 5f;
            volume.Thickness = 0.5f;
            volume.AxisAngleDegrees = 0f;
            volume.Period = 20;
            volume.OpenTicks = 6;
            volume.MoveTicks = 4;
            volume.Phase = 0;
            return volume;
        }

        [Test]
        public void 비어_있는_판에는_문이_없다()
        {
            var field = new DoorField();

            Assert.AreEqual(0, field.All.Count);
        }

        [Test]
        public void 넣은_문이_들어간다()
        {
            var field = new DoorField();
            var a = Place(new Vector3(0f, 0f, 0f));
            var b = Place(new Vector3(10f, 0f, 0f));
            try
            {
                field.Add(a);
                field.Add(b);

                Assert.AreEqual(2, field.All.Count);
                Assert.Contains(a, (System.Collections.ICollection)field.All);
                Assert.Contains(b, (System.Collections.ICollection)field.All);
            }
            finally
            {
                Object.DestroyImmediate(a.gameObject);
                Object.DestroyImmediate(b.gameObject);
            }
        }

        [Test]
        public void 빼면_빠진다()
        {
            var field = new DoorField();
            var a = Place(new Vector3(0f, 0f, 0f));
            try
            {
                field.Add(a);

                field.Remove(a);

                Assert.AreEqual(0, field.All.Count);
            }
            finally
            {
                Object.DestroyImmediate(a.gameObject);
            }
        }

        // 같은 문을 실수로 두 번 등록해도(예: Construct가 재호출) 하나만 남아야 한다 —
        // 라운드 재로드 시 중복 등록을 막는 게 이 테스트의 취지(WindField와 같은 이유).
        [Test]
        public void 같은_문을_두번_넣어도_하나다()
        {
            var field = new DoorField();
            var a = Place(new Vector3(0f, 0f, 0f));
            try
            {
                field.Add(a);
                field.Add(a);

                Assert.AreEqual(1, field.All.Count);
            }
            finally
            {
                Object.DestroyImmediate(a.gameObject);
            }
        }

        [Test]
        public void 주입받으면_스스로_등록한다()
        {
            var field = new DoorField();
            var volume = Place(new Vector3(3f, 4f, 5f));
            try
            {
                volume.Construct(field);

                Assert.AreEqual(1, field.All.Count);
                Assert.AreSame(volume, field.All[0]);
            }
            finally
            {
                Object.DestroyImmediate(volume.gameObject);
            }
        }

        // ⚠️ "파괴되면 스스로 빠진다"는 여기서 EditMode로 못 짚는다: DoorVolume은 LaserVolume과
        // 같은 모양이라 [ExecuteAlways]가 없고, Unity는 Play 모드가 아니면 OnDestroy를 아예
        // 불러 주지 않는다(WindVolume이 [ExecuteAlways]를 붙인 바로 그 이유). 실제 라운드
        // 재로드는 항상 Play 모드 중에 일어나므로 런타임 동작엔 영향 없다 — 다만 이 사실은
        // EditMode 테스트로 강제로 통과시키지 않고 여기 기록만 남긴다(억지 통과 금지).

        [Test]
        public void ToDoor는_트랜스폼_위치와_인스펙터_값을_그대로_옮긴다()
        {
            var volume = Place(new Vector3(3f, 4f, 5f));
            volume.AxisAngleDegrees = 90f;
            try
            {
                var door = volume.ToDoor();

                Assert.AreEqual(3f, door.Center.X, 1e-5f);
                Assert.AreEqual(4f, door.Center.Y, 1e-5f);
                Assert.AreEqual(5f, door.Center.Z, 1e-5f);
                Assert.AreEqual(5f, door.HalfWidth, 1e-5f);
                Assert.AreEqual(5f, door.HalfDepth, 1e-5f);
                Assert.AreEqual(0.5f, door.Thickness, 1e-5f);
                Assert.AreEqual(Mathf.PI * 0.5f, door.AxisAngle, 1e-5f);
                Assert.AreEqual(20, door.Period);
                Assert.AreEqual(6, door.OpenTicks);
                Assert.AreEqual(4, door.MoveTicks);
                Assert.AreEqual(0, door.Phase);
            }
            finally
            {
                Object.DestroyImmediate(volume.gameObject);
            }
        }

        [Test]
        public void Pose는_열린_틱에서_패널을_구멍_밖으로_물린다()
        {
            var volume = Place(new Vector3(0f, 0f, 0f));
            var panelA = new GameObject("panel-a").transform;
            var panelB = new GameObject("panel-b").transform;
            panelA.SetParent(volume.transform);
            panelB.SetParent(volume.transform);
            volume.PanelA = panelA;
            volume.PanelB = panelB;
            try
            {
                // 열림 구간(openness=1) — 반쯤 열린 문(0)보다 패널이 구멍 중심에서 더 멀어야 한다.
                volume.Pose(0);
                float openDistanceA = Mathf.Abs(panelA.localPosition.x);

                volume.Pose(10); // 닫힘 구간(openness=0)
                float closedDistanceA = Mathf.Abs(panelA.localPosition.x);

                Assert.Greater(openDistanceA, closedDistanceA);
            }
            finally
            {
                Object.DestroyImmediate(panelA.gameObject);
                Object.DestroyImmediate(panelB.gameObject);
                Object.DestroyImmediate(volume.gameObject);
            }
        }

        // 패널 참조가 비어 있어도(굽기 전 씬, 테스트 등) 예외 없이 넘어가야 한다.
        [Test]
        public void 패널이_없으면_조용히_넘어간다()
        {
            var volume = Place(new Vector3(0f, 0f, 0f));
            try
            {
                Assert.DoesNotThrow(() => volume.Pose(0));
            }
            finally
            {
                Object.DestroyImmediate(volume.gameObject);
            }
        }
    }
}
