using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 결승선 바운드 홀더. 맵 마커가 스스로 등록하고, 마커가 없는 맵을 위해 폴백 좌표를 받는다
    /// (Skydive가 지면 높이를 넘긴다).
    /// </summary>
    public class FinishLineBoundsTests
    {
        [Test]
        public void 등록하면_그_바운드를_준다()
        {
            var line = new FinishLineBounds(FinishAxis.X);
            line.Register(new Bounds(new Vector3(100f, 0f, 0f), new Vector3(2f, 50f, 2f)));

            Assert.IsTrue(line.TryGet(out Bounds bounds));
            Assert.That(bounds.min.x, Is.EqualTo(99f).Within(1e-3f));
        }

        [Test]
        public void 아무것도_등록_안_했고_폴백도_없으면_없다고_한다()
        {
            Assert.IsFalse(new FinishLineBounds(FinishAxis.X).TryGet(out _));
        }

        [Test]
        public void 폴백만_있으면_두께_0인_선이다()
        {
            var line = new FinishLineBounds(FinishAxis.Y, fallbackCoordinate: 12f);

            Assert.IsTrue(line.TryGet(out Bounds bounds));
            Assert.That(bounds.min.y, Is.EqualTo(12f).Within(1e-3f));
            Assert.That(bounds.max.y, Is.EqualTo(12f).Within(1e-3f));
        }

        [Test]
        public void 등록된_것이_폴백보다_우선이다()
        {
            var line = new FinishLineBounds(FinishAxis.X, fallbackCoordinate: 5f);
            line.Register(new Bounds(new Vector3(100f, 0f, 0f), new Vector3(2f, 50f, 2f)));

            line.TryGet(out Bounds bounds);
            Assert.That(bounds.min.x, Is.EqualTo(99f).Within(1e-3f));
        }

        [Test]
        public void 등록을_거두면_다시_없다()
        {
            //  라운드가 여러 판이면 맵을 다시 로드한다 — 옛 마커가 남아 있으면 안 된다.
            var line = new FinishLineBounds(FinishAxis.X);
            line.Register(new Bounds(new Vector3(100f, 0f, 0f), new Vector3(2f, 50f, 2f)));
            line.Unregister();

            Assert.IsFalse(line.TryGet(out _));
        }
    }
}
