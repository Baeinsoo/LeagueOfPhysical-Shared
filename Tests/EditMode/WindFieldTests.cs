using NUnit.Framework;
using System.Numerics;

namespace LOP.Tests
{
    public class WindFieldTests
    {
        const float Tolerance = 1e-4f;

        static WindCylinder Updraft(float y, float radius = 10f, float height = 100f, float up = 14f)
            => new WindCylinder(new Vector3(0f, y, 0f), radius, height, new Vector3(0f, up, 0f));

        [Test]
        public void 빈_필드는_0을_준다()
        {
            var field = new WindField();
            Assert.AreEqual(0f, field.SampleAt(new Vector3(0f, 1000f, 0f)).Length(), Tolerance);
        }

        [Test]
        public void 안에_있으면_그_바람이_나온다()
        {
            var field = new WindField();
            field.Add(Updraft(1000f));

            var wind = field.SampleAt(new Vector3(0f, 1000f, 0f));

            Assert.AreEqual(14f, wind.Y, Tolerance);
        }

        [Test]
        public void 가로로_벗어나면_0이다()
        {
            var field = new WindField();
            field.Add(Updraft(1000f, radius: 10f));

            Assert.AreEqual(0f, field.SampleAt(new Vector3(10.1f, 1000f, 0f)).Length(), Tolerance);
        }

        [Test]
        public void 세로로_벗어나면_0이다()
        {
            var field = new WindField();
            field.Add(Updraft(1000f, height: 100f));

            Assert.AreEqual(0f, field.SampleAt(new Vector3(0f, 1050.1f, 0f)).Length(), Tolerance);
        }

        [Test]
        public void 경계는_포함이다()
        {
            var field = new WindField();
            field.Add(Updraft(1000f, radius: 10f, height: 100f));

            Assert.AreEqual(14f, field.SampleAt(new Vector3(10f, 1050f, 0f)).Y, Tolerance);
        }

        [Test]
        public void 겹친_볼륨은_더해진다()
        {
            var field = new WindField();
            field.Add(Updraft(1000f));
            field.Add(new WindCylinder(new Vector3(0f, 1000f, 0f), 10f, 100f, new Vector3(5f, 0f, 0f)));

            var wind = field.SampleAt(new Vector3(0f, 1000f, 0f));

            Assert.AreEqual(5f, wind.X, Tolerance);
            Assert.AreEqual(14f, wind.Y, Tolerance);
        }

        // 등록 순서는 씬 순회 순서라 정해져 있지 않은데, 부동소수 덧셈은 순서가 바뀌면
        // 마지막 자릿수가 바뀐다. 클·서가 그것 때문에 갈리면 안 된다.
        [Test]
        public void 등록_순서가_달라도_같은_값이_나온다()
        {
            var a = new WindCylinder(new Vector3(0f, 1000f, 0f), 50f, 100f, new Vector3(0.1f, 0f, 0f));
            var b = new WindCylinder(new Vector3(0f, 1005f, 0f), 50f, 100f, new Vector3(0.2f, 0f, 0f));
            var c = new WindCylinder(new Vector3(0f, 995f, 0f), 50f, 100f, new Vector3(0.3f, 0f, 0f));

            var forward = new WindField();
            forward.Add(a); forward.Add(b); forward.Add(c);

            var backward = new WindField();
            backward.Add(c); backward.Add(b); backward.Add(a);

            var point = new Vector3(0f, 1000f, 0f);
            Assert.AreEqual(forward.SampleAt(point).X, backward.SampleAt(point).X);
        }

        [Test]
        public void 뺀_볼륨은_더는_안_센다()
        {
            var field = new WindField();
            var cylinder = Updraft(1000f);
            field.Add(cylinder);

            Assert.IsTrue(field.Remove(cylinder));
            Assert.AreEqual(0, field.Count);
            Assert.AreEqual(0f, field.SampleAt(new Vector3(0f, 1000f, 0f)).Length(), Tolerance);
        }

        [Test]
        public void 같은_볼륨을_두_번_넣어도_한_번만_센다()
        {
            var field = new WindField();
            var cylinder = Updraft(1000f);
            field.Add(cylinder);
            field.Add(cylinder);

            Assert.AreEqual(1, field.Count);
            Assert.AreEqual(14f, field.SampleAt(new Vector3(0f, 1000f, 0f)).Y, Tolerance);
        }
    }
}
