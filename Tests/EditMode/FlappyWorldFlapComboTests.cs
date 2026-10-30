using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// 월드 안의 연속 날갯짓(spec 2026-10-27 §1·§3): 이동 커널이 실제로 쓰는 날갯짓만 세고, 박으면 0, 고비에서 게이지.
    /// 되감아 다시 굴려도 고비 보상은 한 시간선에 한 번이다 — 수와 게이지가 짝으로 되돌아가기 때문이다.
    /// 규칙은 광산과 같은 수동 대시 + 다이브 충전. 날갯짓을 이어 가면 떨어지지 않아 다이브로는 안 차므로, 게이지 변화는 고비 보상뿐이다.
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
        public void 스물다섯_번째_날갯짓에_게이지가_4분의1칸_오른다()
        {
            //  10-30부터 10도 0.1칸 — 25에서 4분의1칸이 더 보태진다.
            var world = Make(new FlappyWorldFixture.NeverHit(), out var bird);

            for (long t = 1; t <= 9; t++)
            {
                Step(world, bird, t, flap: true);
            }
            Assert.AreEqual(0f, Charge(bird), "9 — 고비 전");

            Step(world, bird, 10, flap: true);
            Assert.AreEqual(0.1f, Charge(bird), "10");

            for (long t = 11; t <= 24; t++)
            {
                Step(world, bird, t, flap: true);
            }
            Assert.AreEqual(0.1f, Charge(bird), "24 — 다음 고비 전");

            Step(world, bird, 25, flap: true);
            Assert.AreEqual(0.35f, Charge(bird), "25");

            for (long t = 26; t <= 49; t++)
            {
                Step(world, bird, t, flap: true);
            }
            Assert.AreEqual(0.35f, Charge(bird), "49 — 다음 고비 전");
        }

        [Test]
        public void 되감아_다시_굴려도_고비_보상은_한_번()
        {
            //  10-30부터 10도 0.1칸 — 30번째까지면 10(0.1)·25(0.25)가 겹쳐 0.35칸이다.
            var world = Make(new FlappyWorldFixture.NeverHit(), out var bird);
            for (long t = 1; t <= 30; t++)
            {
                Step(world, bird, t, flap: true);
            }
            int liveCombo = Combo(bird);
            float liveCharge = Charge(bird);
            Assert.AreEqual(30, liveCombo);
            Assert.AreEqual(0.35f, liveCharge);

            //  고비 앞(20) · 고비 틱(25) · 고비 뒤(27)에서 되감아 같은 입력으로 다시 굴린다.
            foreach (long anchor in new long[] { 20, 25, 27 })
            {
                Assert.IsTrue(world.LoadState(anchor), $"앵커 {anchor}");
                for (long t = anchor + 1; t <= 30; t++)
                {
                    Step(world, bird, t, flap: true);
                }
                Assert.AreEqual(liveCombo, Combo(bird), $"앵커 {anchor}: 수");
                Assert.AreEqual(liveCharge, Charge(bird), $"앵커 {anchor}: 고비를 다시 지나도 한 번만");
            }
        }

        [Test]
        public void 서버가_고비_전이면_재생에서_서버_수로_한_번_준다()
        {
            //  이 시험은 10을 지난 수(22)에서 시작해 25로 본다. 예측은 29번 다 받았다.
            //  서버는 한 번을 늦게 받아 23틱에 22번·게이지 0이다(보정이 덮는 값).
            var world = Make(new FlappyWorldFixture.NeverHit(), out var bird);
            for (long t = 1; t <= 29; t++)
            {
                Step(world, bird, t, flap: true);
            }

            Assert.IsTrue(world.LoadState(23));
            bird.Get<FlappyFlapCombo>().Count = 22;
            bird.Get<FlappyDash>().Charge = 0f;
            Step(world, bird, 24, flap: true);
            Step(world, bird, 25, flap: true);
            Assert.AreEqual(24, Combo(bird));
            Assert.AreEqual(0f, Charge(bird), "예측이 25틱에 받았던 ¼칸은 서버 수로는 아직이다");

            Step(world, bird, 26, flap: true);
            Assert.AreEqual(25, Combo(bird));
            Assert.AreEqual(0.25f, Charge(bird), "서버와 같은 날갯짓(26틱)에서 한 번");
        }

        [Test]
        public void 서버가_이미_줬으면_재생은_다시_안_준다()
        {
            //  이 시험은 10을 지난 수(26)에서 시작해 25로 본다.
            var world = Make(new FlappyWorldFixture.NeverHit(), out var bird);
            for (long t = 1; t <= 29; t++)
            {
                Step(world, bird, t, flap: true);
            }

            Assert.IsTrue(world.LoadState(27));
            bird.Get<FlappyFlapCombo>().Count = 26;     //  서버: 한 번 덜 받았지만 고비(25)는 이미 지났다
            bird.Get<FlappyDash>().Charge = 0.25f;
            Step(world, bird, 28, flap: true);
            Step(world, bird, 29, flap: true);

            Assert.AreEqual(28, Combo(bird));
            Assert.AreEqual(0.25f, Charge(bird), "0.5가 되면 두 번 준 것이다");
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
            Assert.AreEqual(0.35f, Charge(serverBird), "10이 0.1칸, 25가 4분의1칸 더해 0.35칸(10-30)");
            Assert.AreEqual(Combo(serverBird), Combo(clientBird));
            Assert.AreEqual(Charge(serverBird), Charge(clientBird));
            Assert.AreEqual(serverBird.Get<GameFramework.World.Transform>().Position,
                            clientBird.Get<GameFramework.World.Transform>().Position);
        }
    }
}
