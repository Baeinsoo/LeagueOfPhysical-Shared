using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// 대시 게이지가 차는 규칙과 발동·소진. 게이지는 "떨어질수록 빨리 찬다"가 전부라
    /// 낙하 속도에 따라 얼마나 붙는지가 이 게임의 리스크·리워드를 그대로 정한다.
    /// </summary>
    public class FlappyDashSystemTests
    {
        private const float Dt = 0.02f;   // 50Hz
        private const float Tolerance = 1e-6f;

        //  대시와 낙하에 관계된 값만 실제 값이고 나머지는 이 테스트에 무의미한 자리채움이다.
        private static FlappyConfig Config()
            => new FlappyConfig(forwardSpeed: 11f, flapImpulse: 23f, gravity: 70f, maxFallSpeed: 30f,
                                bodyRadius: 0.45f, bodyHeight: 0.9f, restitution: 0.35f,
                                stunTime: 0.8f, invulnTime: 0.6f,
                                dashMult: 2f, dashDuration: 0.2f, dashChargeBase: 0.13f, dashChargeDive: 1.2f);

        private static Entity Bird(float verticalSpeed = 0f, float charge = 0f)
        {
            var bird = new Entity("bird");
            bird.Add(new Velocity { Linear = new System.Numerics.Vector3(11f, verticalSpeed, 0f) });
            bird.Add(new FlappyDash { Charge = charge });
            return bird;
        }

        [Test]
        public void 안_떨어지면_기본_속도로만_찬다()
        {
            var bird = Bird(verticalSpeed: 0f);

            new FlappyDashSystem(Config()).Tick(bird, Dt);

            Assert.That(bird.Get<FlappyDash>().Charge, Is.EqualTo(0.13f * Dt).Within(Tolerance));
        }

        [Test]
        public void 최고_속도로_떨어지면_기본에_다이브가_다_더해진다()
        {
            var bird = Bird(verticalSpeed: -30f);   // 최대낙하

            new FlappyDashSystem(Config()).Tick(bird, Dt);

            Assert.That(bird.Get<FlappyDash>().Charge, Is.EqualTo((0.13f + 1.2f) * Dt).Within(Tolerance));
        }

        [Test]
        public void 절반_속도로_떨어지면_다이브도_절반만_더해진다()
        {
            //  정규화가 살아 있는지 보는 검사다. 낙하 속도에 비례해야 "낮게 날수록 보상"이 성립한다.
            var bird = Bird(verticalSpeed: -15f);

            new FlappyDashSystem(Config()).Tick(bird, Dt);

            Assert.That(bird.Get<FlappyDash>().Charge, Is.EqualTo((0.13f + 0.6f) * Dt).Within(Tolerance));
        }

        [Test]
        public void 최대낙하보다_빨라도_다이브_몫은_더_안_커진다()
        {
            var bird = Bird(verticalSpeed: -100f);

            new FlappyDashSystem(Config()).Tick(bird, Dt);

            Assert.That(bird.Get<FlappyDash>().Charge, Is.EqualTo((0.13f + 1.2f) * Dt).Within(Tolerance));
        }

        [Test]
        public void 올라가는_중에는_다이브가_안_붙는다()
        {
            var bird = Bird(verticalSpeed: 23f);   // 막 날갯짓한 직후

            new FlappyDashSystem(Config()).Tick(bird, Dt);

            Assert.That(bird.Get<FlappyDash>().Charge, Is.EqualTo(0.13f * Dt).Within(Tolerance));
        }

        [Test]
        public void 게이지는_1을_넘지_않는다()
        {
            var bird = Bird(verticalSpeed: -30f, charge: 0.999f);

            new FlappyDashSystem(Config()).Tick(bird, Dt);

            Assert.That(bird.Get<FlappyDash>().Charge, Is.EqualTo(1f));
        }

        [Test]
        public void 가득_차야만_발동한다()
        {
            var system = new FlappyDashSystem(Config());
            var bird = Bird(charge: 0.99f);

            Assert.That(system.TryActivate(bird), Is.False);
            Assert.That(system.IsDashing(bird), Is.False);
        }

        [Test]
        public void 발동하면_게이지를_전부_쓰고_지속이_찬다()
        {
            var system = new FlappyDashSystem(Config());
            var bird = Bird(charge: 1f);

            Assert.That(system.TryActivate(bird), Is.True);
            Assert.That(bird.Get<FlappyDash>().Charge, Is.EqualTo(0f));
            Assert.That(bird.Get<FlappyDash>().DashRemaining, Is.EqualTo(0.2f).Within(Tolerance));
            Assert.That(system.IsDashing(bird), Is.True);
        }

        [Test]
        public void 대시_중에는_다시_발동되지_않는다()
        {
            var system = new FlappyDashSystem(Config());
            var bird = Bird(charge: 1f);
            system.TryActivate(bird);
            bird.Get<FlappyDash>().Charge = 1f;   // 어떻게든 다시 찼다고 쳐도

            Assert.That(system.TryActivate(bird), Is.False);
        }

        [Test]
        public void 지속만큼의_틱_동안만_대시다()
        {
            //  0.2초 / 0.02초 = 10틱. 월드는 Tick(감소)을 먼저 부르고 그다음 이동하므로
            //  발동한 틱을 포함해 정확히 10틱이 대시여야 한다 — 한 틱이라도 더 가면 안 된다.
            var system = new FlappyDashSystem(Config());
            var bird = Bird(charge: 1f);
            system.TryActivate(bird);

            for (int i = 0; i < 10; i++)
            {
                Assert.That(system.IsDashing(bird), Is.True, $"{i}번째 틱은 아직 대시여야 한다");
                system.Tick(bird, Dt);
            }

            Assert.That(system.IsDashing(bird), Is.False, "10틱이 지나면 대시가 끝나야 한다");
        }

        [Test]
        public void 취소하면_그_자리에서_끝난다()
        {
            var system = new FlappyDashSystem(Config());
            var bird = Bird(charge: 1f);
            system.TryActivate(bird);

            system.Cancel(bird);

            Assert.That(system.IsDashing(bird), Is.False);
        }

        [Test]
        public void 대시_컴포넌트가_없는_엔티티에는_아무_일도_없다()
        {
            //  새가 아닌 엔티티(아이템 등)도 같은 루프를 지나간다.
            var system = new FlappyDashSystem(Config());
            var plain = new Entity("no-dash");

            Assert.That(system.IsDashing(plain), Is.False);
            Assert.That(system.TryActivate(plain), Is.False);
            Assert.DoesNotThrow(() => system.Tick(plain, Dt));
            Assert.DoesNotThrow(() => system.Cancel(plain));
        }

        [Test]
        public void 속도가_없는_엔티티도_기본_충전은_된다()
        {
            //  Velocity가 없으면 낙하 속도를 0으로 본다 — 예외로 죽지 않는 것이 요점이다.
            var bird = new Entity("no-velocity");
            bird.Add(new FlappyDash { Charge = 0f });

            new FlappyDashSystem(Config()).Tick(bird, Dt);

            Assert.That(bird.Get<FlappyDash>().Charge, Is.EqualTo(0.13f * Dt).Within(Tolerance));
        }
    }
}
