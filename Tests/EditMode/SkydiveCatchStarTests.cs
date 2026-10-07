using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>별 조각 붙잡기 결승(왕국의 눈물 엔딩 오마주) — 틱만 넣으면 별 자리가 나오고, 몸이 닿으면 그 틱이 결승.</summary>
    public class SkydiveCatchStarTests
    {
        //  가운데 (0,150,0), 반지름 40으로 1°/틱 돌고, 위아래 ±20을 400틱 주기로.
        private static CatchTarget Star() => new CatchTarget(new Vector3(0f, 150f, 0f), orbitRadius: 40f, degreesPerTick: 1f, startDegrees: 0f,
                                                             bobAmplitude: 20f, bobPeriod: 400, catchRadius: 6f);

        [Test]
        public void 별_자리는_틱만으로_정해진다()
        {
            var s = Star();
            Vector3 p0 = CatchTargetGeometry.PositionAt(s, 0);
            Assert.AreEqual(40f, p0.x, 1e-3f);
            Assert.AreEqual(150f, p0.y, 1e-3f);
            Vector3 p90 = CatchTargetGeometry.PositionAt(s, 90);
            Assert.AreEqual(0f, p90.x, 1e-3f);
            Assert.AreEqual(40f, p90.z, 1e-3f);
            Assert.AreEqual(170f, CatchTargetGeometry.PositionAt(s, 100).y, 1e-3f, "주기 1/4에 위로 최고");
            Assert.AreEqual(p0, CatchTargetGeometry.PositionAt(s, 3600 * 4), "한 바퀴·한 주기 뒤 제자리");
        }

        [Test]
        public void 몸이_닿으면_잡는다()
        {
            var s = Star();
            Vector3 star = CatchTargetGeometry.PositionAt(s, 0);
            Assert.IsTrue(CatchTargetGeometry.Catches(s, 0, star + new Vector3(0f, -0.9f, 5f), bodyRadius: 0.4f, bodyHeight: 1.8f), "몸 가운데에서 5m — 닿음");
            Assert.IsFalse(CatchTargetGeometry.Catches(s, 0, star + new Vector3(0f, -0.9f, 9f), 0.4f, 1.8f), "9m — 안 닿음");
        }

        [Test]
        public void 별을_잡으면_그_틱이_결승이고_더_가까울수록_깊다()
        {
            var field = new CatchTargetField();
            field.Add(Star());
            Vector3 star = CatchTargetGeometry.PositionAt(Star(), 50);

            var near = Diver(star + new Vector3(0f, -0.9f, 1f));
            var far = Diver(star + new Vector3(0f, -0.9f, 5f));
            Assert.IsTrue(SkydiveCatch.TryCatch(near, field, 50, 0.4f, 1.8f));
            Assert.IsTrue(SkydiveCatch.TryCatch(far, field, 50, 0.4f, 1.8f));
            Assert.AreEqual(50, near.Get<FinishState>().FinishedTick);
            Assert.Greater(near.Get<FinishState>().Depth, far.Get<FinishState>().Depth, "같은 틱이면 더 깊이 들어간 쪽이 먼저");
        }

        [Test]
        public void 이미_결승한_사람은_다시_적지_않는다()
        {
            var field = new CatchTargetField();
            field.Add(Star());
            var d = Diver(CatchTargetGeometry.PositionAt(Star(), 10) + new Vector3(0f, -0.9f, 0f));
            d.Get<FinishState>().FinishedTick = 3;
            Assert.IsFalse(SkydiveCatch.TryCatch(d, field, 10, 0.4f, 1.8f));
            Assert.AreEqual(3, d.Get<FinishState>().FinishedTick);
        }

        [Test]
        public void 결승한_사람은_패러세일이_펴지고_스태미나가_마르지_않는다()
        {
            //  붙잡은 뒤 함께 천천히 내려앉는다 — 착지 사망도 막는다.
            var d = Diver(Vector3.zero);
            d.Get<FinishState>().FinishedTick = 1;
            d.Get<Stamina>().Current = 0f;
            SkydiveCatch.SettleAfterFinish(d, staminaMax: 300f);
            Assert.IsTrue(d.Get<Posture>().Gliding);
            Assert.AreEqual(300f, d.Get<Stamina>().Current);
        }

        private static Entity Diver(Vector3 feet)
        {
            var e = new Entity("d");
            e.Add(new GameFramework.World.Transform());
            e.Add(new Velocity());
            e.Add(new FinishState());
            e.Add(new Posture());
            e.Add(new Stamina { Current = 100f });
            e.Teleport(feet);
            return e;
        }
    }
}
