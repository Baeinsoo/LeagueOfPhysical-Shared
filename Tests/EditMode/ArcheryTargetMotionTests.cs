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
                lifetimeSeconds: ArcheryTargetMotion.LifetimeFor(riseSpeed), ownerUserId: string.Empty,
                lateralSpan: 0f, lateralPeriod: 0f);
        }

        //  사거리 과녁 모양 — 안 솟고(riseSpeed 0) 좌우로만 흔든다. facing은 +z(사수 쪽을 본다고
        //  치면 사수는 −z에 있다) — 그 수평 수직선은 x축이라, 흔들림이 x로 나타나서 검증하기 쉽다.
        static ArcheryTarget RangeTarget(long spawnTick, float lateralSpan, float lateralPeriod)
        {
            return new ArcheryTarget(
                waveIndex: 0, slotIndex: 0,
                origin: new Vector3(1f, 0f, 2f), riseSpeed: 0f, spawnTick: spawnTick,
                radius: 0.61f, points: 5, isTrap: false,
                shape: ArcheryTargetShape.Face, bands: null, facing: Vector3.forward,
                lifetimeSeconds: 10f, ownerUserId: string.Empty,
                lateralSpan: lateralSpan, lateralPeriod: lateralPeriod);
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

        //  이 시험은 흔드는 폭이 0인 과녁(웨이브/원형 맵)을 잰다 — 솟았다 떨어지는 것뿐이라
        //  x·z는 출발점 그대로다. 흔드는 폭이 있는 과녁(사거리 맵)은 아래 별도 시험이 잰다.
        [Test]
        public void 좌우로는_움직이지_않는다()
        {
            var target = Target(spawnTick: 100, riseSpeed: 10f);

            var mid = ArcheryTargetMotion.PositionAt(target, 130, TickInterval);

            Assert.AreEqual(target.Origin.x, mid.x, 1e-4f);
            Assert.AreEqual(target.Origin.z, mid.z, 1e-4f);
            Assert.Greater(mid.y, target.Origin.y, "솟는 중이면 출발점보다 높아야 한다");
        }

        // ---- 사거리 과녁의 좌우 흔들림 ----

        [Test]
        public void 사거리_과녁도_솟는_순간에는_출발점이다()
        {
            var target = RangeTarget(spawnTick: 100, lateralSpan: 3f, lateralPeriod: 3f);

            Assert.AreEqual(target.Origin, ArcheryTargetMotion.PositionAt(target, 100, TickInterval));
        }

        //  삼각파는 한 주기(period)마다 정확히 제자리(출발점)로 돌아온다 — t=0, T, 2T, ...
        [Test]
        public void 한_주기가_지날_때마다_출발점으로_돌아온다()
        {
            var target = RangeTarget(spawnTick: 0, lateralSpan: 3f, lateralPeriod: 3f);
            long ticksPerPeriod = (long)System.Math.Round(3f / TickInterval);   // 150틱

            for (int period = 1; period <= 4; period++)
            {
                var at = ArcheryTargetMotion.PositionAt(target, ticksPerPeriod * period, TickInterval);
                Assert.AreEqual(target.Origin.x, at.x, 1e-3f, $"{period}주기 뒤에는 출발점으로 돌아와야 한다");
            }
        }

        //  ⚠️ 이 시험이 이 슬라이스의 존재 이유를 지킨다. 옆의 두 시험(주기마다 제자리 /
        //  폭 안에 머문다)은 과녁이 **아예 안 움직여도** 통과한다 — 0은 늘 제자리이고 늘 폭 안이다.
        //  그래서 흔들림이 통째로 꺼지는 회귀를 아무도 못 잡는다. 여기서는 사분주기(가장 많이
        //  간 지점)의 자리를 정확한 값과 대조한다 — 안 움직이면 실패하고, 진폭을 폭의 절반이
        //  아니라 폭 전체로 잘못 곱해도 실패한다.
        [Test]
        public void 사분주기에는_폭의_절반만큼_옆으로_가_있다()
        {
            const float span = 3f;
            const float period = 3f;
            var target = RangeTarget(spawnTick: 0, lateralSpan: span, lateralPeriod: period);

            //  facing이 +z라 옆으로 가는 축은 +x다(Cross(up, forward) = right).
            double quarter = (period * 0.25f) / TickInterval;   // 37.5틱 — 소수 틱도 물을 수 있다
            var atPeak = ArcheryTargetMotion.PositionAt(target, quarter, TickInterval);
            Assert.AreEqual(target.Origin.x + span / 2f, atPeak.x, 1e-3f,
                "사분주기에는 한쪽 끝(폭의 절반)에 가 있어야 한다 — 값이 출발점 그대로면 아예 안 흔들린 것이다");

            var atOpposite = ArcheryTargetMotion.PositionAt(target, quarter * 3d, TickInterval);
            Assert.AreEqual(target.Origin.x - span / 2f, atOpposite.x, 1e-3f,
                "사분주기 셋이면 반대쪽 끝에 가 있어야 한다");

            //  흔들림은 레인 폭 방향뿐이다 — 사수 쪽으로 다가오거나 멀어지면 거리가 바뀐다.
            Assert.AreEqual(target.Origin.z, atPeak.z, 1e-4f, "앞뒤로 움직이면 안 된다");
            Assert.AreEqual(target.Origin.y, atPeak.y, 1e-4f, "서 있는 과녁은 위아래로 안 움직인다");
        }
        //  주기 안 어디를 찍어도 흔든 폭(끝에서 끝까지)을 벗어나면 안 된다.
        [Test]
        public void 흔드는_폭을_벗어나지_않는다()
        {
            const float span = 2.5f;
            var target = RangeTarget(spawnTick: 0, lateralSpan: span, lateralPeriod: 3f);
            float half = span / 2f;

            for (long tick = 0; tick <= 900; tick++)
            {
                float x = ArcheryTargetMotion.PositionAt(target, tick, TickInterval).x;
                float offset = x - target.Origin.x;
                Assert.LessOrEqual(offset, half + 1e-4f, $"틱 {tick}: 폭 밖으로 나갔다({offset} > {half})");
                Assert.GreaterOrEqual(offset, -half - 1e-4f, $"틱 {tick}: 폭 밖으로 나갔다({offset} < {-half})");
            }
        }

        //  웨이브(원형 맵) 과녁은 흔드는 폭이 늘 0이다 — 값을 아무리 다른 시각에 물어도 제자리다.
        [Test]
        public void 웨이브_과녁은_좌우로_전혀_안_움직인다()
        {
            var target = Target(spawnTick: 0, riseSpeed: 8f);   // 폭 0(웨이브 기본값)

            for (long tick = 0; tick <= 200; tick += 10)
            {
                var at = ArcheryTargetMotion.PositionAt(target, tick, TickInterval);
                Assert.AreEqual(target.Origin.x, at.x, 1e-5f, $"틱 {tick}에서 좌우로 움직였다");
                Assert.AreEqual(target.Origin.z, at.z, 1e-5f, $"틱 {tick}에서 좌우로 움직였다");
            }
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

            //  v0 = sqrt(2gH) = sqrt(2 x 9.81 x 2) = sqrt(39.24) — 손으로 센 숫자다(구현식을
            //  그대로 베끼면 식이 틀려도 이 값이 같이 틀려 못 잡는다).
            Assert.AreEqual(6.264184f, riseSpeed, 1e-3f);
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

        //  투석기 표적: 2.6초 수명, 가로 4 m/s, 호 1 m. 0.02초 틱.
        private static ArcheryTarget Thrown(float arcHeight = 1f)
        {
            return new ArcheryTarget(0, 0, new Vector3(-5f, 6f, 40f), 0f, 100,
                                     0.6f, 3, false, ArcheryTargetShape.Sphere, null, Vector3.back,
                                     2.6f, string.Empty, 0f, 0f, isShared: false,
                                     drift: new Vector3(4f, 0f, 0f), arcHeight: arcHeight, stunTicks: 0, kindId: 10);
        }

        [Test]
        public void 던진_표적은_가로_속도만큼_옆으로_간다()
        {
            var target = Thrown(arcHeight: 0f);
            //  100틱 출발, 50틱 뒤 = 1초 → x가 4 m 갔다. 높이·깊이는 그대로.
            Vector3 p = ArcheryTargetMotion.PositionAt(target, 150, 0.02f);
            Assert.AreEqual(-1f, p.x, 1e-4f);
            Assert.AreEqual(6f, p.y, 1e-4f);
            Assert.AreEqual(40f, p.z, 1e-4f);
        }

        [Test]
        public void 던진_표적은_수명_가운데에서_호의_높이만큼_솟고_끝에서_제_높이로_온다()
        {
            var target = Thrown(arcHeight: 1f);
            double mid = 100 + 1.3 / 0.02;   // 65틱 뒤 = 수명의 절반
            double end = 100 + 2.6 / 0.02;
            Assert.AreEqual(7f, ArcheryTargetMotion.PositionAt(target, mid, 0.02f).y, 1e-3f);
            Assert.AreEqual(6f, ArcheryTargetMotion.PositionAt(target, end, 0.02f).y, 1e-3f);
        }

        [Test]
        public void 던지기_전에는_출발점에_가만히_있다()
        {
            var target = Thrown();
            Assert.AreEqual(target.Origin, ArcheryTargetMotion.PositionAt(target, 99, 0.02f));
        }

        [Test]
        public void 가로_속도가_없는_옛_과녁은_예전과_같은_자리다()
        {
            //  웨이브 과녁(솟기만) — 새 칸이 기본값이면 식이 안 바뀌어야 한다.
            var old = new ArcheryTarget(0, 0, Vector3.zero, 5f, 0, 0.3f, 1, false,
                                        ArcheryTargetShape.Sphere, null, Vector3.zero,
                                        ArcheryTargetMotion.LifetimeFor(5f), string.Empty, 0f, 0f);
            float t = 0.4f;
            float expectedY = 5f * t - 0.5f * ArcheryTargetMotion.Gravity * t * t;
            Assert.AreEqual(expectedY, ArcheryTargetMotion.PositionAt(old, t / 0.02f, 0.02f).y, 1e-4f);
        }

        [Test]
        public void 종류는_거리대와_가로_속도대와_사격_금지를_들고_있다()
        {
            var kind = new ArcheryTargetKind(0.85f, -3, 1, true, ArcheryTargetShape.Sphere, null,
                                             id: 12, distanceMinM: 32f, distanceMaxM: 50f,
                                             driftMinMps: 3f, driftMaxMps: 5f, stunTicks: 75);
            Assert.AreEqual(12, kind.Id);
            Assert.AreEqual(32f, kind.DistanceMinM);
            Assert.AreEqual(50f, kind.DistanceMaxM);
            Assert.AreEqual(3f, kind.DriftMinMps);
            Assert.AreEqual(5f, kind.DriftMaxMps);
            Assert.AreEqual(75, kind.StunTicks);
        }
    }
}
