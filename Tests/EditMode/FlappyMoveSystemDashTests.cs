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
        public void 대시는_누른_순간부터_최고_속도를_유지하다_끝에서_줄어든다()
        {
            //  시작 배수 2, 대시 0.2초, 유지 구간 70%(꼬리 30%=0.06초): 남은 시간이 0.1초면 아직 유지 구간(t=0.5≥0.3),
            //  0.03초(t=0.15, 꼬리의 절반)면 1 + 1×0.5 = 1.5배로 곧게 내려온다.
            var bird = Bird(verticalSpeed: 0f);
            var dash = new FlappyDash { DashRemaining = 0.1f };
            bird.Add(dash);

            new FlappyMoveSystem(Config()).Tick(bird, Dt, dashing: true, finished: false);
            Assert.That(VelocityOf(bird).X, Is.EqualTo(22f).Within(Tolerance), "아직 유지 구간");

            dash.DashRemaining = 0.03f;
            new FlappyMoveSystem(Config()).Tick(bird, Dt, dashing: true, finished: false);
            Assert.That(VelocityOf(bird).X, Is.EqualTo(11f * 1.5f).Within(Tolerance), "꼬리 구간 절반");

            dash.DashRemaining = 0.2f;
            new FlappyMoveSystem(Config()).Tick(bird, Dt, dashing: true, finished: false);
            Assert.That(VelocityOf(bird).X, Is.EqualTo(22f).Within(Tolerance), "누른 순간은 시작 배수 그대로");
        }

        [Test]
        public void 새_곡선의_추가_거리는_원래_곡선의_1_2배다()
        {
            //  원래 곡선(0.4초·3배·곧은 감속) 추가 거리 2.856m는 옛 곡선의 틱 합이다 — 옛 곡선은 이제 코드에 없어 숫자로 박는다.
            //  새 곡선(0.48초·최고 배율 2.2·유지 70%+꼬리 30% 감속)은 1.2배에 맞춘 값이다.
            float d = FlappyDashCurve.Distance(6.8f, 0.48f, 0.48f, 2.2f, 0.02f);
            float extra = d - 6.8f * 0.48f;
            Assert.That(extra, Is.EqualTo(3.4091f).Within(1e-3f));
            Assert.That(extra / 2.856f, Is.EqualTo(1.1937f).Within(1e-3f));
        }

        [Test]
        public void 배율은_누른_순간_최고이고_끝_30퍼센트에서만_줄어든다()
        {
            Assert.That(FlappyDashCurve.Multiplier(0.48f, 0.48f, 2.2f), Is.EqualTo(2.2f).Within(Tolerance));
            Assert.That(FlappyDashCurve.Multiplier(0.24f, 0.48f, 2.2f), Is.EqualTo(2.2f).Within(Tolerance), "유지 구간");
            Assert.That(FlappyDashCurve.Multiplier(0.144f, 0.48f, 2.2f), Is.EqualTo(2.2f).Within(Tolerance), "유지 구간 경계");
            Assert.That(FlappyDashCurve.Multiplier(0.072f, 0.48f, 2.2f), Is.EqualTo(1.6f).Within(Tolerance), "꼬리 구간 절반");
            Assert.That(FlappyDashCurve.Multiplier(0f, 0.48f, 2.2f), Is.EqualTo(1f).Within(Tolerance));
            float previous = float.MaxValue;
            for (float r = 0.48f; r >= 0f; r -= 0.02f)
            {
                float m = FlappyDashCurve.Multiplier(r, 0.48f, 2.2f);
                Assert.That(m, Is.LessThanOrEqualTo(previous + Tolerance), $"remaining={r:F2}");
                previous = m;
            }
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
