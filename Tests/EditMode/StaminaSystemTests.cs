using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    public class StaminaSystemTests
    {
        const float Tolerance = 1e-3f;

        static SkydiveConfig Config()
            => new SkydiveConfig(
                spreadFallSpeed: 25f, diveFallSpeed: 45f, glideFallSpeed: 6f,
                spreadMoveSpeed: 12f, diveMoveSpeed: 18f, glideMoveSpeed: 14f,
                spreadTurnAccel: 22f, diveTurnAccel: 6f, glideTurnAccel: 18f,
                fallApproach: 30f, postureRate: 4f,
                bodyRadius: 0.4f, bodyHeight: 1.8f, groundY: 0f,
                staminaMax: 100f, glideDrain: 20f, groundRecover: 40f, emergencyGlideTime: 1f,
                groundMoveSpeed: 4f, groundAccel: 100f, jumpPower: 11f);

        static Entity Diver(float stamina, bool gliding)
        {
            var e = new Entity("diver-1");
            e.Add(new Posture { Axis = 0f, Gliding = gliding });
            e.Add(new Stamina { Current = stamina });
            return e;
        }

        [Test]
        public void 패러세일을_켜면_줄어든다()
        {
            var e = Diver(100f, gliding: true);

            new StaminaSystem().Tick(e, 0.5f, Config(), grounded: false);

            Assert.AreEqual(90f, e.Get<Stamina>().Current, Tolerance);   // 20/s × 0.5s
        }

        [Test]
        public void 자유낙하는_공짜다()
        {
            var e = Diver(100f, gliding: false);

            new StaminaSystem().Tick(e, 1f, Config(), grounded: false);

            Assert.AreEqual(100f, e.Get<Stamina>().Current, Tolerance);
        }

        [Test]
        public void 공중에서는_회복되지_않는다()
        {
            var e = Diver(10f, gliding: false);

            new StaminaSystem().Tick(e, 1f, Config(), grounded: false);

            Assert.AreEqual(10f, e.Get<Stamina>().Current, Tolerance);
        }

        [Test]
        public void 발_딛고_있으면_회복된다()
        {
            var e = Diver(10f, gliding: false);

            new StaminaSystem().Tick(e, 0.5f, Config(), grounded: true);

            Assert.AreEqual(30f, e.Get<Stamina>().Current, Tolerance);   // 40/s × 0.5s
        }

        [Test]
        public void 다_떨어지면_패러세일이_저절로_접힌다()
        {
            var e = Diver(5f, gliding: true);

            new StaminaSystem().Tick(e, 1f, Config(), grounded: false);

            Assert.AreEqual(0f, e.Get<Stamina>().Current, Tolerance);
            Assert.IsFalse(e.Get<Posture>().Gliding, "잔고가 0이면 접혀야 한다");
        }

        [Test]
        public void 잔고가_0이어도_마지막_펼침이_한_번_허용된다()
        {
            var e = Diver(0f, gliding: false);
            var sys = new StaminaSystem();

            Assert.IsTrue(sys.TryStartGlide(e, Config()), "첫 비상 펼침은 허용된다");
            Assert.IsTrue(e.Get<Posture>().Gliding);

            // 비상 시간이 끝나면 접힌다
            sys.Tick(e, 1.1f, Config(), grounded: false);
            Assert.IsFalse(e.Get<Posture>().Gliding);

            Assert.IsFalse(sys.TryStartGlide(e, Config()), "두 번째는 허용되지 않는다");
        }

        [Test]
        public void 잔고가_있으면_비상_횟수를_쓰지_않는다()
        {
            var e = Diver(50f, gliding: false);
            var sys = new StaminaSystem();

            Assert.IsTrue(sys.TryStartGlide(e, Config()));

            Assert.IsFalse(e.Get<Stamina>().EmergencyUsed, "잔고로 폈으면 비상 횟수는 그대로다");
        }
    }
}
