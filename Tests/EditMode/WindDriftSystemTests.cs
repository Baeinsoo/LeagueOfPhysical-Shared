using NUnit.Framework;
using System.Numerics;

namespace LOP.Tests
{
    public class WindDriftSystemTests
    {
        const float Tolerance = 1e-3f;

        static SkydiveConfig Config()
            => new SkydiveConfig(
                spreadFallSpeed: 60f, diveFallSpeed: 90f, glideFallSpeed: 6f,
                spreadMoveSpeed: 12f, diveMoveSpeed: 9f, glideMoveSpeed: 14f,
                spreadTurnAccel: 22f, diveTurnAccel: 6f, glideTurnAccel: 18f,
                fallApproach: 29f, postureRate: 4f,
                bodyRadius: 0.4f, bodyHeight: 1.8f, groundY: 0f,
                staminaMax: 100f, glideDrain: 20f, groundRecover: 40f, emergencyGlideTime: 1f,
                groundMoveSpeed: 4f, groundAccel: 100f, jumpPower: 11f, poseClearance: 5f, fallBrake: 150f,
                glideWindLag: 0.2f, spreadWindLag: 2.0f, diveWindLag: 4.0f);

        // 온 코스를 덮는 상승풍 14. 위치를 안 옮겨도 늘 안에 있다.
        static WindField Everywhere(float up = 14f)
        {
            var field = new WindField();
            field.Add(new WindCylinder(new Vector3(0f, 1000f, 0f), 1000f, 2000f, new Vector3(0f, up, 0f)));
            return field;
        }

        static GameFramework.World.Entity Diver(
            float axis = 0f, bool gliding = false,
            SkydiveMotionState state = SkydiveMotionState.Skydiving)
        {
            var entity = new GameFramework.World.Entity("diver-1");
            entity.Add(new GameFramework.World.Transform { Position = new Vector3(0f, 1000f, 0f) });
            entity.Add(new Posture { Axis = axis, Gliding = gliding });
            entity.Add(new MotionState { Value = state });
            entity.Add(new WindDrift());
            return entity;
        }

        static void Run(WindDriftSystem system, GameFramework.World.Entity entity,
                        WindField field, float seconds, float dt = 0.05f)
        {
            int steps = (int)System.Math.Round(seconds / dt);
            for (int i = 0; i < steps; i++)
            {
                system.Tick(entity, dt, Config(), field);
            }
        }

        [Test]
        public void 대자는_SpreadWindLag초에_바람을_다_탄다()
        {
            var entity = Diver(axis: 0f);
            Run(new WindDriftSystem(), entity, Everywhere(), seconds: 2.0f);

            Assert.AreEqual(14f, entity.Get<WindDrift>().Value.Y, Tolerance);
        }

        [Test]
        public void 대자가_다_타기_전에는_비율만큼만_탄다()
        {
            var entity = Diver(axis: 0f);
            Run(new WindDriftSystem(), entity, Everywhere(), seconds: 1.0f);

            // 일정 속도로 다가가므로 절반 시간이면 절반이다.
            Assert.AreEqual(7f, entity.Get<WindDrift>().Value.Y, Tolerance);
        }

        [Test]
        public void 다이브는_같은_시간에_절반만_탄다()
        {
            var entity = Diver(axis: 1f);
            Run(new WindDriftSystem(), entity, Everywhere(), seconds: 2.0f);

            // DiveWindLag 4초 중 2초 = 절반
            Assert.AreEqual(7f, entity.Get<WindDrift>().Value.Y, Tolerance);
        }

        [Test]
        public void 패러세일은_0점2초면_다_탄다()
        {
            var entity = Diver(gliding: true);
            Run(new WindDriftSystem(), entity, Everywhere(), seconds: 0.2f, dt: 0.05f);

            Assert.AreEqual(14f, entity.Get<WindDrift>().Value.Y, Tolerance);
        }

        [Test]
        public void 자세_축_중간은_두_지연_사이다()
        {
            var entity = Diver(axis: 0.5f);
            Run(new WindDriftSystem(), entity, Everywhere(), seconds: 1.5f);

            // 지연 = (2 + 4) / 2 = 3초. 1.5초면 절반.
            Assert.AreEqual(7f, entity.Get<WindDrift>().Value.Y, Tolerance);
        }

        // 들어갈 때만 시간이 걸리고 나올 때 즉시 풀리면, 볼륨을 스치기만 해도 바람이 남지 않는다.
        [Test]
        public void 볼륨을_나가면_같은_시간에_0으로_돌아온다()
        {
            var system = new WindDriftSystem();
            var entity = Diver(axis: 0f);
            Run(system, entity, Everywhere(), seconds: 2.0f);
            Assert.AreEqual(14f, entity.Get<WindDrift>().Value.Y, Tolerance);

            Run(system, entity, new WindField(), seconds: 2.0f);

            Assert.AreEqual(0f, entity.Get<WindDrift>().Value.Y, Tolerance);
        }

        // 발을 딛고 있으면 땅이 잡아 준다. 안 그러면 발판 위에서 걷다가 바람에 끌려간다.
        [Test]
        public void 걸을_때는_바람에_안_실린다()
        {
            var entity = Diver(state: SkydiveMotionState.Walking);
            Run(new WindDriftSystem(), entity, Everywhere(), seconds: 2.0f);

            Assert.AreEqual(0f, entity.Get<WindDrift>().Value.Y, Tolerance);
        }

        [Test]
        public void 옆바람도_같은_규칙으로_실린다()
        {
            var field = new WindField();
            field.Add(new WindCylinder(new Vector3(0f, 1000f, 0f), 1000f, 2000f, new Vector3(20f, 0f, 0f)));
            var entity = Diver(axis: 0f);

            Run(new WindDriftSystem(), entity, field, seconds: 2.0f);

            Assert.AreEqual(20f, entity.Get<WindDrift>().Value.X, Tolerance);
            Assert.AreEqual(0f, entity.Get<WindDrift>().Value.Y, Tolerance);
        }

        [Test]
        public void WindDrift가_없는_몸은_그냥_넘어간다()
        {
            var entity = new GameFramework.World.Entity("no-drift");
            entity.Add(new GameFramework.World.Transform { Position = new Vector3(0f, 1000f, 0f) });

            Assert.DoesNotThrow(() => new WindDriftSystem().Tick(entity, 0.05f, Config(), Everywhere()));
        }
    }
}
