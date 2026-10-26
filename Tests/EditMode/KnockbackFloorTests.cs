using NUnit.Framework;

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
    }
}
