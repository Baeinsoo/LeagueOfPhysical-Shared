using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class KnockbackShotTests
    {
        const float Speed = 17f;
        const float Dt = 0.02f;

        [Test]
        public void 탄은_직선으로_간다()
        {
            var shot = new KnockbackShot("a", 0, Vector2.zero, new Vector2(1, 0));
            var p = shot.PositionAt(10, Speed, Dt);
            Assert.AreEqual(3.4f, p.x, 1e-4f);
            Assert.AreEqual(0f, p.y, 1e-4f);
        }

        [Test]
        public void 방향은_단위_벡터로_정규화된다()
        {
            var shot = new KnockbackShot("a", 0, Vector2.zero, new Vector2(3, 4));
            Assert.AreEqual(1f, shot.Direction.magnitude, 1e-5f);
        }

        [Test]
        public void 사거리를_넘으면_다_쓴_탄()
        {
            var shot = new KnockbackShot("a", 0, Vector2.zero, new Vector2(1, 0));
            Assert.IsFalse(shot.IsSpent(38, Speed, 13f, Dt));
            Assert.IsTrue(shot.IsSpent(39, Speed, 13f, Dt));
        }

        [Test]
        public void 같은_탄은_한_번만()
        {
            var book = new KnockbackShotBook();
            var shot = new KnockbackShot("a", 5, Vector2.zero, Vector2.right);
            Assert.IsTrue(book.Add(shot));
            Assert.IsFalse(book.Add(shot));
            Assert.AreEqual(1, book.Shots.Count);
        }

        [Test]
        public void 목록은_발사_틱_다음_쏜_사람_순으로_정렬된다()
        {
            var book = new KnockbackShotBook();
            book.Add(new KnockbackShot("b", 7, Vector2.zero, Vector2.right));
            book.Add(new KnockbackShot("b", 3, Vector2.zero, Vector2.right));
            book.Add(new KnockbackShot("a", 7, Vector2.zero, Vector2.right));
            Assert.AreEqual(3, book.Shots[0].FireTick);
            Assert.AreEqual("a", book.Shots[1].ShooterId);
            Assert.AreEqual("b", book.Shots[2].ShooterId);
        }

        [Test]
        public void 복원은_저장_때로()
        {
            var book = new KnockbackShotBook();
            book.Add(new KnockbackShot("a", 5, Vector2.zero, Vector2.right));
            var saved = book.Capture();
            Assert.IsTrue(book.Remove("a", 5));
            Assert.AreEqual(0, book.Shots.Count);
            book.Restore(saved);
            Assert.AreEqual(1, book.Shots.Count);
        }

        [Test]
        public void 다_쓴_탄만_정리된다()
        {
            var book = new KnockbackShotBook();
            book.Add(new KnockbackShot("a", 0, Vector2.zero, Vector2.right));
            book.Add(new KnockbackShot("a", 30, Vector2.zero, Vector2.right));
            book.RemoveSpent(39, Speed, 13f, Dt);
            Assert.AreEqual(1, book.Shots.Count);
            Assert.AreEqual(30, book.Shots[0].FireTick);
        }

        [Test]
        public void 빠른_탄이_몸을_건너뛰지_않는다()
        {
            Assert.IsTrue(KnockbackHitTest.SegmentHitsCircle(new Vector2(-1, 0), new Vector2(1, 0),
                new Vector2(0, 0.5f), 0.88f, out float t));
            Assert.Greater(t, 0f);
            Assert.Less(t, 1f);
        }

        [Test]
        public void 비켜_가면_안_맞는다()
        {
            Assert.IsFalse(KnockbackHitTest.SegmentHitsCircle(new Vector2(-1, 0), new Vector2(1, 0),
                new Vector2(0, 0.9f), 0.88f, out _));
        }

        [Test]
        public void 선분_밖의_원은_안_맞는다()
        {
            Assert.IsFalse(KnockbackHitTest.SegmentHitsCircle(new Vector2(-1, 0), new Vector2(1, 0),
                new Vector2(5, 0), 0.88f, out _));
        }

        [Test]
        public void 탄_사건은_와이어를_다녀와도_같다()
        {
            var shot = new KnockbackShotFiredEvent("a", 42, new Vector2(1.5f, -2f), new Vector2(0, 1));
            var back = (KnockbackShotFiredEvent)WorldEventWire.FromWire(WorldEventWire.ToWire(shot));
            Assert.AreEqual(shot, back);

            var hit = new KnockbackHitEvent("a", 42, "v");
            var hitBack = (KnockbackHitEvent)WorldEventWire.FromWire(WorldEventWire.ToWire(hit));
            Assert.AreEqual(hit, hitBack);
        }
    }
}
