using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class KnockbackFloorTests
    {
        [Test]
        public void 원판_경계_칸()
        {
            var floor = new KnockbackFloor(17f, 1.5f, 0f);
            Assert.IsTrue(floor.HasTile(10, 3));
            Assert.IsFalse(floor.HasTile(10, 4));
            Assert.IsTrue(floor.HasTile(-11, -1));
            Assert.IsFalse(floor.HasTile(11, 0));
            Assert.Greater(floor.Tiles.Count, 0);
            foreach (var t in floor.Tiles) Assert.IsTrue(floor.HasTile(t.x, t.z));
        }

        [Test]
        public void 가운데는_바닥()
        {
            Assert.IsTrue(new KnockbackFloor(17f, 1.5f, 0f).IsSolid(0.1f, 0.1f, 0));
        }

        [Test]
        public void 원판_밖은_허공()
        {
            var floor = new KnockbackFloor(17f, 1.5f, 0f);
            Assert.IsFalse(floor.IsSolid(18f, 0f, 0));
            Assert.IsTrue(floor.IsSolid(0f, 16.4f, 0));
        }

        [Test]
        public void 음수_좌표도_바닥_칸을_정확히_찾는다()
        {
            Assert.AreEqual((-1, 0), KnockbackFloor.TileAt(-0.3f, 0.2f, 1.5f));
            Assert.AreEqual((-1, -1), KnockbackFloor.TileAt(-1.5f, -1.5f, 1.5f));
        }

        [Test]
        public void 가운데_구멍()
        {
            var floor = new KnockbackFloor(17f, 1.5f, 2.6f);
            Assert.IsFalse(floor.IsSolid(0.1f, 0.1f, 0));
            Assert.IsTrue(floor.IsSolid(3.5f, 0.5f, 0));
        }

        [Test]
        public void 칸_순서는_고정()
        {
            var a = new KnockbackFloor(17f, 1.5f, 0f).Tiles;
            var b = new KnockbackFloor(17f, 1.5f, 0f).Tiles;
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++) Assert.AreEqual(a[i], b[i]);
        }

        static ZoneStage Stage(Vector2 toCenter, float toRadius, KnockbackFloor floor)
            => new ZoneStage(0, 0, 100, 200, Vector2.zero, floor.ZoneStartRadius, toCenter, toRadius, 4f);

        [Test]
        public void 단계가_없으면_슬라이스1_원판_그대로()
        {
            var floor = new KnockbackFloor(17f, 1.5f, 0f);
            foreach (var t in floor.Tiles)
            {
                Assert.IsTrue(floor.IsTileSolid(t.x, t.z, 0));
                Assert.IsTrue(floor.IsTileSolid(t.x, t.z, 10000));
            }
        }

        [Test]
        public void 축소_중에_가장자리부터_무너진다()
        {
            var floor = new KnockbackFloor(17f, 1.5f, 0f);
            floor.SetZoneStages(new[] { Stage(Vector2.zero, 11f, floor) });
            // 틱 150의 반지름 = (18.0607+11)/2 = 14.53.
            // 칸 (8,3): 가장 먼 꼭짓점 (13.5, 6) 거리 14.77 > 14.53 → 무너짐
            // 칸 (8,2): 가장 먼 꼭짓점 (13.5, 4.5) 거리 14.23 <= 14.53 → 살아 있음
            Assert.IsFalse(floor.IsTileSolid(8, 3, 150));
            Assert.IsTrue(floor.IsTileSolid(8, 2, 150));
        }

        [Test]
        public void 중심이_옮겨_가면_반대편이_먼저()
        {
            var floor = new KnockbackFloor(17f, 1.5f, 0f);
            floor.SetZoneStages(new[] { Stage(new Vector2(3f, 0f), 11f, floor) });
            // 틱 150: 중심 (1.5, 0), 반지름 14.53.
            // 칸 (8,0)은 가장 먼 꼭짓점이 약 12.1 → 살아 있음, 칸 (-9,0)은 약 15.1 → 무너짐
            Assert.IsTrue(floor.IsTileSolid(8, 0, 150));
            Assert.IsFalse(floor.IsTileSolid(-9, 0, 150));
        }

        [Test]
        public void 단계를_받으면_같은_칸이_무너진다()
        {
            var a = new KnockbackFloor(17f, 1.5f, 0f);
            var b = new KnockbackFloor(17f, 1.5f, 0f);
            var list = new List<ZoneStage> { Stage(new Vector2(3f, 0f), 11f, a) };
            a.SetZoneStages(list);
            b.SetZoneStages(list);
            bool anyDead = false;
            for (long tick = 100; tick <= 300; tick++)
            {
                foreach (var t in a.Tiles)
                {
                    bool sa = a.IsTileSolid(t.x, t.z, tick);
                    Assert.AreEqual(sa, b.IsTileSolid(t.x, t.z, tick));
                    if (!sa) anyDead = true;
                }
            }
            Assert.IsTrue(anyDead);
        }

        [Test]
        public void 받은_목록을_나중에_바꿔도_바닥은_그대로()
        {
            var floor = new KnockbackFloor(17f, 1.5f, 0f);
            var list = new List<ZoneStage> { Stage(Vector2.zero, 11f, floor) };
            floor.SetZoneStages(list);
            list.Clear();
            Assert.IsFalse(floor.IsTileSolid(8, 3, 150));
        }

        [Test]
        public void 마지막엔_다_무너진다()
        {
            var floor = new KnockbackFloor(17f, 1.5f, 0f);
            floor.SetZoneStages(new[] { Stage(Vector2.zero, 0f, floor) });
            foreach (var t in floor.Tiles) Assert.IsFalse(floor.IsTileSolid(t.x, t.z, 200));
            Assert.IsTrue(floor.IsTileSolid(0, 0, 100));
        }

        [Test]
        public void 깜박임은_1초_전부터()
        {
            var floor = new KnockbackFloor(17f, 1.5f, 0f);
            floor.SetZoneStages(new[] { Stage(Vector2.zero, 11f, floor) });
            long death = 0;
            for (long t = 100; t <= 200; t++) { if (!floor.IsTileSolid(8, 3, t)) { death = t; break; } }
            Assert.Greater(death, 100);
            Assert.IsTrue(floor.IsWarning(8, 3, death - 1, 50));
            Assert.IsTrue(floor.IsWarning(8, 3, death - 50, 50));
            Assert.IsFalse(floor.IsWarning(8, 3, death - 51, 50));
            Assert.IsFalse(floor.IsWarning(8, 3, death, 50));
        }
    }
}
