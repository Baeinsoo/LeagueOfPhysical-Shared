using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// 연속 날갯짓 세기와 고비 보상(spec 2026-10-27 §1). 시뮬이 받은 날갯짓마다 +1, 수가 고비 값이 되는 그 날갯짓에 게이지,
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

        [Test]
        public void 날갯짓마다_하나씩_센다()
        {
            var bird = Bird();

            Flaps(System(), bird, 3);

            Assert.AreEqual(3, bird.Get<FlappyFlapCombo>().Count);
        }

        [Test]
        public void 고비가_되는_날갯짓에만_칸을_준다()
        {
            var system = System();
            var bird = Bird();

            Flaps(system, bird, 9);
            Assert.AreEqual(0f, Charge(bird), "9");
            Flaps(system, bird, 1);
            Assert.AreEqual(0f, Charge(bird), "10 — 10-28부터 말만, 게이지는 없다");
            Flaps(system, bird, 14);
            Assert.AreEqual(0f, Charge(bird), "24");
            Flaps(system, bird, 1);
            Assert.AreEqual(0.25f, Charge(bird), "25");
            Flaps(system, bird, 25);
            Assert.AreEqual(0.75f, Charge(bird), "50");
            Flaps(system, bird, 50);
            Assert.AreEqual(1.25f, Charge(bird), "100 — 합 1.25칸, 2칸 상한 전");
        }

        [Test]
        public void 마지막_고비_뒤로는_더_주지_않는다()
        {
            var system = System();
            var bird = Bird();
            Flaps(system, bird, 100);
            bird.Get<FlappyDash>().Charge = 0f;

            Flaps(system, bird, 100);

            Assert.AreEqual(0f, Charge(bird));
            Assert.AreEqual(200, bird.Get<FlappyFlapCombo>().Count, "수는 계속 센다(기록용)");
        }

        [Test]
        public void 끊으면_0이고_다음_연속도_고비에서_또_준다()
        {
            //  10은 보상이 0이라(10-28) 이 시험은 25로 본다.
            var system = System();
            var bird = Bird();
            Flaps(system, bird, 25);
            Assert.AreEqual(0.25f, Charge(bird));

            system.Break(bird);
            Assert.AreEqual(0, bird.Get<FlappyFlapCombo>().Count);
            Flaps(system, bird, 25);

            Assert.AreEqual(0.5f, Charge(bird));
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
        public void 맵이_정한_고비를_따른다()
        {
            FlappyFlapComboRules.TryCreate(new[] { 3 }, new[] { 1f }, out var custom);
            var rules = new FlappyMapRulesField();
            rules.Set(chaser: false, manualDash: true, flapCombo: custom);
            var system = System(rules);
            var bird = Bird();

            Flaps(system, bird, 3);
            Assert.AreEqual(1f, Charge(bird), "3번째");
            Flaps(system, bird, 7);
            Assert.AreEqual(1f, Charge(bird), "기본 고비 10은 이 맵엔 없다");
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
