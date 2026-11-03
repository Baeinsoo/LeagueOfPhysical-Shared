using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LOP.Tests
{
    public class KnockbackCrackFloorTests
    {
        static KnockbackFloor Floor(long crackWarn = 50, bool hazard = false)
            => new KnockbackFloor(17f, 1.5f, 0f, hazard, 0, crackWarn);

        static KnockbackConfig Config(int enabled = 0, int hits = 2, float seconds = 1f)
            => new KnockbackConfig(120f, 6f, 10f, 12f, 2f, 0.5f, 5f, 11f, 30f, 18f, 0.3f, 0.55f, 3, 0.95f, 0.26f, 1.2f, 12, 6f, 0.2f, 0.5f, 0.3f, 0.1f, 0.2f, 0.8f, 8f,
                new List<MountedArcheryZoneStageConfig>(), null, 0, 0f, 1f, 20f, 20f, 1.5f, 0.9f, 2.5f, enabled, hits, seconds);

        // ---- 설정 ----

        [Test]
        public void 설정_금_켜짐과_값을_그대로_읽는다()
        {
            var on = Config(1, 3, 1.5f);
            Assert.IsTrue(on.CrackEnabled);
            Assert.AreEqual(3, on.CrackHitsToCollapse);
            Assert.AreEqual(1.5f, on.CrackCollapseSeconds);
            Assert.AreEqual(75, on.CrackCollapseTicks);
            Assert.IsFalse(Config(0).CrackEnabled);
        }

        [Test]
        public void 설정_횟수가_0_이하면_기본값과_오류_로그()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("crack_hits_to_collapse"));
            Assert.AreEqual(2, Config(1, 0).CrackHitsToCollapse);
        }

        [Test]
        public void 설정_무너지는_시간이_0_이하면_기본값과_오류_로그()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("crack_collapse_seconds"));
            Assert.AreEqual(1f, Config(1, 2, 0f).CrackCollapseSeconds);
        }

        [Test]
        public void 설정_켜짐_값이_0과_1이_아니면_끄고_오류_로그()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("crack_enabled"));
            Assert.IsFalse(Config(2).CrackEnabled);
        }

        [Test]
        public void 설정이_만든_바닥은_깜박임_틱을_들고_있다()
        {
            var floor = Config(1, 2, 1f).CreateFloor();
            int i = floor.TileIndexAt(3f, 3f);
            floor.SetCollapseTick(i, 200);
            var (x, z) = floor.Tiles[i];
            Assert.IsTrue(floor.IsWarning(x, z, 150, 0));
            Assert.IsFalse(floor.IsWarning(x, z, 149, 0));
        }

        // ---- 무너지는 틱 ----

        [Test]
        public void 무너지는_틱_전엔_서_있고_그_틱부터_안_선다()
        {
            var floor = Floor();
            int i = floor.TileIndexAt(3f, 3f);
            floor.SetCollapseTick(i, 100);
            Assert.IsTrue(floor.IsSolid(3f, 3f, 99));
            Assert.IsFalse(floor.IsSolid(3f, 3f, 100));
            Assert.IsFalse(floor.IsSolid(3f, 3f, 5000));
        }

        [Test]
        public void 다른_칸은_영향이_없다()
        {
            var floor = Floor();
            floor.SetCollapseTick(floor.TileIndexAt(3f, 3f), 100);
            Assert.IsTrue(floor.IsSolid(0.1f, 0.1f, 5000));
            Assert.IsTrue(floor.IsSolid(4.6f, 3f, 5000));
        }

        [Test]
        public void 되감아_옛_틱을_물어도_그_틱_그대로()
        {
            var floor = Floor();
            floor.SetCollapseTick(floor.TileIndexAt(3f, 3f), 100);
            Assert.IsFalse(floor.IsSolid(3f, 3f, 150));
            Assert.IsTrue(floor.IsSolid(3f, 3f, 50));
            Assert.IsFalse(floor.IsSolid(3f, 3f, 150));
        }

        [Test]
        public void 더_이른_틱이_이긴다()
        {
            var floor = Floor();
            int i = floor.TileIndexAt(3f, 3f);
            floor.SetCollapseTick(i, 100);
            floor.SetCollapseTick(i, 300);
            Assert.AreEqual(100, floor.CollapseTickOf(i));
            floor.SetCollapseTick(i, 60);
            Assert.AreEqual(60, floor.CollapseTickOf(i));
            Assert.IsTrue(floor.IsSolid(3f, 3f, 59));
            Assert.IsFalse(floor.IsSolid(3f, 3f, 60));
        }

        [Test]
        public void 지우면_다시_선다()
        {
            var floor = Floor();
            int i = floor.TileIndexAt(3f, 3f);
            floor.SetCollapseTick(i, 100);
            Assert.IsFalse(floor.IsSolid(3f, 3f, 150));
            floor.ClearCollapses();
            Assert.AreEqual(KnockbackFloor.NoCollapse, floor.CollapseTickOf(i));
            Assert.IsTrue(floor.IsSolid(3f, 3f, 150));
        }

        static ZoneStage Shrink(KnockbackFloor floor)
            => new ZoneStage(0, 0, 10, 20, Vector2.zero, floor.ZoneStartRadius, Vector2.zero, 0f, 4f);

        [Test]
        public void 원_규칙이_더_이르면_원이_이긴다()
        {
            var floor = new KnockbackFloor(17f, 1.5f, 0f, false, 0, 50);
            floor.SetZoneStages(new[] { Shrink(floor) });
            floor.SetCollapseTick(floor.TileIndexAt(10f, 0f), 1000);
            long circleGone = -1;
            for (long t = 0; t < 1000; t++) { if (!floor.IsSolid(10f, 0f, t)) { circleGone = t; break; } }
            Assert.Greater(circleGone, 0);
            Assert.Less(circleGone, 1000);
            Assert.IsTrue(floor.IsSolid(10f, 0f, circleGone - 1));
        }

        [Test]
        public void 금이_더_이르면_금이_이긴다()
        {
            var floor = new KnockbackFloor(17f, 1.5f, 0f, false, 0, 50);
            floor.SetZoneStages(new[] { Shrink(floor) });
            floor.SetCollapseTick(floor.TileIndexAt(10f, 0f), 1);
            Assert.IsTrue(floor.IsSolid(10f, 0f, 0));
            Assert.IsFalse(floor.IsSolid(10f, 0f, 1));
        }

        [Test]
        public void 위험_지대_방식에서도_금_칸은_무너진다()
        {
            var floor = Floor(50, true);
            floor.SetCollapseTick(floor.TileIndexAt(3f, 3f), 100);
            Assert.IsTrue(floor.IsSolid(3f, 3f, 99));
            Assert.IsFalse(floor.IsSolid(3f, 3f, 100));
        }

        [Test]
        public void 없는_칸_번호는_예외()
        {
            var floor = Floor();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => floor.SetCollapseTick(-1, 5));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => floor.SetCollapseTick(floor.Tiles.Count, 5));
        }

        // ---- 깜박임 ----

        [Test]
        public void 무너지기_전_예고_구간에만_깜박인다()
        {
            var floor = Floor(50);
            int i = floor.TileIndexAt(3f, 3f);
            var (x, z) = floor.Tiles[i];
            floor.SetCollapseTick(i, 200);
            Assert.IsFalse(floor.IsWarning(x, z, 149, 0));
            Assert.IsTrue(floor.IsWarning(x, z, 150, 0));
            Assert.IsTrue(floor.IsWarning(x, z, 199, 0));
            Assert.IsFalse(floor.IsWarning(x, z, 200, 0));
        }

        [Test]
        public void 금이_없는_칸은_깜박이지_않는다()
        {
            var floor = Floor(50);
            var (x, z) = floor.Tiles[floor.TileIndexAt(3f, 3f)];
            Assert.IsFalse(floor.IsWarning(x, z, 100, 0));
        }

        [Test]
        public void 깜박임_틱이_0이면_금_규칙으론_깜박이지_않는다()
        {
            var floor = Floor(0);
            int i = floor.TileIndexAt(3f, 3f);
            var (x, z) = floor.Tiles[i];
            floor.SetCollapseTick(i, 200);
            Assert.IsFalse(floor.IsWarning(x, z, 199, 0));
        }

        // ---- 자리 -> 칸 번호 ----

        [Test]
        public void 칸_번호는_칸_목록_순서와_같다()
        {
            var floor = Floor();
            for (int i = 0; i < floor.Tiles.Count; i++)
            {
                var (x, z) = floor.Tiles[i];
                Assert.AreEqual(i, floor.TileIndex(x, z));
                var c = floor.TileCenter(x, z);
                Assert.AreEqual(i, floor.TileIndexAt(c.x, c.z));
            }
        }

        [Test]
        public void 칸_경계와_원판_밖_구멍은_번호가_없다()
        {
            var floor = Floor();
            // 한 칸 크기 1.5: x=1.5는 다음 칸, 1.4999는 앞 칸.
            Assert.AreEqual(floor.TileIndex(1, 0), floor.TileIndexAt(1.5f, 0.1f));
            Assert.AreEqual(floor.TileIndex(0, 0), floor.TileIndexAt(1.4999f, 0.1f));
            Assert.AreEqual(floor.TileIndex(-1, 0), floor.TileIndexAt(-0.0001f, 0.1f));
            Assert.AreEqual(-1, floor.TileIndexAt(18f, 0f));
            Assert.AreEqual(-1, floor.TileIndexAt(0f, -40f));
            var holed = new KnockbackFloor(17f, 1.5f, 2.6f);
            Assert.AreEqual(-1, holed.TileIndexAt(0.1f, 0.1f));
            Assert.GreaterOrEqual(holed.TileIndexAt(3.5f, 0.5f), 0);
        }
    }
}
