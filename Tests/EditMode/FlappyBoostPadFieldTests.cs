using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// 부스트 패드는 <b>산술 포함 판정</b>만 한다 — 트리거 콜라이더로 판정하면 클·서가 다른 틱에
    /// 다른 답을 내고, 롤백 재생에서는 물리를 안 돌려 아예 답이 없다.
    ///
    /// <para>겹친 패드에서 어느 쪽이 이기는지도 못박는다. 등록 순서가 결과를 바꾸면 결정론이 깨진다.</para>
    /// </summary>
    public class FlappyBoostPadFieldTests
    {
        static FlappyBoostPadField Field(params FlappyBoostRect[] rects)
        {
            var field = new FlappyBoostPadField();
            foreach (var rect in rects)
            {
                field.Add(rect);
            }
            return field;
        }

        static FlappyBoostRect Rect(float x0, float x1, float y0, float y1, float duration)
        {
            return new FlappyBoostRect(x0, x1, y0, y1, duration);
        }

        [Test]
        public void 안에_있으면_지속시간을_준다()
        {
            var field = Field(Rect(10f, 20f, -5f, 5f, 0.6f));
            Assert.IsTrue(field.TryDuration(15f, 0f, out float duration));
            Assert.AreEqual(0.6f, duration, 1e-5f);
        }

        [Test]
        public void 밖이면_주지_않는다()
        {
            var field = Field(Rect(10f, 20f, -5f, 5f, 0.6f));
            Assert.IsFalse(field.TryDuration(9.9f, 0f, out _));
            Assert.IsFalse(field.TryDuration(15f, 5.1f, out _));
        }

        [Test]
        public void 경계는_안쪽이다()
        {
            //  틱 경계에서 스칠 때 클·서가 다르게 판정하면 안 된다 — 포함 규칙을 한쪽으로 고정한다.
            var field = Field(Rect(10f, 20f, -5f, 5f, 0.6f));
            Assert.IsTrue(field.TryDuration(10f, -5f, out _));
            Assert.IsTrue(field.TryDuration(20f, 5f, out _));
        }

        [Test]
        public void 뒤집어_놓아도_같은_사각형이다()
        {
            //  씬에서 음수 스케일로 놓거나 값을 거꾸로 넣어도 같은 자리를 덮어야 한다.
            var field = Field(Rect(20f, 10f, 5f, -5f, 0.6f));
            Assert.IsTrue(field.TryDuration(15f, 0f, out _));
        }

        [Test]
        public void 겹치면_긴_쪽이_이긴다()
        {
            //  등록 순서가 결과를 바꾸면 결정론이 깨진다. 둘 다 넣어 보고 같은 답인지 본다.
            var a = Field(Rect(0f, 10f, -5f, 5f, 0.4f), Rect(5f, 15f, -5f, 5f, 0.9f));
            var b = Field(Rect(5f, 15f, -5f, 5f, 0.9f), Rect(0f, 10f, -5f, 5f, 0.4f));

            Assert.IsTrue(a.TryDuration(7f, 0f, out float da));
            Assert.IsTrue(b.TryDuration(7f, 0f, out float db));
            Assert.AreEqual(0.9f, da, 1e-5f);
            Assert.AreEqual(da, db, 1e-5f);
        }

        [Test]
        public void 패드가_없으면_조용하다()
        {
            Assert.IsFalse(new FlappyBoostPadField().TryDuration(0f, 0f, out _));
        }

        [Test]
        public void 빼면_사라진다()
        {
            //  라운드가 여러 판이면 맵을 다시 로드한다 — 안 빠지면 죽은 패드가 남는다.
            var rect = Rect(10f, 20f, -5f, 5f, 0.6f);
            var field = Field(rect);
            Assert.IsTrue(field.Remove(rect));
            Assert.IsFalse(field.TryDuration(15f, 0f, out _));
            Assert.AreEqual(0, field.Count);
        }

        [Test]
        public void 같은_것을_두_번_넣지_않는다()
        {
            var rect = Rect(10f, 20f, -5f, 5f, 0.6f);
            var field = Field(rect, rect);
            Assert.AreEqual(1, field.Count);
        }
    }
}
