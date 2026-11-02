using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class KnockbackBlastTests
    {
        static List<(string id, Vector2 position, int team)> Bodies(params (string, float, float, int)[] rows)
        {
            var list = new List<(string, Vector2, int)>();
            foreach (var r in rows) { list.Add((r.Item1, new Vector2(r.Item2, r.Item3), r.Item4)); }
            return list;
        }

        [Test]
        public void 반경_안_상대만_밀린다()
        {
            var bodies = Bodies(("a", 0f, 0f, 0), ("b", 2f, 0f, 0), ("c", 5f, 0f, 0));
            var pushes = KnockbackBlast.Resolve(Vector2.zero, Vector2.right, 2.5f, bodies, "shooter", 0);
            Assert.AreEqual(2, pushes.Count);
            Assert.AreEqual("a", pushes[0].id);
            Assert.AreEqual("b", pushes[1].id);
        }

        [Test]
        public void 쏜_사람_자신은_빠진다()
        {
            var bodies = Bodies(("shooter", 1f, 0f, 0), ("b", 1f, 1f, 0));
            var pushes = KnockbackBlast.Resolve(Vector2.zero, Vector2.right, 2.5f, bodies, "shooter", 0);
            Assert.AreEqual(1, pushes.Count);
            Assert.AreEqual("b", pushes[0].id);
        }

        [Test]
        public void 같은_팀은_빠지고_팀_없음은_서로_상대다()
        {
            var team = Bodies(("mate", 1f, 0f, 1), ("foe", 0f, 1f, 2));
            var pushes = KnockbackBlast.Resolve(Vector2.zero, Vector2.right, 2.5f, team, "shooter", 1);
            Assert.AreEqual(1, pushes.Count);
            Assert.AreEqual("foe", pushes[0].id);

            var solo = Bodies(("x", 1f, 0f, 0));
            Assert.AreEqual(1, KnockbackBlast.Resolve(Vector2.zero, Vector2.right, 2.5f, solo, "shooter", 0).Count);
        }

        [Test]
        public void 방향은_터진_자리에서_몸_쪽으로_정규화된다()
        {
            var bodies = Bodies(("b", 0f, -2f, 0));
            var pushes = KnockbackBlast.Resolve(new Vector2(0f, 0f), Vector2.right, 2.5f, bodies, "s", 0);
            Assert.AreEqual(0f, pushes[0].direction.x, 1e-5f);
            Assert.AreEqual(-1f, pushes[0].direction.y, 1e-5f);
        }

        [Test]
        public void 거리가_0이면_탄_방향으로_민다()
        {
            var bodies = Bodies(("b", 3f, 4f, 0));
            var pushes = KnockbackBlast.Resolve(new Vector2(3f, 4f), new Vector2(0f, 1f), 2.5f, bodies, "s", 0);
            Assert.AreEqual(1, pushes.Count);
            Assert.AreEqual(0f, pushes[0].direction.x, 1e-5f);
            Assert.AreEqual(1f, pushes[0].direction.y, 1e-5f);
        }

        [Test]
        public void 반경_딱은_들고_조금_밖은_뺀다()
        {
            var bodies = Bodies(("edge", 2.5f, 0f, 0), ("out", 2.5001f, 0f, 0));
            var pushes = KnockbackBlast.Resolve(Vector2.zero, Vector2.right, 2.5f, bodies, "s", 0);
            Assert.AreEqual(1, pushes.Count);
            Assert.AreEqual("edge", pushes[0].id);
        }

        [Test]
        public void 결과는_id_순이다()
        {
            var bodies = Bodies(("c", 1f, 0f, 0), ("a", 0f, 1f, 0), ("b", -1f, 0f, 0));
            var pushes = KnockbackBlast.Resolve(Vector2.zero, Vector2.right, 2.5f, bodies, "s", 0);
            Assert.AreEqual("a", pushes[0].id);
            Assert.AreEqual("b", pushes[1].id);
            Assert.AreEqual("c", pushes[2].id);
        }

        [Test]
        public void 설정의_상자_시간은_틱으로_바뀐다()
        {
            var c = new KnockbackConfig(120f, 6.2f, 14f, 17f, 1.5f, 0f, 5f, 11f, 30f, 18f, 0.3f, 0.55f, 3, 0.95f, 0.26f, 1.2f, 1, 6f, 0.5f, 0.5f, 0.3f, 0.1f, 0.2f, 0.8f, 5f,
                new List<MountedArcheryZoneStageConfig>(),
                crateFirstSeconds: 20f, crateIntervalSeconds: 20f, crateWarnSeconds: 1.5f, cratePickupRadius: 0.9f, blastRadius: 2.5f);
            Assert.AreEqual(1000, c.CrateFirstTicks);
            Assert.AreEqual(1000, c.CrateIntervalTicks);
            Assert.AreEqual(75, c.CrateWarnTicks);
            Assert.AreEqual(0.9f, c.CratePickupRadius);
            Assert.AreEqual(2.5f, c.BlastRadius);
        }
    }
}
