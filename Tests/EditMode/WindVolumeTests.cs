using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class WindVolumeTests
    {
        const float Tolerance = 1e-3f;

        static WindVolume Place(Vector3 position, Vector3 wind, float radius = 25f, float height = 120f)
        {
            var go = new GameObject("wind-test");
            go.transform.position = position;
            var volume = go.AddComponent<WindVolume>();
            volume.Radius = radius;
            volume.Height = height;
            volume.Wind = wind;
            return volume;
        }

        [Test]
        public void 주입받으면_스스로_등록한다()
        {
            var field = new WindField();
            var volume = Place(new Vector3(0f, 1000f, 0f), new Vector3(0f, 14f, 0f));
            try
            {
                volume.Construct(field);

                Assert.AreEqual(1, field.Count);
                Assert.AreEqual(14f,
                    field.SampleAt(new System.Numerics.Vector3(0f, 1000f, 0f)).Y, Tolerance);
            }
            finally
            {
                Object.DestroyImmediate(volume.gameObject);
            }
        }

        // 라운드가 여러 판이면 맵을 다시 로드한다. 안 빼면 바람이 두 배가 된다.
        [Test]
        public void 파괴되면_스스로_빠진다()
        {
            var field = new WindField();
            var volume = Place(new Vector3(0f, 1000f, 0f), new Vector3(0f, 14f, 0f));
            volume.Construct(field);

            Object.DestroyImmediate(volume.gameObject);

            Assert.AreEqual(0, field.Count);
        }

        [Test]
        public void 마커_위치가_원기둥_중심이_된다()
        {
            var field = new WindField();
            var volume = Place(new Vector3(30f, 1900f, 30f), new Vector3(0f, 14f, 0f), radius: 25f);
            try
            {
                volume.Construct(field);

                Assert.AreEqual(14f,
                    field.SampleAt(new System.Numerics.Vector3(30f, 1900f, 30f)).Y, Tolerance);
                Assert.AreEqual(0f,
                    field.SampleAt(new System.Numerics.Vector3(0f, 1900f, 0f)).Length(), Tolerance);
            }
            finally
            {
                Object.DestroyImmediate(volume.gameObject);
            }
        }
    }
}
