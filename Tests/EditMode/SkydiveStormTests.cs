using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>태풍의 눈 몸개그 장애물(시제품) — 닿으면 튕기거나 기절하거나 떠오르고, 절대 죽이지 않는다.</summary>
    public class SkydiveStormTests
    {
        private const float Dt = 0.02f;

        private static SkydiveConfig Config() => new SkydiveConfig(
            spreadFallSpeed: 60f, diveFallSpeed: 90f, glideFallSpeed: 6f,
            spreadMoveSpeed: 12f, diveMoveSpeed: 9f, glideMoveSpeed: 14f,
            spreadTurnAccel: 22f, diveTurnAccel: 6f, glideTurnAccel: 18f,
            fallApproach: 29f, postureRate: 4f,
            bodyRadius: 0.4f, bodyHeight: 1.8f, groundY: 0f,
            staminaMax: 300f, glideDrain: 20f, groundRecover: 40f, emergencyGlideTime: 1f,
            groundMoveSpeed: 4f, groundAccel: 100f, jumpPower: 10f, poseClearance: 5f,
            fallBrake: 150f,
            glideWindLag: 0.2f, spreadWindLag: 2.06f, diveWindLag: 3.1f,
            landingLethalSpeed: 30f,
            restitution: 0.35f);

        private static Entity Diver(Vector3 feet, Vector3 velocity)
        {
            var e = new Entity("d1");
            e.Add(new GameFramework.World.Transform { Position = new System.Numerics.Vector3(feet.x, feet.y, feet.z) });
            e.Add(new Velocity { Linear = new System.Numerics.Vector3(velocity.x, velocity.y, velocity.z) });
            e.Add(new Posture { Gliding = true });
            e.Add(new FinishState());
            e.Add(new StormState());
            return e;
        }

        [Test]
        public void 구름_쿠션을_위에서_밟으면_튕겨_오른다()
        {
            var field = new StormHazardField();
            field.Cushions.Add(new CloudCushion(new Vector3(0f, 100f, 0f), 5f));
            var d = Diver(new Vector3(1f, 99.5f, 0f), new Vector3(0f, -40f, 0f));

            new StormHazardSystem().Detect(d, 10, Dt, field, Config(), before: new Vector3(1f, 100.3f, 0f));

            Assert.Greater(d.Get<Velocity>().Linear.Y, 20f, "세게 떨어지면 높이 튕긴다");
            Assert.AreEqual(StormHazardKind.CloudCushion, d.Get<StormState>().LastHitKind);
            Assert.IsFalse(d.Get<Posture>().Gliding, "패러세일이 접힌다");
        }

        [Test]
        public void 구름_쿠션_옆을_지나가면_아무_일_없다()
        {
            var field = new StormHazardField();
            field.Cushions.Add(new CloudCushion(new Vector3(0f, 100f, 0f), 5f));
            var d = Diver(new Vector3(8f, 99.5f, 0f), new Vector3(0f, -40f, 0f));

            new StormHazardSystem().Detect(d, 10, Dt, field, Config(), before: new Vector3(8f, 100.3f, 0f));

            Assert.AreEqual(-40f, d.Get<Velocity>().Linear.Y, 1e-4f);
            Assert.AreEqual(-1, d.Get<StormState>().LastHitTick);
        }

        [Test]
        public void 먹구름에_박으면_기절해_조작이_잠긴다()
        {
            var field = new StormHazardField();
            var cloud = new StormCloud(y: 200f, orbit: 0f, startDegrees: 0f, degreesPerTick: 0f, bob: 0f, bobPeriod: 0, radius: 4f);
            field.Clouds.Add(cloud);
            var d = Diver(new Vector3(0f, 202f, 0f) - Vector3.up * 0.9f, new Vector3(0f, -30f, 0f));

            new StormHazardSystem().Detect(d, 100, Dt, field, Config(), before: new Vector3(0f, 205f, 0f));

            var s = d.Get<StormState>();
            Assert.IsTrue(s.ControlLocked(101), "다음 틱부터 기절");
            Assert.IsFalse(s.ControlLocked(100 + StormHazardSystem.Ticks(StormHazardSystem.StunSeconds, Dt)), "정해진 시간 뒤 풀린다");
        }

        [Test]
        public void 이미_긴_기절_중에_짧은_기절이_와도_줄지_않는다()
        {
            var field = new StormHazardField();
            field.Clouds.Add(new StormCloud(y: 200f, orbit: 0f, startDegrees: 0f, degreesPerTick: 0f, bob: 0f, bobPeriod: 0, radius: 4f));
            var d = Diver(new Vector3(0f, 202f, 0f) - Vector3.up * 0.9f, new Vector3(0f, -30f, 0f));
            d.Get<StormState>().StunEndTick = 1000;

            new StormHazardSystem().Detect(d, 100, Dt, field, Config(), before: new Vector3(0f, 205f, 0f));

            Assert.AreEqual(StormHazardKind.StormCloud, d.Get<StormState>().LastHitKind, "맞기는 맞았다");
            Assert.AreEqual(1000, d.Get<StormState>().StunEndTick);
        }

        [Test]
        public void 상승_기류는_떠올랐다가_끝나는_틱에_바깥으로_뱉는다()
        {
            var field = new StormHazardField();
            field.Updrafts.Add(new Updraft(new Vector3(0f, 100f, 0f), 6f, 30f));
            var d = Diver(new Vector3(3f, 110f, 0f), new Vector3(0f, -20f, 0f));
            var sys = new StormHazardSystem();

            sys.Detect(d, 50, Dt, field, Config(), before: new Vector3(3f, 111f, 0f));
            long end = d.Get<StormState>().LiftEndTick;
            Assert.Greater(end, 50);

            sys.ApplyLift(d, 51, Dt, field);
            Assert.AreEqual(StormHazardSystem.LiftSpeed, d.Get<Velocity>().Linear.Y, 1e-4f, "떠오르는 동안은 위로");

            sys.ApplyLift(d, end, Dt, field);
            Assert.Greater(d.Get<Velocity>().Linear.X, 10f, "들어온 쪽(+x) 바깥으로 휙");
        }

        [Test]
        public void 되감기_사진은_기절_상태를_되돌린다()
        {
            var d = Diver(Vector3.zero, Vector3.zero);
            d.Get<StormState>().StunEndTick = 120;
            var saved = StormSavedState.Capture(d).Value;
            d.Get<StormState>().StunEndTick = 0;
            saved.RestoreTo(d);
            Assert.AreEqual(120, d.Get<StormState>().StunEndTick);
        }

        [Test]
        public void 갈매기_떼_자리는_틱만으로_정해진다()
        {
            var flock = new GullFlock(300f, 0f, 0f, 0.4f, 160f, 7, 0);
            Assert.AreEqual(flock.BirdAt(3, 1234), flock.BirdAt(3, 1234));
            Assert.AreEqual(flock.BirdAt(0, 0), flock.BirdAt(0, 400), "한 바퀴(160m / 0.4m) 뒤 제자리");
        }
    }
}
