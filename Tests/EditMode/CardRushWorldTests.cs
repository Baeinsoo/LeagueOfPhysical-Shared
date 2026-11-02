using System.Collections.Generic;
using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    public class CardRushWorldTests
    {
        private const long StartTick = 300;
        private const float Dt = 0.02f;
        private static readonly CardRushTiming timing = new CardRushTiming(15, 30);

        private sealed class FixedSeed : IMatchSeed { public ulong Value => 4242; }

        private static CardRushMapRulesField Rules()
        {
            var field = new CardRushMapRulesField();
            field.Set(CardRushTheme.StoneBridge, 40, 2.2f, 2.6f, CardRushSections.Default, UnityEngine.Vector3.zero, UnityEngine.Vector3.forward);
            return field;
        }

        private static CardRushWorld Create(CardRushMapRulesField field, out Entity runner)
        {
            var registry = new EntityRegistry();
            runner = new Entity("runner-1");
            runner.Add(new Transform());
            runner.Add(new Simulated());
            runner.Add(new InputBuffer());
            runner.Add(new FinishState());
            runner.Add(new CardRushRunner { LaneOrigin = field.LaneOrigin(3) });
            registry.Add(runner);
            return new CardRushWorld(registry, new WorldEventBuffer(), field, new FixedSeed(), timing) { GameplayStartTick = StartTick };
        }

        private static CardRushRunnerState State(Entity e) => e.Get<CardRushRunner>().State;
        private static System.Numerics.Vector3 Pos(Entity e) => e.Get<Transform>().Position;

        private static void TickWith(CardRushWorld world, Entity runner, long tick, bool grab)
        {
            runner.Get<InputBuffer>().Current = new InputCommand { SequenceNumber = tick, Grab = grab };
            world.Tick(tick, Dt);
        }

        //  고정 씨앗 무작위 탭 — 틀림·맞음·뒷면 탭이 고루 섞인다. 너무 촘촘하면(12%) 멈춤이 풀리자마자 첫 색을 쳐서
        //  한 칸도 못 간다(연타가 느린 설계 그대로) — 5%면 600틱에 칸도 가고 틀리기도 한다.
        private static bool[] RandomTaps(int length)
        {
            var rng = new System.Random(1);
            var taps = new bool[length];
            for (int i = 0; i < length; i++) taps[i] = rng.NextDouble() < 0.05;
            return taps;
        }

        [Test]
        public void 출발선에_선다()
        {
            var field = Rules();
            var world = Create(field, out var runner);
            runner.Get<Transform>().Position = new System.Numerics.Vector3(50f, 50f, 50f);

            TickWith(world, runner, 100, grab: true);

            var origin = field.LaneOrigin(3);
            Assert.AreEqual(new System.Numerics.Vector3(origin.x, origin.y, origin.z), Pos(runner));
            Assert.AreEqual(default(CardRushRunnerState), State(runner), "출발 전 탭은 상태를 안 바꾼다");
            Assert.AreEqual(FinishState.NotFinished, runner.Get<FinishState>().FinishedTick);
            Assert.IsNotNull(world.Course);
        }

        [Test]
        public void 요구_색에_탭하면_한_칸_간다()
        {
            var field = Rules();
            var world = Create(field, out var runner);
            var origin = field.LaneOrigin(3);

            for (long t = StartTick; t < StartTick + 200; t++)
            {
                var s = State(runner);
                bool want = CardRushCard.TryColorAt(s, t, StartTick, world.Course, timing.MoveTicks, out var shown)
                            && shown == world.Course.RequiredColor(s.Step);
                TickWith(world, runner, t, want);

                var after = State(runner);
                float progress = CardRushLanes.Progress(after, t, timing.MoveTicks);
                Assert.AreEqual(origin.z + 2.2f * progress, Pos(runner).Z, 1e-4f, $"tick {t}");
                Assert.AreEqual(origin.x, Pos(runner).X, 1e-5f);
            }

            Assert.GreaterOrEqual(State(runner).Step, 1);
            Assert.AreEqual(0, State(runner).Misses, "요구 색에만 탭했다");
        }

        [Test]
        public void 규칙이_없으면_아무것도_안_한다()
        {
            var empty = new CardRushMapRulesField();
            var world = Create(empty, out var runner);
            var before = new System.Numerics.Vector3(1f, 2f, 3f);
            runner.Get<Transform>().Position = before;

            Assert.DoesNotThrow(() =>
            {
                for (long t = StartTick; t < StartTick + 100; t++) TickWith(world, runner, t, grab: true);
            });

            Assert.IsNull(world.Course);
            Assert.AreEqual(before, Pos(runner));
            Assert.AreEqual(default(CardRushRunnerState), State(runner));
        }

        [Test]
        public void 같은_입력이면_두_월드가_같다()
        {
            var taps = RandomTaps(600);
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
            Assert.Greater(State(ra).Misses, 0, "무작위 탭이 틀림을 한 번은 내야 시험이 뜻이 있다");
            Assert.Greater(State(ra).Step, 0, "무작위 탭이 한 칸은 가야 시험이 뜻이 있다");
        }

        [Test]
        public void 되감아_다시_돌리면_같은_상태()
        {
            var taps = RandomTaps(1000);

            //  먼저 한 번 돌려 틀림이 처음 나는 틱 w를 찾는다 — 저장 지점은 그 바로 앞 틱이다.
            //  그래야 복원이 StunEndTick을 빠뜨리면 재생의 w 탭이 "멈춤 중"으로 무시돼 갈린다.
            long w = -1;
            {
                var probe = Create(Rules(), out var pr);
                for (int i = 0; i < taps.Length && w < 0; i++)
                {
                    int missesBefore = State(pr).Misses;
                    TickWith(probe, pr, StartTick + i, taps[i]);
                    if (State(pr).Misses > missesBefore && State(pr).Step > 0) w = StartTick + i;
                }
            }
            Assert.Greater(w, StartTick, "입력열에 칸을 간 뒤의 틀림이 있어야 한다");

            var world = Create(Rules(), out var runner);
            long saveTick = w - 1;
            const int replayTicks = 60;
            for (long t = StartTick; t <= saveTick; t++)
            {
                TickWith(world, runner, t, taps[t - StartTick]);
                world.SaveState(t);
            }

            var atSave = State(runner);
            for (long t = saveTick + 1; t <= saveTick + replayTicks; t++)
            {
                TickWith(world, runner, t, taps[t - StartTick]);
                world.SaveState(t);
                if (t == w) Assert.AreEqual(atSave.Misses + 1, State(runner).Misses, "저장 바로 다음 틱이 틀림이어야 한다");
            }
            var liveState = State(runner);
            var livePos = Pos(runner);
            var liveFinish = runner.Get<FinishState>().FinishedTick;
            Assert.AreNotEqual(atSave, liveState, "저장 뒤 상태가 바뀌어야 복원을 시험할 수 있다");

            Assert.IsTrue(world.LoadState(saveTick));
            Assert.AreEqual(atSave, State(runner), "불러오면 저장 틱의 상태");
            Assert.IsTrue(world.TryGetSaved(saveTick, runner.Id, out var saved));
            Assert.AreEqual(atSave, saved);

            for (long t = saveTick + 1; t <= saveTick + replayTicks; t++)
            {
                TickWith(world, runner, t, taps[t - StartTick]);
                world.SaveState(t);
            }

            Assert.AreEqual(liveState, State(runner), "라이브 == 재생");
            Assert.AreEqual(livePos, Pos(runner));
            Assert.AreEqual(liveFinish, runner.Get<FinishState>().FinishedTick);
        }

        [Test]
        public void 불러오면_자리도_그_틱으로_돌아간다()
        {
            var field = Rules();
            var world = Create(field, out var runner);
            var r = runner.Get<CardRushRunner>();
            r.State = new CardRushRunnerState { Step = 5, MoveStartTick = 1000 };
            world.Tick(1005, Dt);
            world.SaveState(1005);
            var atSave = Pos(runner);

            r.State = new CardRushRunnerState { Step = 20, MoveStartTick = 1100 };
            runner.Get<Transform>().Position = new System.Numerics.Vector3(9f, 9f, 9f);

            Assert.IsTrue(world.LoadState(1005));
            Assert.AreEqual(atSave, Pos(runner));
            Assert.IsFalse(world.LoadState(999), "저장 없는 틱");
            Assert.IsFalse(world.TryGetSaved(999, runner.Id, out _));
        }

        [Test]
        public void 완주하면_결승_틱이_찍힌다()
        {
            var world = Create(Rules(), out var runner);
            var r = runner.Get<CardRushRunner>();
            r.State = new CardRushRunnerState { Step = 39, MoveStartTick = 1000 };

            //  첫 틱은 코스를 만들게 탭 없이 돈다.
            TickWith(world, runner, 1000, grab: false);
            long finishTick = -1;
            for (long t = 1001; t < 1300 && finishTick < 0; t++)
            {
                var s = State(runner);
                bool want = CardRushCard.TryColorAt(s, t, StartTick, world.Course, timing.MoveTicks, out var shown)
                            && shown == world.Course.RequiredColor(s.Step);
                TickWith(world, runner, t, want);
                if (want) finishTick = t;
            }

            Assert.AreEqual(40, State(runner).Step);
            Assert.AreEqual(finishTick, State(runner).MoveStartTick);
            Assert.AreEqual(State(runner).MoveStartTick, runner.Get<FinishState>().FinishedTick);
        }

        private sealed class LateSeed : IMatchSeed { public ulong Value { get; set; } }

        [Test]
        public void 씨앗이_늦게_오면_코스를_다시_만든다()
        {
            //  클라는 씨앗을 GameInfo로 늦게 받는다 — 받기 전(0)에 만든 코스로 판을 돌면 서버와 요구 색이 갈린다.
            var field = Rules();
            var seed = new LateSeed();
            var registry = new EntityRegistry();
            var world = new CardRushWorld(registry, new WorldEventBuffer(), field, seed, timing) { GameplayStartTick = StartTick };

            world.Tick(100, Dt);
            var early = world.Course;
            Assert.IsNotNull(early);

            seed.Value = 4242;
            world.Tick(101, Dt);
            Assert.AreNotSame(early, world.Course);
            var want = new CardRushCourse(4242, 40, CardRushSections.Default);
            for (int i = 0; i < 40; i++) Assert.AreEqual(want.RequiredColor(i), world.Course.RequiredColor(i), $"step {i}");

            var settled = world.Course;
            world.Tick(102, Dt);
            Assert.AreSame(settled, world.Course, "씨앗이 그대로면 다시 만들지 않는다");
        }

        [Test]
        public void 순위_진행은_판_기준_틱으로_잰다()
        {
            //  서버 절대 틱이 아주 커도(오래 켠 서버) 같은 칸의 이웃 틱이 갈려야 한다 — ForRanking은 13만 틱 안에서만 가른다.
            var world = Create(Rules(), out _);
            world.GameplayStartTick = 50_000_000;
            long horizon = world.GameplayStartTick + 6000;
            var early = new CardRushRunnerState { Step = 39, MoveStartTick = world.GameplayStartTick + 4999 };
            var late = new CardRushRunnerState { Step = 39, MoveStartTick = world.GameplayStartTick + 5000 };
            Assert.Greater(world.RankingProgress(early, horizon), world.RankingProgress(late, horizon));
            Assert.Greater(world.RankingProgress(new CardRushRunnerState { Step = 10, MoveStartTick = horizon }, horizon),
                           world.RankingProgress(new CardRushRunnerState { Step = 9, MoveStartTick = world.GameplayStartTick }, horizon));
            Assert.AreEqual(0.5f, world.RankingProgress(new CardRushRunnerState(), horizon), 1e-6f);
        }
    }
}
