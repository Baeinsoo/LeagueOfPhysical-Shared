using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class CheckpointFieldTests
    {
        [Test]
        public void 비어_있으면_체크포인트가_없다()
        {
            //  옛 더미 코스 표 폴백은 맵과 함께 지웠다(10-07) — 맵이 스폰 표식을 둬야 한다.
            var field = new CheckpointField();
            Assert.AreEqual(0, field.Count);
            Assert.AreEqual(0, field.ShelfYs.Count);
            Assert.AreEqual(0, field.RespawnPoints.Count);
            Assert.AreEqual(0f, field.SpawnY);
        }

        [Test]
        public void 표식을_넣으면_높은_것부터_서고_맨_위가_스폰이다()
        {
            var field = new CheckpointField();
            field.Add(2000f, new Vector3(-25f, 2000f, -10f));
            field.Add(3600f, new Vector3(0f, 3600f, 0f));
            field.Add(3200f, new Vector3(0f, 3200f, 40f));

            CollectionAssert.AreEqual(new[] { 3600f, 3200f, 2000f }, field.ShelfYs);
            Assert.AreEqual(3600f, field.SpawnY);
            Assert.AreEqual(new Vector3(0f, 3200f, 40f), field.RespawnPoints[3200f]);
        }

        [Test]
        public void 죽은_자리_위의_가장_가까운_체크포인트로_돌아간다()
        {
            var field = new CheckpointField();
            field.Add(3600f, new Vector3(0f, 3600f, 0f));
            field.Add(3200f, new Vector3(0f, 3200f, 40f));
            field.Add(2000f, new Vector3(-25f, 2000f, -10f));

            Assert.AreEqual(3200f, SkydiveCheckpoints.LastPassedShelfY(2500f, field.ShelfYs, field.SpawnY));
            Assert.AreEqual(3600f, SkydiveCheckpoints.LastPassedShelfY(3400f, field.ShelfYs, field.SpawnY));
        }

        [Test]
        public void 뺀_것만_빠지고_다_빼면_빈다()
        {
            var field = new CheckpointField();
            var a = new Vector3(0f, 3600f, 0f);
            field.Add(3600f, a);
            Assert.IsFalse(field.Remove(3600f, new Vector3(1f, 3600f, 0f)), "다른 표식의 값으로는 안 빠진다");
            Assert.IsTrue(field.Remove(3600f, a));
            Assert.AreEqual(0, field.Count);
            Assert.AreEqual(0, field.ShelfYs.Count);
        }

        [Test]
        public void 표식은_주입될_때_자기_자리를_넣는다()
        {
            var go = new GameObject("Checkpoint");
            try
            {
                go.transform.position = new Vector3(0f, 1300f, -40f);
                var field = new CheckpointField();
                go.AddComponent<CheckpointMarker>().Construct(field);
                Assert.AreEqual(1, field.Count);
                Assert.AreEqual(new Vector3(0f, 1300f, -40f), field.RespawnPoints[1300f]);
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
