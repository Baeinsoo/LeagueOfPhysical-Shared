using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class ArcheryTargetMotionTests
    {
        const float TickInterval = 0.02f;

        static ArcheryTarget Target(long spawnTick, float riseSpeed)
        {
            return new ArcheryTarget(
                waveIndex: 0, slotIndex: 0,
                origin: new Vector3(1f, 0f, 2f), riseSpeed: riseSpeed, spawnTick: spawnTick,
                radius: 0.3f, points: 2, isTrap: false,
                shape: ArcheryTargetShape.Sphere, bands: null, facing: Vector3.zero,
                lifetimeSeconds: ArcheryTargetMotion.LifetimeFor(riseSpeed), ownerUserId: string.Empty);
        }

        [Test]
        public void 솟기_전에는_출발점에_있다()
        {
            var target = Target(spawnTick: 100, riseSpeed: 10f);

            var before = ArcheryTargetMotion.PositionAt(target, 90, TickInterval);

            Assert.AreEqual(target.Origin, before);
        }

        [Test]
        public void 솟는_순간에도_출발점이다()
        {
            var target = Target(spawnTick: 100, riseSpeed: 10f);

            Assert.AreEqual(target.Origin, ArcheryTargetMotion.PositionAt(target, 100, TickInterval));
        }

        //  좌우로는 안 움직인다 — 솟았다 떨어지는 것뿐이라 x·z는 출발점 그대로다.
        [Test]
        public void 좌우로는_움직이지_않는다()
        {
            var target = Target(spawnTick: 100, riseSpeed: 10f);

            var mid = ArcheryTargetMotion.PositionAt(target, 130, TickInterval);

            Assert.AreEqual(target.Origin.x, mid.x, 1e-4f);
            Assert.AreEqual(target.Origin.z, mid.z, 1e-4f);
            Assert.Greater(mid.y, target.Origin.y, "솟는 중이면 출발점보다 높아야 한다");
        }

        //  정점 높이가 설계값(솟는 높이)과 맞아야 한다 — 이게 틀리면 과녁이 뜨는 공간을 벗어난다.
        [Test]
        public void 정점에서_정해진_높이만큼_솟는다()
        {
            float riseHeight = 2f;
            float riseSpeed = ArcheryTargetMotion.RiseSpeedFor(riseHeight);
            var target = Target(spawnTick: 100, riseSpeed: riseSpeed);

            //  정점은 수명의 절반 시점이다(올라간 만큼 내려온다).
            double apexTick = 100 + (target.LifetimeSeconds / 2f) / TickInterval;
            var apex = ArcheryTargetMotion.PositionAt(target, apexTick, TickInterval);

            Assert.AreEqual(target.Origin.y + riseHeight, apex.y, 0.01f);
        }

        //  수명이 끝나는 순간 출발 높이로 돌아온다 — 올라간 만큼 내려온다는 뜻이다.
        [Test]
        public void 수명이_끝나면_출발_높이로_돌아온다()
        {
            float riseSpeed = ArcheryTargetMotion.RiseSpeedFor(2f);
            var target = Target(spawnTick: 100, riseSpeed: riseSpeed);

            double endTick = 100 + target.LifetimeSeconds / TickInterval;
            var end = ArcheryTargetMotion.PositionAt(target, endTick, TickInterval);

            Assert.AreEqual(target.Origin.y, end.y, 0.01f);
        }

        //  화면은 틱 사이도 물어본다 — 소수 틱이 정수 틱 둘 사이에 있어야 한다.
        [Test]
        public void 소수_틱도_받는다()
        {
            var target = Target(spawnTick: 100, riseSpeed: 10f);

            float at110 = ArcheryTargetMotion.PositionAt(target, 110, TickInterval).y;
            float at110half = ArcheryTargetMotion.PositionAt(target, 110.5, TickInterval).y;
            float at111 = ArcheryTargetMotion.PositionAt(target, 111, TickInterval).y;

            Assert.Greater(at110half, at110);
            Assert.Less(at110half, at111);
        }

        [Test]
        public void 수명_안에서만_살아_있다()
        {
            var target = Target(spawnTick: 100, riseSpeed: ArcheryTargetMotion.RiseSpeedFor(2f));

            Assert.IsFalse(ArcheryTargetMotion.IsAlive(target, 99, TickInterval), "솟기 전");
            Assert.IsTrue(ArcheryTargetMotion.IsAlive(target, 100, TickInterval), "솟는 순간");
            Assert.IsTrue(ArcheryTargetMotion.IsAlive(target, 120, TickInterval), "공중");

            double endTick = 100 + target.LifetimeSeconds / TickInterval;
            Assert.IsFalse(ArcheryTargetMotion.IsAlive(target, endTick + 1, TickInterval), "떨어진 뒤");
        }

        //  중력이 고정이라 높이 하나가 속도도 수명도 정한다 — 손으로 적어 넣는 값이 아니다.
        [Test]
        public void 솟는_속도는_높이에서_나온다()
        {
            float riseSpeed = ArcheryTargetMotion.RiseSpeedFor(riseHeight: 2f);

            //  v0 = sqrt(2gH) = sqrt(2 x 20 x 2) = sqrt(80) — 손으로 센 숫자다(구현식을 그대로
            //  베끼면 식이 틀려도 이 값이 같이 틀려 못 잡는다).
            Assert.AreEqual(8.944272f, riseSpeed, 1e-3f);
        }

        //  화살과 과녁이 같은 화면에 있다 — 중력이 다르면 같은 시간에 다르게 떨어져 눈에 띈다.
        [Test]
        public void 과녁_중력은_화살_중력과_같다()
        {
            Assert.AreEqual(ArcheryTrajectory.Gravity, ArcheryTargetMotion.Gravity);
        }

        //  높이가 다르면 수명도 다르다 — 그래서 "언제쯤 정점"이 과녁마다 달라진다.
        [Test]
        public void 높이가_다르면_수명도_다르다()
        {
            var low = Target(spawnTick: 100, riseSpeed: ArcheryTargetMotion.RiseSpeedFor(1.2f));
            var high = Target(spawnTick: 100, riseSpeed: ArcheryTargetMotion.RiseSpeedFor(2.4f));

            Assert.Less(low.LifetimeSeconds, high.LifetimeSeconds);
        }
    }
}
