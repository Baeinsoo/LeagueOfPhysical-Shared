using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LOP.Tests
{
    /// <summary>맵 안의 무작위(기둥 고리 회전, 드럼통 자리)는 씨앗이 같으면 클·서가 같은 값을 낸다.</summary>
    public class KnockbackMapRandomTests
    {
        class FakeSeed : IMatchSeed
        {
            public ulong Value { get; set; }
        }

        static KnockbackFloor Floor() => new KnockbackFloor(23f, 1.5f, 0f);
        static readonly KnockbackPillarLayout Ring4 = new KnockbackPillarLayout(4, 9.5f, 0.9f, 3f);
        static readonly KnockbackBarrelLayout Four = new KnockbackBarrelLayout(4, 0.55f, 9f, 2.5f, 0.6f, 6f, 1f);

        static List<Vector2> Centers(KnockbackPillars p)
        {
            var list = new List<Vector2>();
            for (int i = 0; i < p.Count; i++) list.Add(p.Center(i));
            return list;
        }

        [Test]
        public void 같은_씨앗은_같은_각도_다른_씨앗은_다른_각도다()
        {
            float a = KnockbackMapRandom.PillarRotationDegrees(12345UL, 4);
            Assert.AreEqual(a, KnockbackMapRandom.PillarRotationDegrees(12345UL, 4));
            Assert.AreNotEqual(a, KnockbackMapRandom.PillarRotationDegrees(54321UL, 4));
        }

        [Test]
        public void 회전_각은_기둥_한_칸_사이_0도_이상_90도_미만이다()
        {
            for (ulong s = 1; s <= 300; s++)
            {
                float a = KnockbackMapRandom.PillarRotationDegrees(s, 4);
                Assert.GreaterOrEqual(a, 0f);
                Assert.Less(a, 90f);
            }
            Assert.AreEqual(0f, KnockbackMapRandom.PillarRotationDegrees(7UL, 0));
        }

        [Test]
        public void 씨앗을_안_주면_회전이_없다_옛_배치_그대로()
        {
            var p = new KnockbackPillars(Ring4, Floor());
            Assert.AreEqual(2.679f * 9.5f / 7f, p.Center(0).x, 1e-2f);
        }

        [Test]
        public void 씨앗을_주면_고리가_그_각만큼_돌고_반지름은_그대로다()
        {
            var seed = new FakeSeed { Value = 99UL };
            var rotated = new KnockbackPillars(Ring4, Floor(), seed);
            var plain = new KnockbackPillars(Ring4, Floor());
            float deg = KnockbackMapRandom.PillarRotationDegrees(99UL, 4);
            Assert.Greater(deg, 0f);
            Assert.AreEqual(deg, rotated.RotationDegrees, 1e-4f);
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(9.5f, rotated.Center(i).magnitude, 1e-3f);
                Assert.Greater(Mathf.Abs(plain.Center(i).x - rotated.Center(i).x), 1e-3f);
                // 각은 +z가 0도, 시계 방향으로 는다 — 돌린 각도만큼 더해진다.
                float expected = (22.5f + 90f * i + deg) * Mathf.Deg2Rad;
                Assert.AreEqual(Mathf.Sin(expected) * 9.5f, rotated.Center(i).x, 1e-3f);
                Assert.AreEqual(Mathf.Cos(expected) * 9.5f, rotated.Center(i).y, 1e-3f);
            }
        }

        [Test]
        public void 클라가_씨앗을_늦게_받아도_받는_순간_서버와_같은_배치가_된다()
        {
            var clientSeed = new FakeSeed { Value = 0UL };
            var client = new KnockbackPillars(Ring4, Floor(), clientSeed);
            var server = new KnockbackPillars(Ring4, Floor(), new FakeSeed { Value = 777UL });
            Assert.Greater(Mathf.Abs(server.Center(0).x - client.Center(0).x), 1e-3f);
            int before = client.Version;

            clientSeed.Value = 777UL;
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(server.Center(i).x, client.Center(i).x, 1e-6f);
                Assert.AreEqual(server.Center(i).y, client.Center(i).y, 1e-6f);
            }
            Assert.Greater(client.Version, before);
        }

        [Test]
        public void 기둥_콜라이더도_씨앗이_바뀌면_새_자리로_옮겨진다()
        {
            var seed = new FakeSeed { Value = 0UL };
            var pillars = new KnockbackPillars(Ring4, Floor(), seed);
            using (var colliders = new KnockbackPillarColliders(pillars))
            {
                seed.Value = 4242UL;
                colliders.Apply(0);
                var go = GameObject.Find("KnockbackPillar_0");
                Assert.AreEqual(pillars.Center(0).x, go.transform.position.x, 1e-4f);
                Assert.AreEqual(pillars.Center(0).y, go.transform.position.z, 1e-4f);
            }
        }

        [Test]
        public void 드럼통_자리는_같은_씨앗이면_같고_다른_씨앗이면_다르다()
        {
            var pillars = Centers(new KnockbackPillars(Ring4, Floor(), new FakeSeed { Value = 5UL }));
            var a = KnockbackMapRandom.BarrelSpawns(5UL, Four, pillars, 0.9f);
            var b = KnockbackMapRandom.BarrelSpawns(5UL, Four, pillars, 0.9f);
            var c = KnockbackMapRandom.BarrelSpawns(6UL, Four, pillars, 0.9f);
            Assert.AreEqual(4, a.Length);
            bool differs = false;
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(a[i], b[i]);
                if ((a[i] - c[i]).sqrMagnitude > 1e-4f) differs = true;
            }
            Assert.IsTrue(differs);
        }

        [Test]
        public void 드럼통은_고리_위에_있고_기둥과_안_겹치며_서로_흩어져_있다()
        {
            float zone = 0.9f + 0.55f + 0.3f;
            for (ulong s = 1; s <= 200; s++)
            {
                var pillars = Centers(new KnockbackPillars(Ring4, Floor(), new FakeSeed { Value = s }));
                var spawns = KnockbackMapRandom.BarrelSpawns(s, Four, pillars, 0.9f);
                for (int i = 0; i < spawns.Length; i++)
                {
                    Assert.AreEqual(9f, spawns[i].magnitude, 1e-3f, $"seed {s} #{i}");
                    foreach (var p in pillars)
                    {
                        Assert.GreaterOrEqual((spawns[i] - p).magnitude, zone - 1e-3f, $"seed {s} #{i}");
                    }
                    for (int j = i + 1; j < spawns.Length; j++)
                    {
                        Assert.Greater((spawns[i] - spawns[j]).magnitude, 2f * 0.55f, $"seed {s} {i}-{j}");
                    }
                }
            }
        }

        [Test]
        public void 드럼통_수가_0이면_자리도_없다()
        {
            Assert.AreEqual(0, KnockbackMapRandom.BarrelSpawns(1UL, KnockbackBarrelLayout.None, new List<Vector2>(), 0.9f).Length);
        }

        [Test]
        public void 드럼통_설정은_개수만_0을_허용하고_나머지_0_이하는_기본값과_오류_로그다()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("barrel_radius"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("barrel_friction"));
            var l = new KnockbackBarrelLayout(4, 0f, 9f, -1f, 0.6f, 6f, 1f);
            Assert.AreEqual(0.55f, l.Radius);
            Assert.AreEqual(2.5f, l.Friction);
            Assert.AreEqual(4, l.Count);
            Assert.AreEqual(0, new KnockbackBarrelLayout(-2, 0.55f, 9f, 2.5f, 0.6f, 6f, 1f).Count);
        }

        [Test]
        public void 설정에_드럼통을_안_주면_없고_주면_그대로_읽힌다()
        {
            Assert.AreEqual(0, KnockbackBarrelLayout.None.Count);
            var c = new KnockbackConfig(120f, 6f, 10f, 12f, 2f, 0.5f, 5f, 11f, 30f, 18f, 0.3f, 0.55f, 3, 0.95f, 0.26f, 1.2f, 12, 6f, 0.2f, 0.5f, 0.3f, 0.1f, 0.2f, 0.8f, 8f,
                new List<MountedArcheryZoneStageConfig>(), barrels: Four);
            Assert.AreEqual(4, c.Barrels.Count);
            Assert.AreEqual(9f, c.Barrels.RingRadius);
            Assert.AreEqual(0.6f, c.Barrels.Restitution);
            Assert.AreEqual(6f, c.Barrels.ShotImpulse);
            Assert.AreEqual(1f, c.Barrels.HitScale);
        }
    }
}
