using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class ArcheryShotBookTests
    {
        static ArcheryShot Shot(string who, long tick)
            => new ArcheryShot(who, tick, Vector3.zero, Vector3.forward);

        [Test]
        public void 더하면_목록에_있다()
        {
            var book = new ArcheryShotBook();
            book.Add(Shot("a", 5));
            Assert.AreEqual(1, book.Shots.Count);
        }

        [Test]
        public void 취소는_구간_안의_그_사람_발만()
        {
            var book = new ArcheryShotBook();
            book.Add(Shot("a", 5));
            book.Add(Shot("a", 9));
            book.Add(Shot("b", 7));

            book.Cancel("a", 4, 6);

            Assert.AreEqual(2, book.Shots.Count);
            Assert.AreEqual(("a", 9L), (book.Shots[0].ShooterId, book.Shots[0].FireTick));
            Assert.AreEqual(("b", 7L), (book.Shots[1].ShooterId, book.Shots[1].FireTick));
        }

        [Test]
        public void 수명이_지나면_지운다()
        {
            var book = new ArcheryShotBook();
            book.Add(Shot("a", 0));

            book.RemoveExpired(150, 0.02f);
            Assert.AreEqual(1, book.Shots.Count);

            book.RemoveExpired(151, 0.02f);
            Assert.AreEqual(0, book.Shots.Count);
        }

        [Test]
        public void 복원은_취소된_발을_되살리지_않는다()
        {
            var book = new ArcheryShotBook();
            book.Add(Shot("a", 5));
            book.Add(Shot("a", 9));
            var saved = book.Capture();

            book.Cancel("a", 8, 9);
            book.Restore(saved);

            Assert.AreEqual(1, book.Shots.Count);
            Assert.AreEqual(5L, book.Shots[0].FireTick);
        }

        [Test]
        public void 복사본은_뒤의_변경에_안_흔들린다()
        {
            var book = new ArcheryShotBook();
            book.Add(Shot("a", 5));
            var saved = book.Capture();

            book.Add(Shot("a", 9));

            Assert.AreEqual(1, saved.Count);
        }
    }
}
