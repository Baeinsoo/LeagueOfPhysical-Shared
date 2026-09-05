using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// 통과 뒤의 움직임. 100m 주자가 결승선을 지나 감속하는 그림이고, 조작이 끊겨야 골인 뒤
    /// 행동이 등수에 영향을 주지 않는다(레이싱 장르 관례).
    /// </summary>
    public class FlappyMoveSystemFinishTests
    {
        private const float Dt = 0.02f;
        private const float Tolerance = 1e-4f;

        private static FlappyConfig Config()
            => new FlappyConfig(forwardSpeed: 11f, flapImpulse: 23f, gravity: 70f, maxFallSpeed: 30f,
                                bodyRadius: 0.45f, bodyHeight: 0.9f, restitution: 0.35f,
                                stunTime: 0.8f, invulnTime: 0.6f,
                                dashMult: 2f, dashDuration: 0.2f, dashChargeBase: 0.13f, dashChargeDive: 1.2f,
                                chaserStartX: -60f, chaserInitialSpeed: 7f,
                                chaserAcceleration: 0.075f, chaserMaxSpeed: 10f,
                                finishBrake: 5.5f);

        private static Entity Bird(float forwardSpeed, float verticalSpeed, bool jump)
        {
            var bird = new Entity("bird");
            bird.Add(new Velocity
            {
                Linear = new System.Numerics.Vector3(forwardSpeed, verticalSpeed, 0f)
            });
            var buffer = new InputBuffer();
            buffer.Current = new InputCommand { Jump = jump };
            bird.Add(buffer);
            return bird;
        }

        [Test]
        public void 통과하면_중력이_안_실린다()
        {
            var bird = Bird(11f, 0f, jump: false);

            new FlappyMoveSystem(Config()).Tick(bird, Dt, dashing: false, finished: true);

            Assert.That(bird.Get<Velocity>().Linear.Y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void 통과하면_날갯짓이_안_먹는다()
        {
            //  골인 뒤 행동이 등수에 영향을 주면 안 된다.
            var bird = Bird(11f, 0f, jump: true);

            new FlappyMoveSystem(Config()).Tick(bird, Dt, dashing: false, finished: true);

            Assert.That(bird.Get<Velocity>().Linear.Y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void 통과하면_전진이_줄어든다()
        {
            var bird = Bird(11f, 0f, jump: false);

            new FlappyMoveSystem(Config()).Tick(bird, Dt, dashing: false, finished: true);

            Assert.That(bird.Get<Velocity>().Linear.X, Is.EqualTo(11f - 5.5f * Dt).Within(Tolerance));
        }

        [Test]
        public void 감속은_0에서_멈추고_뒤로_안_간다()
        {
            //  음수로 내려가면 새가 결승선 쪽으로 되돌아온다.
            var bird = Bird(0.01f, 0f, jump: false);

            new FlappyMoveSystem(Config()).Tick(bird, Dt, dashing: false, finished: true);

            Assert.That(bird.Get<Velocity>().Linear.X, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void 통과_전에는_평소대로다()
        {
            var bird = Bird(11f, 0f, jump: false);

            new FlappyMoveSystem(Config()).Tick(bird, Dt, dashing: false, finished: false);

            Assert.That(bird.Get<Velocity>().Linear.X, Is.EqualTo(11f).Within(Tolerance));
            Assert.That(bird.Get<Velocity>().Linear.Y, Is.EqualTo(-70f * Dt).Within(Tolerance));
        }
    }
}
