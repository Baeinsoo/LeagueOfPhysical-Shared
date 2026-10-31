using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class KnockbackHitsTests
    {
        const float Dt = 0.02f;
        static readonly System.Collections.Generic.List<Vector2> NoPillars = new System.Collections.Generic.List<Vector2>();

        static readonly KnockbackConfig Config = new KnockbackConfig(120f, 6.2f, 14f, 17f, 1.5f, 0f, 5f, 11f,
            30f, 18f, 0.3f, 0.55f, 3, 0.95f, 0.26f, 1.2f, 1, 6f, 0.5f, 0.5f, 0.3f, 0.1f, 0.2f, 0.8f, 5f,
            new List<MountedArcheryZoneStageConfig>());

        //  30 m/s x 0.02 = 틱당 0.6 m. 발사 틱 0, 틱 3이면 1.2 에서 1.8 구간을 지난다.
        static KnockbackShot Shot(string shooter, long fireTick = 0)
            => new KnockbackShot(shooter, fireTick, Vector2.zero, new Vector2(1f, 0f));

        [Test]
        public void 탄_하나는_몸_하나만()
        {
            var targets = new List<(string, Vector2)> { ("far", new Vector2(2.2f, 0f)), ("near", new Vector2(1.7f, 0f)) };
            //  틱 3 선분 1.2~1.8 이 둘 다 반지름(0.55+0.3=0.85) 안에 든다 — 가까운 쪽만 나와야 한다.
            var hits = KnockbackHits.Find(new[] { Shot("s") }, targets, NoPillars, 0f, 3, Config, Dt);
            Assert.AreEqual(1, hits.Count);
            Assert.AreEqual("near", hits[0].victimId);
        }

        [Test]
        public void 자기_탄은_자기를_안_맞힌다()
        {
            var self = new List<(string, Vector2)> { ("s", new Vector2(1.5f, 0f)) };
            Assert.AreEqual(0, KnockbackHits.Find(new[] { Shot("s") }, self, NoPillars, 0f, 3, Config, Dt).Count);

            //  양성 대조 — 남이 같은 자리에 서 있으면 맞는다.
            var other = new List<(string, Vector2)> { ("o", new Vector2(1.5f, 0f)) };
            Assert.AreEqual(1, KnockbackHits.Find(new[] { Shot("s") }, other, NoPillars, 0f, 3, Config, Dt).Count);
        }

        [Test]
        public void 같은_틱_두_탄은_둘_다_맞는다()
        {
            var shots = new[] { Shot("a"), Shot("b") };
            var targets = new List<(string, Vector2)> { ("v", new Vector2(1.5f, 0f)) };
            var hits = KnockbackHits.Find(shots, targets, NoPillars, 0f, 3, Config, Dt);
            Assert.AreEqual(2, hits.Count);
            Assert.AreEqual("a", hits[0].shot.ShooterId);
            Assert.AreEqual("b", hits[1].shot.ShooterId);
        }

        [Test]
        public void 쏜_틱에는_원점에서_시작한다()
        {
            //  원점 바로 앞에 선 몸 — 앞 틱 위치(원점 뒤)를 쓰면 선분이 달라진다.
            var targets = new List<(string, Vector2)> { ("v", new Vector2(0.5f, 0f)) };
            Assert.AreEqual(1, KnockbackHits.Find(new[] { Shot("s", 5) }, targets, NoPillars, 0f, 5, Config, Dt).Count);
        }

        static readonly List<Vector2> PillarAt3 = new List<Vector2> { new Vector2(3f, 0f) };

        [Test]
        public void 기둥_뒤에_선_몸은_맞지_않는다()
        {
            //  틱 6 선분 3.0~3.6: 몸 (4,0)의 원(3.15~)과 기둥 (3,0)의 원(1.8~4.2)이 둘 다 지나가는 틱.
            var targets = new List<(string, Vector2)> { ("v", new Vector2(4f, 0f)) };
            Assert.AreEqual(0, KnockbackHits.Find(new[] { Shot("s") }, targets, PillarAt3, 0.9f, 6, Config, Dt).Count);
            //  기둥이 없으면 같은 자리에서 맞는다 - 기둥 때문에 안 맞은 것임을 보이는 대조.
            Assert.AreEqual(1, KnockbackHits.Find(new[] { Shot("s") }, targets, NoPillars, 0f, 6, Config, Dt).Count);
        }

        [Test]
        public void 기둥_앞에_선_몸은_맞는다()
        {
            //  틱 2 선분 0.6~1.2: 몸 (2,0)의 원(1.15~)에 먼저 닿고 기둥(1.8~)에는 아직 안 닿는다.
            var targets = new List<(string, Vector2)> { ("v", new Vector2(2f, 0f)) };
            Assert.AreEqual(1, KnockbackHits.Find(new[] { Shot("s") }, targets, PillarAt3, 0.9f, 2, Config, Dt).Count);
        }

        [Test]
        public void 쓰러진_기둥은_탄을_막지_않는다()
        {
            var targets = new List<(string, Vector2)> { ("v", new Vector2(4f, 0f)) };
            Assert.AreEqual(1, KnockbackHits.Find(new[] { Shot("s") }, targets, new List<Vector2>(), 0.9f, 6, Config, Dt).Count);
        }
    }
}
