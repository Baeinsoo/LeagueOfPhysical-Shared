using NUnit.Framework;

namespace LOP.Tests
{
    public class ArcheryCatapultSeatsTests
    {
        [Test]
        public void 첫_라운드는_입장_순서다()
        {
            for (int i = 0; i < 8; i++)
            {
                Assert.AreEqual(i, ArcheryCatapultSeats.SlotOf(i, 0, 8, 123UL));
            }
        }

        [Test]
        public void 라운드마다_모두가_서로_다른_자리를_받는다()
        {
            for (int round = 1; round <= 2; round++)
            {
                var used = new bool[8];
                for (int i = 0; i < 8; i++)
                {
                    int slot = ArcheryCatapultSeats.SlotOf(i, round, 8, 123UL);
                    Assert.That(slot, Is.InRange(0, 7));
                    Assert.IsFalse(used[slot], $"라운드 {round} 자리 {slot}가 두 번");
                    used[slot] = true;
                }
            }
        }

        [Test]
        public void 같은_씨앗이면_같은_자리다()
        {
            for (int i = 0; i < 5; i++)
            {
                Assert.AreEqual(ArcheryCatapultSeats.SlotOf(i, 2, 5, 9UL), ArcheryCatapultSeats.SlotOf(i, 2, 5, 9UL));
            }
        }

        [Test]
        public void 섞기는_실제로_자리를_바꾼다()
        {
            //  양성 대조 — 씨앗 여럿 중 하나라도 입장 순서와 다르면 섞기가 살아 있다.
            int moved = 0;
            for (ulong seed = 1; seed <= 10; seed++)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (ArcheryCatapultSeats.SlotOf(i, 1, 4, seed) != i) { moved++; }
                }
            }
            Assert.Greater(moved, 10);
        }

        [Test]
        public void 한_명이면_늘_가운데_자리_0이다()
        {
            Assert.AreEqual(0, ArcheryCatapultSeats.SlotOf(0, 2, 1, 5UL));
            Assert.AreEqual(0, ArcheryCatapultSeats.SlotOf(0, 2, 0, 5UL));
        }
    }
}
