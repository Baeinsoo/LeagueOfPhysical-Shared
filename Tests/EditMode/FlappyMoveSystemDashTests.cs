using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// 대시 중의 이동. 대시는 <b>완전한 수평 직선</b>이어야 한다 — 중력도 날갯짓도 그 동안엔 없다.
    /// 그 성질이 깨지면 대시가 "빠른 점프"가 되어 노려서 쓰는 도구가 아니게 된다.
    /// </summary>
    public class FlappyMoveSystemDashTests
    {
        private const float Dt = 0.02f;
        private const float Tolerance = 1e-4f;

        private static FlappyConfig Config()
            => new FlappyConfig(forwardSpeed: 11f, flapImpulse: 23f, gravity: 70f, maxFallSpeed: 30f,
                                bodyRadius: 0.45f, bodyHeight: 0.9f, restitution: 0.35f,
                                stunTime: 0.8f, invulnTime: 0.6f,
                                dashMult: 2f, dashDuration: 0.2f, dashChargeBase: 0.13f, dashChargeDive: 1.2f);

        private static Entity Bird(float verticalSpeed, bool jump = false)
        {
            var bird = new Entity("bird");
            bird.Add(new Velocity { Linear = new System.Numerics.Vector3(11f, verticalSpeed, 0f) });
            bird.Add(new InputBuffer { Current = new InputCommand { Jump = jump } });
            return bird;
        }

        private static System.Numerics.Vector3 VelocityOf(Entity bird)
            => bird.Get<Velocity>().Linear;

        [Test]
        public void 대시_중에는_전진이_두_배다()
        {
            var bird = Bird(verticalSpeed: -5f);

            new FlappyMoveSystem(Config()).Tick(bird, Dt, dashing: true, finished: false);

            Assert.That(VelocityOf(bird).X, Is.EqualTo(22f).Within(Tolerance));
        }

        [Test]
        public void 대시_중에는_세로_속도가_0이고_중력이_안_먹는다()
        {
            var bird = Bird(verticalSpeed: -5f);

            new FlappyMoveSystem(Config()).Tick(bird, Dt, dashing: true, finished: false);

            Assert.That(VelocityOf(bird).Y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void 대시_중_날갯짓은_무시된다()
        {
            //  여기서 플랩이 먹으면 수평 직선이 깨진다 — 그러면 대시가 아니라 빠른 점프다.
            var bird = Bird(verticalSpeed: -5f, jump: true);

            new FlappyMoveSystem(Config()).Tick(bird, Dt, dashing: true, finished: false);

            Assert.That(VelocityOf(bird).Y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void 대시가_아니면_예전과_똑같이_중력을_받는다()
        {
            var bird = Bird(verticalSpeed: 0f);

            new FlappyMoveSystem(Config()).Tick(bird, Dt, dashing: false, finished: false);

            Assert.That(VelocityOf(bird).X, Is.EqualTo(11f).Within(Tolerance));
            Assert.That(VelocityOf(bird).Y, Is.EqualTo(-70f * Dt).Within(Tolerance));
        }

        [Test]
        public void 대시가_아니면_날갯짓이_그대로_먹는다()
        {
            var bird = Bird(verticalSpeed: -5f, jump: true);

            new FlappyMoveSystem(Config()).Tick(bird, Dt, dashing: false, finished: false);

            Assert.That(VelocityOf(bird).Y, Is.EqualTo(23f).Within(Tolerance));
        }

        [Test]
        public void 대시가_끝나면_전진이_바로_원래대로_돌아온다()
        {
            var bird = Bird(verticalSpeed: 0f);

            new FlappyMoveSystem(Config()).Tick(bird, Dt, dashing: true, finished: false);
            Assert.That(VelocityOf(bird).X, Is.EqualTo(22f).Within(Tolerance));

            new FlappyMoveSystem(Config()).Tick(bird, Dt, dashing: false, finished: false);
            Assert.That(VelocityOf(bird).X, Is.EqualTo(11f).Within(Tolerance),
                "대시가 끝난 틱에는 전진이 즉시 상수로 돌아와야 한다 — 여운이 남으면 안 된다");
        }
    }
}
