using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class ArcheryShootOffRankingTests
    {
        private static ArcheryRoundShot Hit(string id, float distance)
            => new ArcheryRoundShot(id, true, new Vector2(distance, 0f), distance);

        private static ArcheryRoundShot Miss(string id)
            => new ArcheryRoundShot(id, false, Vector2.zero, 0f);

        private static ArcheryRoundPlacement Of(List<ArcheryRoundPlacement> result, string id)
            => result.Find(p => p.ShooterId == id);

        [Test]
        public void 네_명이면_가까운_순서로_3_2_1_0점()
        {
            var result = ArcheryShootOffRanking.Rank(new[]
            {
                Hit("c", 0.30f), Hit("a", 0.05f), Hit("d", 0.50f), Hit("b", 0.10f),
            }, multiplier: 1);

            Assert.AreEqual(new[] { "a", "b", "c", "d" }, result.ConvertAll(p => p.ShooterId).ToArray());
            Assert.AreEqual(new[] { 3, 2, 1, 0 }, result.ConvertAll(p => p.Points).ToArray());
            Assert.AreEqual(new[] { 0, 1, 2, 3 }, result.ConvertAll(p => p.Rank).ToArray());
        }

        [Test]
        public void 거리가_같으면_공동_순위이고_둘_다_높은_점수()
        {
            var result = ArcheryShootOffRanking.Rank(new[]
            {
                Hit("a", 0.10f), Hit("b", 0.10f), Hit("c", 0.20f), Hit("d", 0.30f),
            }, 1);

            Assert.AreEqual(3, Of(result, "a").Points);
            Assert.AreEqual(3, Of(result, "b").Points);
            Assert.AreEqual(0, Of(result, "b").Rank);
            Assert.AreEqual(2, Of(result, "c").Rank);
            Assert.AreEqual(1, Of(result, "c").Points);
        }

        [Test]
        public void 못_맞힌_사람은_맞힌_사람_뒤이고_모두_0점()
        {
            var result = ArcheryShootOffRanking.Rank(new[]
            {
                Miss("a"), Hit("b", 0.40f), Miss("c"), Hit("d", 0.20f),
            }, 1);

            Assert.AreEqual(new[] { "d", "b", "a", "c" }, result.ConvertAll(p => p.ShooterId).ToArray());
            Assert.AreEqual(3, Of(result, "d").Points);
            Assert.AreEqual(2, Of(result, "b").Points);
            Assert.AreEqual(0, Of(result, "a").Points);
            Assert.AreEqual(0, Of(result, "c").Points);
            Assert.AreEqual(3, Of(result, "a").Rank);   // 공동 꼴찌 = 인원 − 1
            Assert.AreEqual(3, Of(result, "c").Rank);
        }

        [Test]
        public void 전원_못_맞히면_전원_0점()
        {
            var result = ArcheryShootOffRanking.Rank(new[] { Miss("a"), Miss("b") }, 2);
            Assert.IsTrue(result.TrueForAll(p => p.Points == 0 && p.Rank == 1));
        }

        [Test]
        public void 혼자면_맞혀도_0점()
        {
            var result = ArcheryShootOffRanking.Rank(new[] { Hit("a", 0f) }, 2);
            Assert.AreEqual(0, result[0].Points);
            Assert.AreEqual(0, result[0].Rank);
        }

        [Test]
        public void 배수_2면_점수가_두_배()
        {
            var result = ArcheryShootOffRanking.Rank(new[]
            {
                Hit("a", 0.1f), Hit("b", 0.2f), Hit("c", 0.3f), Hit("d", 0.4f),
            }, 2);
            Assert.AreEqual(new[] { 6, 4, 2, 0 }, result.ConvertAll(p => p.Points).ToArray());
        }

        [Test]
        public void 빈_목록이면_빈_결과()
        {
            Assert.AreEqual(0, ArcheryShootOffRanking.Rank(new ArcheryRoundShot[0], 1).Count);
        }
    }
}
