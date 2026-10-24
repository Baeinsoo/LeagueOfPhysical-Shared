using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class MountedArcheryZoneTests
    {
        static readonly Vector2 Origin = Vector2.zero;

        // 틱 한 칸 0.02초. 1단계: 30초에 공개, 10초 쉬고, 23초 동안 100 -> 60.
        static readonly ZoneStage First = new ZoneStage(
            index: 1, revealTick: 1500, shrinkStartTick: 2000, shrinkEndTick: 3150,
            fromCenter: Origin, fromRadius: 100f, toCenter: new Vector2(10f, 0f), toRadius: 60f,
            damagePerSecond: 4f);

        // 마지막 단계: 반지름 0까지 줄어든다.
        static readonly ZoneStage Last = new ZoneStage(
            index: 2, revealTick: 6000, shrinkStartTick: 6000, shrinkEndTick: 7050,
            fromCenter: new Vector2(10f, 0f), fromRadius: 60f, toCenter: new Vector2(10f, 0f), toRadius: 0f,
            damagePerSecond: 15f);

        static readonly IReadOnlyList<ZoneStage> Stages = new[] { First, Last };

        [Test]
        public void 공개_전엔_처음_원()
        {
            var (c, r) = MountedArcheryZone.CircleAt(Stages, 1499, Origin, 100f);
            Assert.AreEqual(100f, r);
            Assert.AreEqual(Origin, c);
        }

        [Test]
        public void 공개_뒤_쉬는_동안은_아직_앞_원()
        {
            var (_, r) = MountedArcheryZone.CircleAt(Stages, 1999, Origin, 100f);
            Assert.AreEqual(100f, r);
        }

        [Test]
        public void 축소_중간은_선형()
        {
            Assert.AreEqual(2575, (First.shrinkStartTick + First.shrinkEndTick) / 2);
            var (c, r) = MountedArcheryZone.CircleAt(Stages, 2575, Origin, 100f);
            Assert.AreEqual(80f, r, 1e-3f);
            Assert.AreEqual(5f, c.x, 1e-3f);
        }

        [Test]
        public void 축소가_끝나면_끝값에_고정()
        {
            var (c, r) = MountedArcheryZone.CircleAt(Stages, 5000, Origin, 100f);
            Assert.AreEqual(60f, r, 1e-4f);
            Assert.AreEqual(10f, c.x, 1e-4f);
        }

        [Test]
        public void 마지막은_0()
        {
            var (_, r) = MountedArcheryZone.CircleAt(Stages, 7050, Origin, 100f);
            Assert.AreEqual(0f, r, 1e-4f);
            var (_, after) = MountedArcheryZone.CircleAt(Stages, 9000, Origin, 100f);
            Assert.AreEqual(0f, after, 1e-4f);
        }

        [Test]
        public void 새_원은_지금_원_안에_통째로()
        {
            var center = new Vector2(3f, -4f);
            const float radius = 100f, next = 60f;
            foreach (float u1 in new[] { 0f, 1f })
            foreach (float u2 in new[] { 0f, 1f })
            {
                var p = MountedArcheryZone.PickNextCenter(center, radius, next, u1, u2);
                Assert.LessOrEqual(Vector2.Distance(p, center), radius - next + 1e-3f, $"u1={u1} u2={u2}");
            }
            // 양성 대조: u1=1이면 실제로 가장자리까지 간다(항상 중심만 돌려주는 구현을 잡는다)
            var far = MountedArcheryZone.PickNextCenter(center, radius, next, 1f, 0.25f);
            Assert.AreEqual(radius - next, Vector2.Distance(far, center), 1e-3f);
        }

        [Test]
        public void 원_밖_피해는_공개된_단계_값()
        {
            Assert.AreEqual(0f, MountedArcheryZone.DamagePerSecondAt(Stages, 1499, 15f, 0.02f));
            Assert.AreEqual(4f, MountedArcheryZone.DamagePerSecondAt(Stages, 1500, 15f, 0.02f));
            Assert.AreEqual(4f, MountedArcheryZone.DamagePerSecondAt(Stages, 5999, 15f, 0.02f));
            Assert.AreEqual(15f, MountedArcheryZone.DamagePerSecondAt(Stages, 6000, 15f, 0.02f));
        }

        [Test]
        public void 마지막_원_뒤엔_피해가_커진다()
        {
            Assert.AreEqual(15f, MountedArcheryZone.DamagePerSecondAt(Stages, 7050, 15f, 0.02f), 1e-4f);
            Assert.AreEqual(30f, MountedArcheryZone.DamagePerSecondAt(Stages, 7100, 15f, 0.02f), 1e-4f);
            Assert.AreEqual(45f, MountedArcheryZone.DamagePerSecondAt(Stages, 7150, 15f, 0.02f), 1e-4f);
            // 마지막이 아닌 단계는 시간이 지나도 안 커진다
            Assert.AreEqual(4f, MountedArcheryZone.DamagePerSecondAt(new[] { First }, 9000, 0f, 0.02f), 1e-4f);
        }

        [Test]
        public void 반지름_0이면_한가운데도_밖()
        {
            Assert.IsTrue(MountedArcheryZone.IsOutside(new Vector2(10f, 0f), (new Vector2(10f, 0f), 0f)));
            // 양성 대조: 반지름이 있으면 한가운데는 안
            Assert.IsFalse(MountedArcheryZone.IsOutside(new Vector2(10f, 0f), (new Vector2(10f, 0f), 1f)));
        }

        [Test]
        public void 원_밖_판정()
        {
            Assert.IsTrue(MountedArcheryZone.IsOutside(new Vector2(101f, 0f), (Origin, 100f)));
            Assert.IsFalse(MountedArcheryZone.IsOutside(new Vector2(99f, 0f), (Origin, 100f)));
        }
    }
}
