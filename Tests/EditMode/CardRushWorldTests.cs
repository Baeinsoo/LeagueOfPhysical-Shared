using System.Text.RegularExpressions;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace LOP.Tests
{
    public class CardRushWorldTests
    {
        private const long StartTick = 300;
        private const float Dt = 0.02f;
        private const int Lane = 3;

        //  spec §6.1 시작 값 — 마스터데이터가 바뀌어도 이 시험의 코스는 그대로다.
        private static readonly CardRushConfig config =
            new CardRushConfig(120, 4, 140, 11, 19, 0.5f, 28, 48, 1.2f, 1.39f, 0.7f, 2.77f, 1f, 0.22f, 0.40f, 0.20f, 0.02f, 0.5f);

        private sealed class FixedSeed : IMatchSeed { public ulong Value => 4242; }

        private static CardRushMapRulesField Rules(UnityEngine.Vector3 forward)
        {
            var field = new CardRushMapRulesField();
            field.Set(CardRushTheme.StoneBridge, 40, 2.2f, 2.6f, new UnityEngine.Vector3(5f, 0f, -3f), forward);
            return field;
        }

        private static CardRushMapRulesField Rules() => Rules(UnityEngine.Vector3.forward);

        private static CardRushWorld Create(CardRushMapRulesField field, out Entity runner, IMatchSeed seed = null)
        {
            var registry = new EntityRegistry();
            runner = new Entity("runner-1");
            runner.Add(new Transform());
            runner.Add(new Simulated());
            runner.Add(new InputBuffer());
            runner.Add(new FinishState());
            runner.Add(new CardRushRunner { LaneOrigin = field.LaneOrigin(Lane), Lane = Lane });
            registry.Add(runner);
            return new CardRushWorld(registry, new WorldEventBuffer(), field, seed ?? new FixedSeed(), config) { GameplayStartTick = StartTick };
        }

        private static CardRushRunnerState State(Entity e) => e.Get<CardRushRunner>().State;
        private static System.Numerics.Vector3 Pos(Entity e) => e.Get<Transform>().Position;

        private static void TickWith(CardRushWorld world, Entity runner, long tick, bool grab)
        {
            runner.Get<InputBuffer>().Current = new InputCommand { SequenceNumber = tick, Grab = grab };
            world.Tick(tick, Dt);
        }

        //  봇처럼 띄엄띄엄 누르는 고정 씨앗 탭 열 — 빨강·노랑·파랑이 고루 쓰이고 헛방도 난다.
        private static bool[] Taps(int length, int seed, double rate = 0.08)
        {
            var rng = new System.Random(seed);
            var taps = new bool[length];
            for (int i = 0; i < length; i++) taps[i] = rng.NextDouble() < rate;
            return taps;
        }

        [Test]
        public void 같은_입력이면_두_월드가_매_틱_같다()
        {
            var taps = Taps(600, 1);
            var a = Create(Rules(), out var ra);
            var b = Create(Rules(), out var rb);

            for (int i = 0; i < taps.Length; i++)
            {
                long t = StartTick + i;
                TickWith(a, ra, t, taps[i]);
                TickWith(b, rb, t, taps[i]);
                Assert.AreEqual(State(ra), State(rb), $"tick {t}");
                Assert.AreEqual(Pos(ra), Pos(rb), $"tick {t}");
            }
            var s = State(ra);
            Assert.Greater(s.PosMm, 0, "앞으로 가야 시험이 뜻이 있다");
            Assert.Greater(s.UseCount, 0, "카드를 써야 시험이 뜻이 있다");
            Assert.Greater(s.ObstacleIndex, 0, "장애물을 하나는 넘어야 시험이 뜻이 있다");
        }

        [Test]
        public void 라이브와_재생이_같다()
        {
            //  저장 지점 = 바리케이드에 닿은 뒤·올라가기 전. 그래야 복원이 ObstacleTick(닿은 틱)을 빠뜨리면 올라가는 틱이 갈린다.
            //  닿기 전에도 이 탭 열을 그대로 써서 카드·대시 상태가 섞여 있게 한다.
            var taps = Taps(2000, 7);
            var world = Create(Rules(), out var runner);
            world.Tick(StartTick - 1, Dt);   // 코스를 만든다
            var track = world.Track;
            Assert.IsNotNull(track);

            long saveTick = -1;
            for (long t = StartTick; t < StartTick + taps.Length && saveTick < 0; t++)
            {
                TickWith(world, runner, t, taps[t - StartTick]);
                world.SaveState(t);
                var s = State(runner);
                if (s.ObstacleTick != 0 && s.ObstacleIndex < track.Obstacles.Count
                    && track.Obstacles[s.ObstacleIndex].Kind == CardRushObstacleKind.Barricade)
                {
                    saveTick = t;
                }
            }
            Assert.Greater(saveTick, StartTick, "탭 열이 바리케이드에 닿아야 한다");
            var atSave = State(runner);
            int barricade = atSave.ObstacleIndex;
            Assert.Greater(atSave.UseCount, 0, "저장 지점에 카드 쓴 기록이 있어야 복원을 시험한다");

            //  닿은 뒤엔 올라갈 때까지 안 누른다(빨강으로 부수면 닿은 틱이 판정에서 빠진다) — 그 뒤엔 다시 섞어 누른다.
            const int replayTicks = 120;
            bool[] after = new bool[replayTicks + 1];
            for (int i = 1; i <= replayTicks; i++)
                after[i] = i > config.Tuning.BarricadeWaitTicks && taps[(saveTick - StartTick + i) % taps.Length];
            for (int i = 1; i <= replayTicks; i++)
            {
                TickWith(world, runner, saveTick + i, after[i]);
                world.SaveState(saveTick + i);
            }
            var live = State(runner);
            var livePos = Pos(runner);
            Assert.Greater(live.ObstacleIndex, barricade, "저장 뒤에 바리케이드를 넘어야 한다");
            Assert.Greater(live.UseCount, atSave.UseCount, "저장 뒤에도 카드를 써야 한다");

            Assert.IsTrue(world.LoadState(saveTick));
            Assert.AreEqual(atSave, State(runner), "불러오면 저장 틱의 상태");
            Assert.IsTrue(world.TryGetSaved(saveTick, runner.Id, out var saved));
            Assert.AreEqual(atSave, saved);

            for (int i = 1; i <= replayTicks; i++)
            {
                TickWith(world, runner, saveTick + i, after[i]);
                world.SaveState(saveTick + i);
            }
            Assert.AreEqual(live, State(runner), "라이브 == 재생");
            Assert.AreEqual(livePos, Pos(runner));
        }

        [Test]
        public void 마커가_없으면_아무것도_안_한다()
        {
            var world = Create(new CardRushMapRulesField(), out var runner);
            var before = new System.Numerics.Vector3(1f, 2f, 3f);
            runner.Get<Transform>().Position = before;

            Assert.DoesNotThrow(() =>
            {
                for (long t = StartTick; t < StartTick + 100; t++) TickWith(world, runner, t, grab: true);
            });

            Assert.IsNull(world.Track);
            Assert.AreEqual(before, Pos(runner));
            Assert.AreEqual(default(CardRushRunnerState), State(runner));
        }

        [Test]
        public void 자리는_레인_출발점_더하기_앞으로_PosMm()
        {
            //  비스듬하고 높이가 섞인 앞 방향 — 바닥에 눕힌 방향으로 간다.
            var forward = new UnityEngine.Vector3(1f, 0.5f, 1f);
            var field = Rules(forward);
            var world = Create(field, out var runner);
            var origin = field.LaneOrigin(Lane);
            var flat = new UnityEngine.Vector3(1f, 0f, 1f).normalized;

            TickWith(world, runner, StartTick - 1, grab: false);
            Assert.AreEqual(new System.Numerics.Vector3(origin.x, origin.y, origin.z), Pos(runner), "출발 전엔 출발선");

            for (long t = StartTick; t < StartTick + 50; t++)
            {
                TickWith(world, runner, t, grab: false);
                int pos = State(runner).PosMm;
                var want = origin + flat * (pos / 1000f);
                Assert.AreEqual(want.x, Pos(runner).X, 1e-4f, $"tick {t}");
                Assert.AreEqual(want.y, Pos(runner).Y, 1e-4f, $"tick {t}");
                Assert.AreEqual(want.z, Pos(runner).Z, 1e-4f, $"tick {t}");
            }
            Assert.AreEqual(50 * CardRushRules.RunMmPerTick, State(runner).PosMm, "첫 장애물 전엔 한 틱에 36 mm");
        }

        [Test]
        public void 완주하면_FinishState에_완주_틱()
        {
            var world = Create(Rules(), out var runner);
            world.Tick(StartTick - 1, Dt);
            var track = world.Track;
            var r = runner.Get<CardRushRunner>();
            //  장애물·구역을 다 넘긴 결승 100 mm 앞 — 36 mm씩 세 틱째에 들어온다.
            r.State = new CardRushRunnerState
            {
                PosMm = track.TrackMm - 100, ObstacleIndex = track.Obstacles.Count, ZoneIndex = track.Zones.Count,
            };

            TickWith(world, runner, StartTick, false);
            TickWith(world, runner, StartTick + 1, false);
            Assert.AreEqual(FinishState.NotFinished, runner.Get<FinishState>().FinishedTick);
            TickWith(world, runner, StartTick + 2, false);

            Assert.AreEqual(StartTick + 2, State(runner).FinishTick);
            Assert.AreEqual(StartTick + 2, runner.Get<FinishState>().FinishedTick);
            Assert.AreEqual(track.TrackMm, State(runner).PosMm);

            //  되감아 완주 전으로 가면 결승 틱도 지워진다.
            r.State = new CardRushRunnerState { PosMm = 10 };
            world.Tick(StartTick + 3, Dt);
            Assert.AreEqual(FinishState.NotFinished, runner.Get<FinishState>().FinishedTick);
        }

        [Test]
        public void 코스_지문을_로그로_찍는다()
        {
            var world = Create(Rules(), out _);
            LogAssert.Expect(UnityEngine.LogType.Log, new Regex("코스 지문"));
            world.Tick(StartTick, Dt);
            var want = new CardRushTrack(4242, config.Tuning);
            Assert.AreEqual(want.Fingerprint, world.Track.Fingerprint);
        }

        [Test]
        public void 불러오면_자리도_그_틱으로_돌아간다()
        {
            var world = Create(Rules(), out var runner);
            var r = runner.Get<CardRushRunner>();
            world.Tick(StartTick - 1, Dt);
            r.State = new CardRushRunnerState { PosMm = 5000 };
            world.SaveState(1005);
            Assert.IsTrue(world.LoadState(1005));
            var atSave = Pos(runner);

            r.State = new CardRushRunnerState { PosMm = 20000 };
            runner.Get<Transform>().Position = new System.Numerics.Vector3(9f, 9f, 9f);

            Assert.IsTrue(world.LoadState(1005));
            Assert.AreEqual(atSave, Pos(runner));
            Assert.AreEqual(5000, State(runner).PosMm);
            var origin = Rules().LaneOrigin(Lane);
            Assert.AreEqual(origin.z + 5f, Pos(runner).Z, 1e-4f);
            Assert.IsFalse(world.LoadState(999), "저장 없는 틱");
            Assert.IsFalse(world.TryGetSaved(999, runner.Id, out _));
        }

        private sealed class LateSeed : IMatchSeed { public ulong Value { get; set; } }

        [Test]
        public void 씨앗이_늦게_오면_코스를_다시_만든다()
        {
            //  클라는 씨앗을 GameInfo로 늦게 받는다 — 받기 전(0)에 만든 코스로 판을 돌면 서버와 장애물이 갈린다.
            var seed = new LateSeed();
            var world = Create(Rules(), out _, seed);

            world.Tick(100, Dt);
            var early = world.Track;
            Assert.IsNotNull(early);

            seed.Value = 4242;
            world.Tick(101, Dt);
            Assert.AreNotSame(early, world.Track);
            Assert.AreEqual(new CardRushTrack(4242, config.Tuning).Fingerprint, world.Track.Fingerprint);

            var settled = world.Track;
            world.Tick(102, Dt);
            Assert.AreSame(settled, world.Track, "씨앗이 그대로면 다시 만들지 않는다");
        }

        [Test]
        public void 출발_뒤엔_씨앗이_바뀌어도_코스를_안_바꾼다()
        {
            //  판 도중에 코스를 갈아 끼우면 선수 상태(장애물·구역 번호)는 옛 코스 기준이라 판정이 엉킨다 — 크게 알리고 옛 코스를 지킨다.
            var seed = new LateSeed { Value = 4242 };
            var world = Create(Rules(), out _, seed);
            world.Tick(StartTick, Dt);
            var track = world.Track;

            seed.Value = 777;
            LogAssert.Expect(UnityEngine.LogType.Error, new Regex("CardRushWorld"));
            world.Tick(StartTick + 1, Dt);
            world.Tick(StartTick + 2, Dt);   // 한 번만 알린다 — 두 번째 로그가 있으면 LogAssert가 실패시킨다
            Assert.AreSame(track, world.Track);
        }

        [Test]
        public void 씨앗이_오기_전엔_지문을_안_찍는다()
        {
            //  클라는 씨앗 0으로 먼저 코스를 만든다 — 그 지문까지 찍으면 서버 로그와 맞대 볼 때 헷갈린다. 진짜 씨앗일 때 한 번만.
            var seed = new LateSeed();
            var world = Create(Rules(), out _, seed);
            int logs = 0;
            UnityEngine.Application.LogCallback count = (message, _, type) =>
            {
                if (type == UnityEngine.LogType.Log && message.Contains("코스 지문")) logs++;
            };
            UnityEngine.Application.logMessageReceived += count;
            try
            {
                world.Tick(100, Dt);
                world.Tick(101, Dt);
                Assert.AreEqual(0, logs, "씨앗 0");
                seed.Value = 4242;
                world.Tick(102, Dt);
                world.Tick(103, Dt);
                Assert.AreEqual(1, logs, "진짜 씨앗에서 한 번");
            }
            finally
            {
                UnityEngine.Application.logMessageReceived -= count;
            }
        }

        [Test]
        public void 규칙보다_먼저_생긴_몸은_규칙이_오면_레인을_다시_구한다()
        {
            //  맵이 몸보다 늦은 비정상 순서 — 받은 자리를 출발점으로 쓴 몸이 레인 0으로 굳으면 첫 장이 서버와 영영 갈린다.
            var field = new CardRushMapRulesField();
            var world = Create(field, out var runner);
            var r = runner.Get<CardRushRunner>();
            var real = Rules();
            var received = real.LaneOrigin(5) + UnityEngine.Vector3.forward * 30f;
            r.Lane = 0;
            r.LaneOrigin = received;
            r.LanePending = true;

            world.Tick(StartTick - 2, Dt);
            Assert.IsTrue(r.LanePending, "규칙이 없으면 그대로 기다린다");

            field.Set(CardRushTheme.StoneBridge, 40, 2.2f, 2.6f, new UnityEngine.Vector3(5f, 0f, -3f), UnityEngine.Vector3.forward);
            world.Tick(StartTick - 1, Dt);
            Assert.AreEqual(5, r.Lane);
            Assert.AreEqual(real.LaneOrigin(5), r.LaneOrigin);
            Assert.IsFalse(r.LanePending);
        }

        [Test]
        public void 순위_진행은_간_거리()
        {
            var world = Create(Rules(), out _);
            Assert.AreEqual(12345f, world.RankingProgress(new CardRushRunnerState { PosMm = 12345 }));
            Assert.Greater(world.RankingProgress(new CardRushRunnerState { PosMm = 189_999 }),
                           world.RankingProgress(new CardRushRunnerState { PosMm = 189_998 }), "190 m 안에선 1 mm도 가른다");
        }
    }
}
