using System.Collections.Generic;
using NUnit.Framework;

namespace LOP.Tests
{
    public class ArcheryShootOffSeatsTests
    {
        [Test]
        public void 자리는_가운데를_중심으로_좌우_대칭()
        {
            float s = ArcheryShootOffSeats.SpacingMeters;
            Assert.AreEqual(0f, ArcheryShootOffSeats.LateralOffset(0, 1), 1e-5f);
            Assert.AreEqual(-s, ArcheryShootOffSeats.LateralOffset(0, 3), 1e-5f);
            Assert.AreEqual(0f, ArcheryShootOffSeats.LateralOffset(1, 3), 1e-5f);
            Assert.AreEqual(s, ArcheryShootOffSeats.LateralOffset(2, 3), 1e-5f);
            Assert.AreEqual(-1.5f * s, ArcheryShootOffSeats.LateralOffset(0, 4), 1e-5f);
            Assert.AreEqual(0.5f * s, ArcheryShootOffSeats.LateralOffset(2, 4), 1e-5f);
        }

        [Test]
        public void 라운드마다_한_칸씩_돌아_모두가_모든_자리를_같은_횟수만큼_쓴다()
        {
            const int players = 4, rounds = 12;
            for (int p = 0; p < players; p++)
            {
                var used = new Dictionary<int, int>();
                for (int r = 0; r < rounds; r++)
                {
                    int slot = ArcheryShootOffSeats.SlotOf(p, r, players);
                    used[slot] = used.TryGetValue(slot, out int n) ? n + 1 : 1;
                }
                Assert.AreEqual(players, used.Count, $"사람 {p}");
                foreach (var pair in used)
                {
                    Assert.AreEqual(rounds / players, pair.Value, $"사람 {p} 자리 {pair.Key}");
                }
            }
        }

        [Test]
        public void 한_라운드에_두_사람이_같은_자리에_서지_않는다()
        {
            for (int r = 0; r < 12; r++)
            {
                var taken = new HashSet<int>();
                for (int p = 0; p < 3; p++)
                {
                    Assert.IsTrue(taken.Add(ArcheryShootOffSeats.SlotOf(p, r, 3)), $"라운드 {r}");
                }
            }
        }

        [Test]
        public void 사람이_없으면_자리_0()
        {
            Assert.AreEqual(0, ArcheryShootOffSeats.SlotOf(0, 5, 0));
        }
    }
}
