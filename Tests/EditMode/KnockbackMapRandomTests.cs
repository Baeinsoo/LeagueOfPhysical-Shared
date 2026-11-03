using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LOP.Tests
{
    /// <summary>맵 안의 무작위(기둥 고리 회전)는 씨앗이 같으면 클·서가 같은 값을 낸다.</summary>
    public class KnockbackMapRandomTests
    {
        class FakeSeed : IMatchSeed
        {
            public ulong Value { get; set; }
        }

        static KnockbackFloor Floor() => new KnockbackFloor(23f, 1.5f, 0f);
        static readonly KnockbackPillarLayout Ring4 = new KnockbackPillarLayout(4, 9.5f, 0.9f, 3f);

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
    }
}
