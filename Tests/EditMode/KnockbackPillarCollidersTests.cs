using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class KnockbackPillarCollidersTests
    {
        KnockbackPillarColliders colliders;

        [TearDown]
        public void TearDown()
        {
            colliders?.Dispose();
            colliders = null;
        }

        [Test]
        public void 기둥_콜라이더는_틱만_보고_켜고_끈다()
        {
            var floor = new KnockbackFloor(17f, 1.5f, 0f);
            floor.SetZoneStages(new[] { new ZoneStage(0, 0, 100, 200, Vector2.zero, floor.ZoneStartRadius, Vector2.zero, 3f, 4f) });
            var pillars = new KnockbackPillars(new KnockbackPillarLayout(4, 7f, 0.9f, 3f), floor);
            colliders = new KnockbackPillarColliders(pillars);

            long collapse = -1;
            for (long t = 100; t <= 200; t++)
            {
                if (!pillars.IsStanding(0, t)) { collapse = t; break; }
            }
            Assert.Greater(collapse, 100);

            colliders.Apply(collapse);
            Assert.IsFalse(colliders.IsEnabled(0));
            colliders.Apply(collapse - 1);   // 되감기
            Assert.IsTrue(colliders.IsEnabled(0));
        }

        [Test]
        public void 기둥_몸은_Default_레이어에_바닥부터_서_있다()
        {
            var floor = new KnockbackFloor(17f, 1.5f, 0f);
            var pillars = new KnockbackPillars(new KnockbackPillarLayout(4, 7f, 0.9f, 3f), floor);
            colliders = new KnockbackPillarColliders(pillars);

            var go = GameObject.Find("KnockbackPillar_0");
            Assert.IsNotNull(go);
            Assert.AreEqual(0, go.layer);
            var capsule = go.GetComponent<CapsuleCollider>();
            Assert.AreEqual(0.9f, capsule.radius);
            Assert.AreEqual(3f, capsule.height);
            Assert.AreEqual(0f, capsule.bounds.min.y, 1e-3f);

            colliders.Dispose();
            colliders = null;
            Assert.IsNull(GameObject.Find("KnockbackPillar_0"));
        }
    }
}
