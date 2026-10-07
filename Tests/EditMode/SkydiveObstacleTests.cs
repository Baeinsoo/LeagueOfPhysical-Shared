using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>원통 맵의 대형 장애물 — 틱만 넣으면 자세가 나와 클·서·되감기가 같은 답을 낸다(문과 같은 성질).</summary>
    public class SkydiveObstacleTests
    {
        [Test]
        public void 도는_장애물의_각도는_틱만으로_정해진다()
        {
            Assert.AreEqual(70f, SpinnerVolume.AngleAt(10f, 0.6f, 100), 0.001f);
            Assert.AreEqual(10f, SpinnerVolume.AngleAt(10f, 0.6f, 600), 0.001f, "한 바퀴 돌면 제자리");
            Assert.AreEqual(355f, SpinnerVolume.AngleAt(10f, -0.6f, 25), 0.001f, "거꾸로 돌아도 0~360");
            Assert.AreEqual(70.3f, SpinnerVolume.AngleAt(10f, 0.6f, 100.5), 0.001f, "그림은 틱 사이도 담는다");
        }

        [Test]
        public void 조리개는_문과_같은_박자로_열리고_닫힌다()
        {
            //  주기 200 = 열림 80 → 닫힘 20틱 → 닫힘 80 → 열림 20틱
            Assert.AreEqual(1f, IrisVolume.OpennessAt(200, 80, 20, 0, 10), 0.001f);
            Assert.AreEqual(0.5f, IrisVolume.OpennessAt(200, 80, 20, 0, 90), 0.001f);
            Assert.AreEqual(0f, IrisVolume.OpennessAt(200, 80, 20, 0, 150), 0.001f);
        }

        [Test]
        public void 도는_장애물을_세우면_트랜스폼이_그_각도로_돈다()
        {
            var go = new GameObject("spinner");
            try
            {
                var s = go.AddComponent<SpinnerVolume>();
                s.StartDegrees = 0f;
                s.DegreesPerTick = 1f;
                s.Pose(90);
                Assert.AreEqual(90f, go.transform.localEulerAngles.y, 0.01f);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void 조리개를_세우면_날개가_바깥으로_물러난다()
        {
            var go = new GameObject("iris");
            var blade = new GameObject("blade").transform;
            try
            {
                blade.SetParent(go.transform, false);
                blade.localPosition = new Vector3(10f, 0f, 0f);   // 닫힌 자리
                var iris = go.AddComponent<IrisVolume>();
                iris.Travel = 20f;
                iris.Period = 200; iris.OpenTicks = 80; iris.MoveTicks = 20;
                iris.Blades = new[] { blade };
                iris.Capture();   // 닫힌 자리를 기억

                iris.Pose(10);    // 활짝 열림
                Assert.AreEqual(30f, blade.localPosition.x, 0.001f);
                iris.Pose(150);   // 닫힘
                Assert.AreEqual(10f, blade.localPosition.x, 0.001f);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void 장애물_모음은_한_번만_담는다()
        {
            var go = new GameObject("spinner");
            try
            {
                var s = go.AddComponent<SpinnerVolume>();
                var field = new ObstacleField();
                field.Add(s);
                field.Add(s);
                Assert.AreEqual(1, field.All.Count);
                field.Remove(s);
                Assert.AreEqual(0, field.All.Count);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
