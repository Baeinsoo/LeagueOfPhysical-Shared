using NUnit.Framework;

namespace LOP.Tests
{
    public class FlappyAirflowFieldTests
    {
        [Test]
        public void 사각형_안이면_그_종류_밖이면_None()
        {
            var field = new FlappyAirflowField();
            field.Add(new FlappyAirflowRect(10f, 17f, -5f, 5f, FlappyAirflowKind.Up));
            Assert.AreEqual(FlappyAirflowKind.Up, field.Sample(12f, 0f));
            Assert.AreEqual(FlappyAirflowKind.None, field.Sample(18f, 0f));
        }

        [Test]
        public void 겹치면_등록_순서와_무관하게_x가_작은_쪽이_이긴다()
        {
            var a = new FlappyAirflowRect(10f, 20f, 0f, 10f, FlappyAirflowKind.Down);
            var b = new FlappyAirflowRect(15f, 25f, 0f, 10f, FlappyAirflowKind.Up);
            var ab = new FlappyAirflowField(); ab.Add(a); ab.Add(b);
            var ba = new FlappyAirflowField(); ba.Add(b); ba.Add(a);
            Assert.AreEqual(FlappyAirflowKind.Down, ab.Sample(17f, 5f));
            Assert.AreEqual(FlappyAirflowKind.Down, ba.Sample(17f, 5f));
        }

        [Test]
        public void 뺀_사각형은_더_안_잡힌다()
        {
            var r = new FlappyAirflowRect(0f, 5f, 0f, 5f, FlappyAirflowKind.Up);
            var field = new FlappyAirflowField(); field.Add(r); field.Remove(r);
            Assert.AreEqual(FlappyAirflowKind.None, field.Sample(1f, 1f));
        }
    }
}
