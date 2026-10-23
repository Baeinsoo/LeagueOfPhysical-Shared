using System.Collections.Generic;
using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// 빛 알갱이의 "세로줄을 지나는 순간" 판정. 월드에 물려 있는지는 <see cref="FlappyWorldMoteTests"/>가 본다.
    /// </summary>
    public class FlappyMoteFieldTests
    {
        static FlappyMoteField Field(params (float x, float y)[] motes)
        {
            var xs = new float[motes.Length];
            var ys = new float[motes.Length];
            for (int i = 0; i < motes.Length; i++)
            {
                xs[i] = motes[i].x;
                ys[i] = motes[i].y;
            }
            var field = new FlappyMoteField();
            field.Add(new FlappyMoteSet(xs, ys, big: false));
            return field;
        }

        [Test]
        public void 구간_끝은_넣고_시작은_뺀다()
        {
            //  (x0, x1] — 한 틱의 끝이 다음 틱의 시작이므로, 양쪽을 다 넣으면 경계의 알갱이를 두 번 먹는다.
            var field = Field((1f, 0f));

            Assert.AreEqual(1, field.CountCrossed(0f, 0f, 1f, 0f, 1f));
            Assert.AreEqual(0, field.CountCrossed(1f, 0f, 2f, 0f, 1f));
        }

        [Test]
        public void 연속한_구간으로_나눠_지나도_한_번만_센다()
        {
            var field = Field((0.3f, 0f), (0.6f, 0f));
            int total = 0;
            for (int i = 0; i < 10; i++)
            {
                total += field.CountCrossed(i * 0.1f, 0f, (i + 1) * 0.1f, 0f, 1f);
            }
            Assert.AreEqual(2, total);
        }

        [Test]
        public void 높이_차가_닿는_거리_안이면_먹고_밖이면_안_먹는다()
        {
            var field = Field((1f, 0.94f), (1f, -0.96f));

            Assert.AreEqual(1, field.CountCrossed(0f, 0f, 2f, 0f, 0.95f));
        }

        [Test]
        public void 그_순간의_높이는_시작과_끝_높이를_x_비율로_나눠_구한다()
        {
            //  y가 0 → 4로 오르는 동안 x=1(4분의 1 지점)을 지나므로 그때 높이는 1이다.
            var field = Field((1f, 1f));

            Assert.AreEqual(1, field.CountCrossed(0f, 0f, 4f, 4f, 0.01f));
            Assert.AreEqual(0, field.CountCrossed(0f, 0f, 4f, 0f, 0.01f));
        }

        [Test]
        public void 뒤로_가거나_멈춰_있으면_아무것도_안_먹는다()
        {
            var field = Field((1f, 0f));

            Assert.AreEqual(0, field.CountCrossed(2f, 0f, 0f, 0f, 1f));
            Assert.AreEqual(0, field.CountCrossed(1f, 0f, 1f, 0f, 1f));
        }

        [Test]
        public void 여러_무더기를_한_번에_지나면_전부_센다_먹은_자리도_돌려준다()
        {
            var field = new FlappyMoteField();
            field.Add(new FlappyMoteSet(new[] { 3f, 1f }, new[] { 0f, 0f }, big: false));
            field.Add(new FlappyMoteSet(new[] { 2f }, new[] { 0f }, big: true));
            var hits = new List<(float x, float y)>();

            Assert.AreEqual(3, field.CountCrossed(0f, 0f, 5f, 0f, 1f, hits));
            CollectionAssert.AreEqual(new[] { (1f, 0f), (2f, 0f), (3f, 0f) }, hits);
        }

        [Test]
        public void 무더기를_빼면_더는_안_먹는다()
        {
            var field = new FlappyMoteField();
            var set = new FlappyMoteSet(new[] { 1f }, new[] { 0f }, big: false);
            field.Add(set);
            Assert.AreEqual(1, field.Count);

            field.Remove(set);

            Assert.AreEqual(0, field.Count);
            Assert.AreEqual(0, field.CountCrossed(0f, 0f, 2f, 0f, 1f));
        }
    }
}
