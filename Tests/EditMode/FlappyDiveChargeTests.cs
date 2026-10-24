using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// 다이브 충전(spec 2026-10-23-flappy-dive-charge §1): 날갯짓을 참고 빠르게 떨어지는 동안 찬다.
    /// 문턱 = 최고 낙하 × 비율, 이번 틱 = 세기 × (0.4 + 0.6·k) × 틱 간격.
    /// </summary>
    public class FlappyDiveChargeTests
    {
        //  원조 물리 최고 낙하 11.25 — 픽스처(30)가 아니라 실제 값으로 본다.
        static FlappyConfig Config()
            => new FlappyConfig(forwardSpeed: 4.5f, flapImpulse: 10.125f, gravity: 33.75f, maxFallSpeed: 11.25f,
                                bodyRadius: 0.45f, bodyHeight: 0.9f, restitution: 0.35f, stunTime: 1.2f, invulnTime: 0.6f,
                                dashMult: 2.2f, dashDuration: 0.48f, dashChargeBase: 0f, dashChargeDive: 1.3f,
                                dashChargeMinFall: 23.2f);   // 옛 문턱(넘을 수 없음) — 새 규칙이 이걸 안 쓰는지도 본다

        static FlappyMapRulesField DiveRules()
        {
            var rules = new FlappyMapRulesField();
            rules.Set(chaser: false, manualDash: true, diveCharge: true);
            return rules;
        }

        static Entity Bird(float fallSpeed, float charge = 0f)
        {
            var e = new Entity("bird");
            e.Add(new Velocity { Linear = new System.Numerics.Vector3(0f, -fallSpeed, 0f) });
            e.Add(new FlappyDash { Charge = charge });
            return e;
        }

        const float Dt = 0.02f;

        [Test]
        public void 문턱_아래로_떨어지면_안_찬다()
        {
            var bird = Bird(11.25f * 0.74f);
            new FlappyDashSystem(Config(), DiveRules()).Tick(bird, Dt);
            Assert.AreEqual(0f, bird.Get<FlappyDash>().Charge);
        }

        [Test]
        public void 문턱에서는_세기의_40퍼센트_최고_낙하에서는_세기만큼_찬다()
        {
            float rate = FlappyMapRulesField.DefaultDiveRate;
            var atMin = Bird(11.25f * 0.75f);
            var atMax = Bird(11.25f);
            var system = new FlappyDashSystem(Config(), DiveRules());
            system.Tick(atMin, Dt);
            system.Tick(atMax, Dt);
            Assert.AreEqual(rate * 0.4f * Dt, atMin.Get<FlappyDash>().Charge, 1e-6f);
            Assert.AreEqual(rate * Dt, atMax.Get<FlappyDash>().Charge, 1e-6f);
        }

        [Test]
        public void 대시_중엔_안_찬다()
        {
            var bird = Bird(11.25f);
            bird.Get<FlappyDash>().DashRemaining = 0.3f;
            new FlappyDashSystem(Config(), DiveRules()).Tick(bird, Dt);
            Assert.AreEqual(0f, bird.Get<FlappyDash>().Charge);
        }

        [Test]
        public void 다이브_맵은_출발_0이고_박으면_일정량만_잃되_찬_칸은_지킨다()
        {
            //  10-24 사람 판: 채우던 칸 전부는 아깝다 → 0.3칸만. 채우던 몫이 그보다 적으면 그만큼만(찬 칸은 지킨다).
            var system = new FlappyDashSystem(Config(), DiveRules());
            var bird = Bird(0f, charge: 0.6f);
            system.ResetForStart(bird);
            Assert.AreEqual(0f, bird.Get<FlappyDash>().Charge);

            foreach (var (before, after) in new[] { (1.7f, 1.4f), (1.2f, 1f), (0.2f, 0f), (2f, 2f), (0.65f, 0.35f) })
            {
                bird.Get<FlappyDash>().Charge = before;
                system.LoseOnStun(bird);
                Assert.AreEqual(after, bird.Get<FlappyDash>().Charge, 1e-5f, $"{before}에서 박음");
            }
        }

        [Test]
        public void 칸_경계_바로_아래에_멈추지_않는다()
        {
            //  한 틱 더하면 0.99995가 되는 자리에서 시작 — 맞추지 않으면 1에 못 미쳐 버튼이 안 나간다.
            var system = new FlappyDashSystem(Config(), DiveRules());
            var bird = Bird(11.25f, charge: 0.99995f - FlappyMapRulesField.DefaultDiveRate * Dt);
            system.Tick(bird, Dt);
            Assert.AreEqual(1f, bird.Get<FlappyDash>().Charge, 0f, "칸 경계에 맞춰야 한다");
            Assert.IsTrue(system.TryActivate(bird));
        }

        [Test]
        public void 스턴_중엔_속도가_남아_있어도_안_찬다()
        {
            //  박은 틱의 다음 틱: 대시 시스템이 스턴 새의 속도를 0으로 지우기 전에 지난 속도를 읽는다(재리뷰 10-23).
            var bird = Bird(11.25f, charge: 1f);
            bird.Add(new FlappyStun { StunRemaining = 1.0f });
            new FlappyDashSystem(Config(), DiveRules()).Tick(bird, Dt);
            Assert.AreEqual(1f, bird.Get<FlappyDash>().Charge, 0f);
        }

        [Test]
        public void 수동_대시가_꺼진_맵에선_다이브로_안_찬다()
        {
            //  버튼이 숨겨진 맵에서 게이지만 차면 쓸 수 없는 게이지가 된다.
            var rules = new FlappyMapRulesField();
            rules.Set(chaser: false, manualDash: false, diveCharge: true);
            var bird = Bird(11.25f);
            new FlappyDashSystem(Config(), rules).Tick(bird, Dt);
            Assert.AreEqual(0f, bird.Get<FlappyDash>().Charge);
        }

        [Test]
        public void 클래식_맵은_옛_다이브_충전_그대로다()
        {
            //  ManualDash만 켠 맵(새 규칙 둘 다 끔)은 옛 식 — 여기선 옛 문턱 23.2를 못 넘으니 기본 충전(0)만.
            var rules = new FlappyMapRulesField();
            rules.Set(chaser: false, manualDash: true);
            var bird = Bird(11.25f);
            new FlappyDashSystem(Config(), rules).Tick(bird, Dt);
            Assert.AreEqual(0f, bird.Get<FlappyDash>().Charge);

            //  옛 식이 살아 있는지: 문턱을 낮춘 설정이면 찬다.
            var legacy = new FlappyConfig(4.5f, 10.125f, 33.75f, 11.25f, 0.45f, 0.9f, 0.35f, 1.2f, 0.6f, 2.2f, 0.48f, 0f, 1.3f, dashChargeMinFall: 0f);
            var bird2 = Bird(11.25f);
            new FlappyDashSystem(legacy, rules).Tick(bird2, Dt);
            Assert.AreEqual(1.3f * Dt, bird2.Get<FlappyDash>().Charge, 1e-6f);
        }

        [Test]
        public void 클래식_맵은_박아도_안_잃는다()
        {
            var rules = new FlappyMapRulesField();
            rules.Set(chaser: false, manualDash: true);
            var bird = Bird(0f, charge: 1.7f);
            new FlappyDashSystem(Config(), rules).LoseOnStun(bird);
            Assert.AreEqual(1.7f, bird.Get<FlappyDash>().Charge, 1e-6f);
        }
    }
}
