using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// 연속 날갯짓 세기와 보상(spec 2026-10-27 §1, 11-02 개정). 시뮬이 받은 날갯짓마다 +1, 수가 간격(10)의 배수가 되는 그 날갯짓에 표의 칸,
    /// 새로 기절하면 0. 게이지 상한은 2칸. 어느 날갯짓을 "받았나"는 월드가 가린다(<see cref="FlappyWorldFlapComboTests"/>).
    /// </summary>
    public class FlappyFlapComboSystemTests
    {
        private static Entity Bird(float charge = 0f)
        {
            var bird = new Entity("bird");
            bird.Add(new FlappyDash { Charge = charge });
            bird.Add(new FlappyFlapCombo());
            return bird;
        }

        //  규칙 없음(null)은 시험용 — 대시 시스템처럼 켜진 것으로 군다(기본 고비, 버튼 있음).
        private static FlappyFlapComboSystem System(FlappyMapRulesField rules = null)
            => new FlappyFlapComboSystem(new FlappyDashSystem(FlappyWorldFixture.Config(), rules), rules);

        private static void Flaps(FlappyFlapComboSystem system, Entity bird, int count)
        {
            for (int i = 0; i < count; i++)
            {
                system.Flap(bird);
            }
        }

        private static float Charge(Entity bird) => bird.Get<FlappyDash>().Charge;

        //  0.06+0.07처럼 2진수로 안 떨어지는 칸을 더하면 끝자리가 흔들린다.
        private const float Eps = 1e-4f;

        [Test]
        public void 날갯짓마다_하나씩_센다()
        {
            var bird = Bird();

            Flaps(System(), bird, 3);

            Assert.AreEqual(3, bird.Get<FlappyFlapCombo>().Count);
        }

        [Test]
        public void 간격마다_표의_칸을_준다()
        {
            //  11-02 — 10마다 커지는 고정 표(0.06·0.07·0.07·…). 누적 50→0.37, 100→0.97, 150→1.93.
            var system = System();
            var bird = Bird();

            Flaps(system, bird, 9);
            Assert.AreEqual(0f, Charge(bird), "9");
            Flaps(system, bird, 1);
            Assert.AreEqual(0.06f, Charge(bird), Eps, "10");
            Flaps(system, bird, 9);
            Assert.AreEqual(0.06f, Charge(bird), Eps, "19");
            Flaps(system, bird, 1);
            Assert.AreEqual(0.13f, Charge(bird), Eps, "20");
            Flaps(system, bird, 30);
            Assert.AreEqual(0.37f, Charge(bird), Eps, "50");
            Flaps(system, bird, 50);
            Assert.AreEqual(0.97f, Charge(bird), Eps, "100");
            Flaps(system, bird, 50);
            Assert.AreEqual(1.93f, Charge(bird), Eps, "150 — 2칸 상한 전");
        }

        [Test]
        public void 표_뒤로는_10마다_4분의1칸()
        {
            var system = System();
            var bird = Bird();
            Flaps(system, bird, 150);
            bird.Get<FlappyDash>().Charge = 0f;

            Flaps(system, bird, 9);
            Assert.AreEqual(0f, Charge(bird), "159");
            Flaps(system, bird, 1);
            Assert.AreEqual(0.25f, Charge(bird), Eps, "160");
            Flaps(system, bird, 40);
            Assert.AreEqual(1.25f, Charge(bird), Eps, "200");
            Assert.AreEqual(200, bird.Get<FlappyFlapCombo>().Count, "수는 계속 센다(기록용)");
        }

        [Test]
        public void 끊으면_0이고_다음_연속도_처음_칸부터_또_준다()
        {
            //  25까지 한 묶음은 10(0.06)·20(0.07) = 0.13칸이다.
            var system = System();
            var bird = Bird();
            Flaps(system, bird, 25);
            Assert.AreEqual(0.13f, Charge(bird), Eps);

            system.Break(bird);
            Assert.AreEqual(0, bird.Get<FlappyFlapCombo>().Count);
            Flaps(system, bird, 25);

            Assert.AreEqual(0.26f, Charge(bird), Eps);
        }

        [Test]
        public void 보상이_꺼진_맵은_세기만_한다()
        {
            var rules = new FlappyMapRulesField();
            rules.Set(chaser: false, manualDash: true, flapCombo: FlappyFlapComboRules.Off);
            var bird = Bird();

            Flaps(System(rules), bird, 10);

            Assert.AreEqual(10, bird.Get<FlappyFlapCombo>().Count);
            Assert.AreEqual(0f, Charge(bird));
        }

        [Test]
        public void 맵이_정한_간격을_따른다()
        {
            FlappyFlapComboRules.TryCreate(4, new[] { 0.5f }, 0.5f, out var custom);
            var rules = new FlappyMapRulesField();
            rules.Set(chaser: false, manualDash: true, flapCombo: custom);
            var system = System(rules);
            var bird = Bird();

            Flaps(system, bird, 4);
            Assert.AreEqual(0.5f, Charge(bird), "4번째");
            Flaps(system, bird, 6);
            Assert.AreEqual(1f, Charge(bird), "8에서 한 번 더, 기본 간격 10은 이 맵엔 없다");
        }

        [Test]
        public void 연속_칸이_없는_몸은_건너뛴다()
        {
            var rock = new Entity("rock");
            rock.Add(new FlappyDash());
            var system = System();

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    system.Flap(rock);
                }
                system.Break(rock);
            });
            Assert.AreEqual(FlappyDash.InitialCharge, rock.Get<FlappyDash>().Charge, "세지 않으니 고비도 없다");
        }
    }
}
