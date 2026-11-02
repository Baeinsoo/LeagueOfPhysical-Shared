using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// 월드 안의 연속 날갯짓(spec 2026-10-27 §1·§3, 11-02 개정): 이동 커널이 실제로 쓰는 날갯짓만 세고, 박으면 0, 10마다 표의 칸만큼 게이지.
    /// 되감아 다시 굴려도 보상은 한 시간선에 한 번이다 — 수와 게이지가 짝으로 되돌아가기 때문이다.
    /// 규칙은 광산과 같은 수동 대시 + 다이브 충전. 날갯짓을 이어 가면 떨어지지 않아 다이브로는 안 차므로, 게이지 변화는 연속 보상뿐이다.
    /// </summary>
    public class FlappyWorldFlapComboTests
    {
        private const float Dt = 0.02f;

        private static FlappyMapRulesField Rules()
        {
            var rules = new FlappyMapRulesField();
            rules.Set(chaser: false, manualDash: true, diveCharge: true);
            return rules;
        }

        private static FlappyWorld Make(GameFramework.Physics.ICollisionQuery query, out Entity bird)
        {
            var world = FlappyWorldFixture.CreateWithCombo(query, Rules(), out bird);
            bird.Get<FlappyDash>().Charge = 0f;
            return world;
        }

        //  라이브 한 틱 = 입력 넣기 → 굴리기 → 사진(Reconciler 재생과 같은 모양).
        private static void Step(FlappyWorld world, Entity bird, long tick, bool flap)
        {
            bird.Get<InputBuffer>().Current = new InputCommand { Jump = flap };
            world.Tick(tick, Dt);
            world.SaveState(tick);
        }

        private static int Combo(Entity bird) => bird.Get<FlappyFlapCombo>().Count;

        private static float Charge(Entity bird) => bird.Get<FlappyDash>().Charge;

        private const float Eps = 1e-4f;

        [Test]
        public void 출발_뒤_시뮬이_받은_날갯짓만_센다()
        {
            var world = Make(new FlappyWorldFixture.NeverHit(), out var bird);
            world.GameplayStartTick = 4;

            for (long t = 1; t <= 6; t++)
            {
                Step(world, bird, t, flap: true);
            }

            Assert.AreEqual(3, Combo(bird), "출발 틱(4) 전 탭은 월드가 무시한다 — 4·5·6만");
        }

        [Test]
        public void 대시_중_날갯짓은_세지도_끊지도_않는다()
        {
            //  커널은 대시 중 날갯짓을 버린다(수평 직선). 세면 위험 없는 직선 동안 난타로 고비를 번다.
            var world = Make(new FlappyWorldFixture.NeverHit(), out var bird);
            bird.Get<FlappyFlapCombo>().Count = 7;
            bird.Get<FlappyDash>().DashRemaining = 0.2f;   //  10틱

            for (long t = 1; t <= 5; t++)
            {
                Step(world, bird, t, flap: true);
            }
            Assert.AreEqual(7, Combo(bird), "대시 중 — 그대로");

            for (long t = 6; t <= 20; t++)
            {
                Step(world, bird, t, flap: true);
            }
            Assert.Greater(Combo(bird), 7, "대시가 끝나면 이어서 센다(0으로 안 끊겼다)");
        }

        [Test]
        public void 결승_뒤_날갯짓은_안_센다()
        {
            var world = Make(new FlappyWorldFixture.NeverHit(), out var bird);
            bird.Add(new FinishState { FinishedTick = 0 });

            for (long t = 1; t <= 3; t++)
            {
                Step(world, bird, t, flap: true);
            }

            Assert.AreEqual(0, Combo(bird));
        }

        [Test]
        public void 박으면_0이고_기절_중_날갯짓은_안_센다()
        {
            var world = Make(new FlappyWorldFixture.AlwaysHit(), out var bird);
            bird.Get<FlappyFlapCombo>().Count = 5;

            Step(world, bird, 1, flap: true);
            Assert.Greater(bird.Get<FlappyStun>().StunRemaining, 0f, "맵에 닿아 기절했다");
            Assert.AreEqual(0, Combo(bird), "그 틱 날갯짓을 센 뒤 새로 기절해 끊겼다");

            for (long t = 2; t <= 10; t++)
            {
                Step(world, bird, t, flap: true);
            }
            Assert.AreEqual(0, Combo(bird), "기절 중엔 날갯짓을 안 받는다");
        }

        [Test]
        public void 열_번째마다_표의_칸만큼_게이지가_오른다()
        {
            //  11-02 — 10에 0.06, 20에 0.07.
            var world = Make(new FlappyWorldFixture.NeverHit(), out var bird);

            for (long t = 1; t <= 9; t++)
            {
                Step(world, bird, t, flap: true);
            }
            Assert.AreEqual(0f, Charge(bird), "9 — 첫 칸 전");

            Step(world, bird, 10, flap: true);
            Assert.AreEqual(0.06f, Charge(bird), Eps, "10");

            for (long t = 11; t <= 19; t++)
            {
                Step(world, bird, t, flap: true);
            }
            Assert.AreEqual(0.06f, Charge(bird), Eps, "19 — 다음 칸 전");

            Step(world, bird, 20, flap: true);
            Assert.AreEqual(0.13f, Charge(bird), Eps, "20");
        }

        [Test]
        public void 되감아_다시_굴려도_보상은_한_번()
        {
            //  30번째까지면 10(0.06)·20(0.07)·30(0.07)이 겹쳐 0.20칸이다.
            var world = Make(new FlappyWorldFixture.NeverHit(), out var bird);
            for (long t = 1; t <= 30; t++)
            {
                Step(world, bird, t, flap: true);
            }
            int liveCombo = Combo(bird);
            float liveCharge = Charge(bird);
            Assert.AreEqual(30, liveCombo);
            Assert.AreEqual(0.20f, liveCharge, Eps);

            //  보상 앞(19) · 보상 틱(20) · 보상 뒤(27)에서 되감아 같은 입력으로 다시 굴린다.
            foreach (long anchor in new long[] { 19, 20, 27 })
            {
                Assert.IsTrue(world.LoadState(anchor), $"앵커 {anchor}");
                for (long t = anchor + 1; t <= 30; t++)
                {
                    Step(world, bird, t, flap: true);
                }
                Assert.AreEqual(liveCombo, Combo(bird), $"앵커 {anchor}: 수");
                Assert.AreEqual(liveCharge, Charge(bird), $"앵커 {anchor}: 보상 자리를 다시 지나도 한 번만");
            }
        }

        [Test]
        public void 되감아_다시_굴려도_150_160_보상은_한_번씩()
        {
            //  표 끝(150, 0.23)과 꼬리(160, 0.25)도 수 하나에 묶여 있어 되감기에 한 번이다.
            var world = Make(new FlappyWorldFixture.NeverHit(), out var bird);
            bird.Get<FlappyFlapCombo>().Count = 140;
            for (long t = 1; t <= 20; t++)
            {
                Step(world, bird, t, flap: true);
            }
            Assert.AreEqual(160, Combo(bird));
            float liveCharge = Charge(bird);
            Assert.AreEqual(0.48f, liveCharge, Eps, "150에서 0.23, 160에서 0.25");

            foreach (long anchor in new long[] { 5, 10, 12 })
            {
                Assert.IsTrue(world.LoadState(anchor), $"앵커 {anchor}");
                for (long t = anchor + 1; t <= 20; t++)
                {
                    Step(world, bird, t, flap: true);
                }
                Assert.AreEqual(160, Combo(bird), $"앵커 {anchor}: 수");
                Assert.AreEqual(liveCharge, Charge(bird), $"앵커 {anchor}: 다시 지나도 한 번만");
            }
        }

        [Test]
        public void 서버가_보상_전이면_재생에서_서버_수로_한_번_준다()
        {
            //  예측은 29번 다 받아 20틱에 20번째 칸을 받았다. 서버는 23틱에 18번·게이지 0이다(보정이 덮는 값).
            var world = Make(new FlappyWorldFixture.NeverHit(), out var bird);
            for (long t = 1; t <= 29; t++)
            {
                Step(world, bird, t, flap: true);
            }

            Assert.IsTrue(world.LoadState(23));
            bird.Get<FlappyFlapCombo>().Count = 18;
            bird.Get<FlappyDash>().Charge = 0f;
            Step(world, bird, 24, flap: true);
            Assert.AreEqual(19, Combo(bird));
            Assert.AreEqual(0f, Charge(bird), "서버 수로는 아직 20 전");

            Step(world, bird, 25, flap: true);
            Assert.AreEqual(20, Combo(bird));
            Assert.AreEqual(0.07f, Charge(bird), "서버 수가 20이 되는 날갯짓에서 한 번");
        }

        [Test]
        public void 서버가_이미_줬으면_재생은_다시_안_준다()
        {
            var world = Make(new FlappyWorldFixture.NeverHit(), out var bird);
            for (long t = 1; t <= 29; t++)
            {
                Step(world, bird, t, flap: true);
            }

            Assert.IsTrue(world.LoadState(27));
            bird.Get<FlappyFlapCombo>().Count = 21;     //  서버: 덜 받았지만 20은 이미 지났다
            bird.Get<FlappyDash>().Charge = 0.13f;
            Step(world, bird, 28, flap: true);
            Step(world, bird, 29, flap: true);

            Assert.AreEqual(23, Combo(bird));
            Assert.AreEqual(0.13f, Charge(bird), "0.20이 되면 20을 두 번 준 것이다");
        }

        [Test]
        public void 자주_되감는_쪽도_곧게_돈_쪽과_수_게이지_자리가_같다()
        {
            //  서버는 곧게 굴리고, 클라는 다섯 틱마다 네 틱을 되감아 다시 굴린다(보정 재생과 같은 모양). 같은 입력이면 끝이 비트까지 같아야 한다.
            var server = Make(new FlappyWorldFixture.NeverHit(), out var serverBird);
            var client = Make(new FlappyWorldFixture.NeverHit(), out var clientBird);
            bool Flap(long t) => t % 3 == 0;

            for (long t = 1; t <= 120; t++)
            {
                Step(server, serverBird, t, Flap(t));
                Step(client, clientBird, t, Flap(t));
                if (t % 5 == 0 && t > 4)
                {
                    Assert.IsTrue(client.LoadState(t - 4));
                    for (long r = t - 3; r <= t; r++)
                    {
                        Step(client, clientBird, r, Flap(r));
                    }
                }
            }

            Assert.AreEqual(40, Combo(serverBird), "무언가 실제로 셌는지 — 120틱에 세 틱마다");
            Assert.AreEqual(0.28f, Charge(serverBird), Eps, "10·20·30·40 — 0.06+0.07+0.07+0.08(11-02)");
            Assert.AreEqual(Combo(serverBird), Combo(clientBird));
            Assert.AreEqual(Charge(serverBird), Charge(clientBird));
            Assert.AreEqual(serverBird.Get<GameFramework.World.Transform>().Position,
                            clientBird.Get<GameFramework.World.Transform>().Position);
        }
    }
}
