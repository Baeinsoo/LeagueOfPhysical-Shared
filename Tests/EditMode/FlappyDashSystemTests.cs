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
        public void 절반_속도로_떨어지면_다이브는_8분의_1이다()
        {
            //  정규화가 살아 있는지 보는 검사다. <b>비례가 아니라 세제곱</b>이라는 것이 요점이다 —
            //  절반(15/30)이면 1/8만 받는다. 예전엔 이 테스트가 "절반이면 절반"을 지켰는데,
            //  그 선형 곡선이 평범한 비행과 과감한 다이브를 구분하지 못해 게이지가 늘 가득 찼다.
            //  곡선을 바꾸면서 지키는 값도 같이 옮겼다(무엇을 재는지는 그대로다).
            var bird = Bird(verticalSpeed: -15f);

            new FlappyDashSystem(Config()).Tick(bird, Dt);

            Assert.That(bird.Get<FlappyDash>().Charge,
                        Is.EqualTo((0.13f + 1.2f * 0.125f) * Dt).Within(Tolerance));
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
        public void 게이지는_두_칸을_넘지_않는다()
        {
            var bird = Bird(verticalSpeed: -30f, charge: 1.999f);

            new FlappyDashSystem(Config()).Tick(bird, Dt);

            Assert.That(bird.Get<FlappyDash>().Charge, Is.EqualTo(FlappyDash.MaxCharge));
        }

        [Test]
        public void 한_칸을_넘어도_다이브가_계속_붙는다()
        {
            //  이 게임의 리스크·리워드를 지키는 검사다. 상한이 한 칸이면 게이지가 차는 순간
            //  낮게 날 이유가 사라진다 — 두 칸까지 쌓이므로 그 보상이 끊기지 않아야 한다.
            var bird = Bird(verticalSpeed: -30f, charge: 1.2f);

            new FlappyDashSystem(Config()).Tick(bird, Dt);

            Assert.That(bird.Get<FlappyDash>().Charge,
                        Is.EqualTo(1.2f + (0.13f + 1.2f) * Dt).Within(Tolerance));
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
        public void 발동하면_한_칸만_쓰고_지속이_찬다()
        {
            var system = new FlappyDashSystem(Config());
            var bird = Bird(charge: 1f);

            Assert.That(system.TryActivate(bird), Is.True);
            Assert.That(bird.Get<FlappyDash>().Charge, Is.EqualTo(0f));
            Assert.That(bird.Get<FlappyDash>().DashRemaining, Is.EqualTo(0.2f).Within(Tolerance));
            Assert.That(system.IsDashing(bird), Is.True);
        }

        [Test]
        public void 한_칸_반일_때_발동하면_반_칸이_남는다()
        {
            //  덜 찬 몫이 증발하면 "조금 더 모아서 쓰자"가 손해가 되어, 두 칸이 있으나 마나가 된다.
            var system = new FlappyDashSystem(Config());
            var bird = Bird(charge: 1.5f);

            Assert.That(system.TryActivate(bird), Is.True);
            Assert.That(bird.Get<FlappyDash>().Charge, Is.EqualTo(0.5f).Within(Tolerance));
        }

        [Test]
        public void 두_칸이면_대시가_끝나는_대로_한_번_더_쓴다()
        {
            var system = new FlappyDashSystem(Config());
            var bird = Bird(charge: FlappyDash.MaxCharge);

            Assert.That(system.TryActivate(bird), Is.True, "첫 칸");

            //  대시가 끝날 때까지 돌린다(0.2초 = 10틱). 낙하 속도가 0이라 그동안 다이브는 안 붙는다.
            for (int i = 0; i < 10; i++)
            {
                system.Tick(bird, Dt);
            }

            Assert.That(system.IsDashing(bird), Is.False);
            Assert.That(system.TryActivate(bird), Is.True, "모아 둔 둘째 칸");
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

        //  ── 충전 곡선 — 이 게임의 리스크·리워드를 숫자로 못박는다 ──────────────
        //  곡선을 건드리면 여기가 먼저 빨개진다.

        //  새 경제의 값. 기본 충전 0(공짜 없음) · 다이브 계수 1.4 · 실제 물리값.
        private static FlappyConfig DiveConfig()
            => new FlappyConfig(forwardSpeed: 6.8f, flapImpulse: 18.6f, gravity: 59f, maxFallSpeed: 30f,
                                bodyRadius: 0.45f, bodyHeight: 0.9f, restitution: 0.35f,
                                stunTime: 0.8f, invulnTime: 0.6f,
                                dashMult: 2f, dashDuration: 0.2f, dashChargeBase: 0f, dashChargeDive: 1.4f);

        [Test]
        public void 평범한_날갯짓_속도에서는_거의_안_찬다()
        {
            //  날갯짓은 튀었다 떨어지는 반복이라 평범하게 날아도 시간의 절반이 낙하다.
            //  그 평균 낙하(9.3 m/s = 최대의 31%)가 충전의 31%를 받아가면 "과감함"이 값을 잃는다.
            //  세제곱이면 31% → 3%다.
            var bird = Bird(verticalSpeed: -9.3f);

            new FlappyDashSystem(DiveConfig()).Tick(bird, 1f);

            float max = DiveConfig().DashChargeDive;   // 최대 낙하에서의 1초치
            Assert.Less(bird.Get<FlappyDash>().Charge, max * 0.05f,
                        "평범한 비행이 최대의 5%를 넘으면 과감함을 구분하지 못한다");
        }

        //  문턱 29 m/s — 정지에서 약 7.1m 떨어져야 넘는다(점프 세 번 높이면 이미 최고 속도).
        private static FlappyConfig ThresholdConfig()
            => new FlappyConfig(forwardSpeed: 6.8f, flapImpulse: 18.6f, gravity: 59f, maxFallSpeed: 30f,
                                bodyRadius: 0.45f, bodyHeight: 0.9f, restitution: 0.35f,
                                stunTime: 1.2f, invulnTime: 0.6f,
                                dashMult: 2f, dashDuration: 0.4f, dashChargeBase: 0f, dashChargeDive: 1.3f,
                                dashChargeMinFall: 29f);

        [Test]
        public void 문턱보다_느리게_떨어지면_하나도_안_찬다()
        {
            var bird = Bird(verticalSpeed: -28.9f);

            new FlappyDashSystem(ThresholdConfig()).Tick(bird, 1f);

            Assert.AreEqual(0f, bird.Get<FlappyDash>().Charge);
        }

        [Test]
        public void 문턱을_넘으면_원래_곡선대로_받는다()
        {
            var bird = Bird(verticalSpeed: -30f);

            new FlappyDashSystem(ThresholdConfig()).Tick(bird, 0.1f);

            Assert.That(bird.Get<FlappyDash>().Charge, Is.EqualTo(1.3f * 0.1f).Within(1e-5f));
        }

        [Test]
        public void 최대_낙하에서는_계수를_그대로_받는다()
        {
            var bird = Bird(verticalSpeed: -30f);   // MaxFallSpeed

            new FlappyDashSystem(DiveConfig()).Tick(bird, 0.1f);

            Assert.That(bird.Get<FlappyDash>().Charge,
                        Is.EqualTo(DiveConfig().DashChargeDive * 0.1f).Within(1e-5f));
        }

        [Test]
        public void 회랑_전체를_다이브하면_반_칸이다()
        {
            //  14.56m(화면 세로 = 회랑)를 중력 59로 떨어지는 동안 실제로 얼마나 차는지.
            //  이 값이 "한 칸 = 깊은 다이브 두 번"이라는 경제를 지킨다 — 회랑 높이나 중력을
            //  바꾸면 여기가 먼저 말해 준다.
            FlappyConfig config = DiveConfig();
            var system = new FlappyDashSystem(config);
            var bird = Bird();
            var velocity = bird.Get<Velocity>();

            float fallen = 0f;
            while (fallen < 14.56f)
            {
                float vy = velocity.Linear.Y - config.Gravity * Dt;
                if (vy < -config.MaxFallSpeed) { vy = -config.MaxFallSpeed; }
                velocity.Linear = new System.Numerics.Vector3(velocity.Linear.X, vy, 0f);
                fallen += -vy * Dt;
                system.Tick(bird, Dt);
            }

            Assert.That(bird.Get<FlappyDash>().Charge, Is.EqualTo(0.50f).Within(0.06f));
        }

        [Test]
        public void 기본_충전이_0이면_올라갈_때는_안_찬다()
        {
            var bird = Bird(verticalSpeed: +18.6f);

            new FlappyDashSystem(DiveConfig()).Tick(bird, 1f);

            Assert.That(bird.Get<FlappyDash>().Charge, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void 부스트는_게이지를_쓰지_않는다()
        {
            //  패드는 공짜 대시다. 게이지를 쓰면 다이브로 번 것과 뒤섞여 경제가 무너진다.
            var bird = Bird(charge: 0f);

            new FlappyDashSystem(Config()).Boost(bird, 0.6f);

            Assert.That(bird.Get<FlappyDash>().DashRemaining, Is.EqualTo(0.6f).Within(Tolerance));
            Assert.That(bird.Get<FlappyDash>().Charge, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void 부스트는_더_긴_쪽으로만_갱신된다()
        {
            //  패드 위를 지나는 동안 매 틱 다시 밟힌다 — 덮어쓰면 나가는 순간 항상 duration이 남아
            //  "긴 패드가 더 오래 간다"가 깨진다.
            var bird = Bird();
            var system = new FlappyDashSystem(Config());

            system.Boost(bird, 0.6f);
            system.Boost(bird, 0.3f);

            Assert.That(bird.Get<FlappyDash>().DashRemaining, Is.EqualTo(0.6f).Within(Tolerance));

            system.Boost(bird, 0.9f);

            Assert.That(bird.Get<FlappyDash>().DashRemaining, Is.EqualTo(0.9f).Within(Tolerance));
        }

        [Test]
        public void 부스트는_취소_뒤에_다시_받을_수_있다()
        {
            //  스턴에 들어가며 Cancel된 새가 풀린 뒤 패드를 밟으면 다시 붙어야 한다.
            var bird = Bird();
            var system = new FlappyDashSystem(Config());

            system.Boost(bird, 0.6f);
            system.Cancel(bird);

            Assert.That(bird.Get<FlappyDash>().DashRemaining, Is.EqualTo(0f).Within(Tolerance));

            system.Boost(bird, 0.6f);

            Assert.That(bird.Get<FlappyDash>().DashRemaining, Is.EqualTo(0.6f).Within(Tolerance));
        }

        [Test]
        public void 지속시간이_0이면_부스트가_아니다()
        {
            //  씬에서 값을 비워 둔 패드가 대시를 "0초"로 덮어쓰지 않게 한다.
            var bird = Bird();
            var system = new FlappyDashSystem(Config());
            system.Boost(bird, 0.6f);

            system.Boost(bird, 0f);

            Assert.That(bird.Get<FlappyDash>().DashRemaining, Is.EqualTo(0.6f).Within(Tolerance));
        }
    }
}
