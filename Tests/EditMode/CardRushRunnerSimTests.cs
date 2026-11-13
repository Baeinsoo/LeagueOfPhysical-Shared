using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LOP.Tests
{
    public class CardRushRunnerSimTests
    {
        private const long G = 100;
        private static CardRushTuning Tuning => CardRushTestTuning.Default;

        private static CardRushTrack Track(CardRushObstacle[] obstacles = null, CardRushZone[] zones = null, int trackMm = 140000, ulong seed = 7)
            => CardRushTrack.ForTests(trackMm, obstacles ?? new CardRushObstacle[0], zones ?? new CardRushZone[0], seed);

        private static CardRushObstacle Barricade(int at) => new CardRushObstacle(CardRushObstacleKind.Barricade, at, 0);
        private static CardRushObstacle Roller(int at) => new CardRushObstacle(CardRushObstacleKind.Roller, at, 0);
        private static CardRushObstacle Pit(int at, int width) => new CardRushObstacle(CardRushObstacleKind.Pit, at, 0, width);

        //  사거리 끝까지 가는 틱 수 — 8000 / 300을 올림.
        private const int ShotFlightTicks = (CardRushRules.ShotRangeMm + CardRushRules.ShotMmPerTick - 1) / CardRushRules.ShotMmPerTick;

        /// <summary>한 선수를 한 틱씩 민다. Now = 다음에 돌 틱.</summary>
        private sealed class Rig
        {
            public CardRushRunnerState S;
            public long Now = G;
            public int Lane;
            public readonly CardRushTrack Track;

            public Rig(CardRushTrack track, int pos = 0) { Track = track; S.PosMm = pos; }

            public void Tick(bool tap = false)
            {
                CardRushRunnerSim.Step(ref S, Now, tap, G, Lane, Track, Tuning);
                Now++;
            }

            public void Ticks(int n) { for (int i = 0; i < n; i++) Tick(); }

            /// <summary>
            /// 지난 틱에 그 색을 잡아 둔 상태로 만든다 — 시험이 쓰는 자리를 정확히 고르려고(잡는 틱의 이동 한 칸을 건너뛴다).
            /// 도는 기준을 옮기므로 쿨다운도 건너뛴다.
            /// </summary>
            public void Arm(CardColor color)
            {
                Assert.AreEqual(0, S.LockTick, "이미 잡은 카드가 있다");
                S.LockTick = Now - 1;
                int first = (int)Track.FirstCard(Lane, S.UseCount);
                int offset = ((int)color - first + CardColors.Count) % CardColors.Count;
                S.RollStartTick = S.LockTick - (long)offset * CardRushRules.CardDwellTicks;
                Assert.IsTrue(CardRushCard.CardAt(S, Now, G, Lane, Track, out var held));
                Assert.AreEqual(color, held);
            }

            public long Use(CardColor color)
            {
                Arm(color);
                long t = Now;
                Tick(true);
                Assert.AreEqual(0, S.LockTick, "썼어야 한다");
                return t;
            }

            /// <summary>지금 상대하는 굴러오는 것과의 거리(마지막으로 돈 틱 기준). 안 풀렸으면 int.MaxValue.</summary>
            public int RollerGap()
            {
                var o = Track.Obstacles[S.ObstacleIndex];
                if (S.ObstacleTick == 0) return int.MaxValue;
                return CardRushRunnerSim.RollerPos(o, S.ObstacleTick, Now - 1) - S.PosMm;
            }

            public void RunUntil(System.Func<Rig, bool> done, int max = 3000)
            {
                for (int i = 0; i < max && !done(this); i++) Tick();
                Assert.IsTrue(done(this), "조건에 닿지 못했다");
            }
        }

        // ── 달리기·대시 ──

        [Test]
        public void 탭이_없으면_틱마다_36mm_달린다()
        {
            var s = new CardRushRunnerState();
            var track = Track();
            for (long t = G; t < G + 10; t++) CardRushRunnerSim.Step(ref s, t, false, G, 0, track, Tuning);
            Assert.AreEqual(10 * CardRushRules.RunMmPerTick, s.PosMm);
        }

        [Test]
        public void 대시는_14틱에_3120mm()
        {
            var r = new Rig(Track(), pos: 1000);
            int before = r.S.PosMm;
            long used = r.Use(CardColor.Blue);
            Assert.AreEqual(used, r.S.DashStartTick);
            Assert.AreEqual(before + CardRushRules.DashOffset(1), r.S.PosMm, "쓴 틱부터 미끄러진다");
            r.Ticks(CardRushRules.DashTicks - 1);
            Assert.AreEqual(before + CardRushRules.DashMm, r.S.PosMm);
            Assert.AreEqual(0, r.S.DashStartTick, "14틱째에 끝난다");
            r.Tick();
            Assert.AreEqual(before + CardRushRules.DashMm + CardRushRules.RunMmPerTick, r.S.PosMm, "끝나면 다시 달린다");
        }

        [Test]
        public void 대시는_바리케이드에서_멈춘다()
        {
            var r = new Rig(Track(new[] { Barricade(5000) }), pos: 3000);
            long used = r.Use(CardColor.Blue);
            r.Ticks(CardRushRules.DashTicks);
            Assert.AreEqual(5000 - CardRushRules.BarricadeStopMm, r.S.PosMm);
            Assert.AreEqual(0, r.S.DashStartTick, "막히면 대시가 끝난다");
            Assert.Greater(r.S.ObstacleTick, used);
            Assert.AreEqual(0, r.S.ObstacleIndex, "대시로는 못 뚫는다");
        }

        // ── 바리케이드 ──

        [Test]
        public void 바리케이드에_닿고_기다림_뒤_올라가_그_틱에_다시_달린다()
        {
            int stop = 5000 - CardRushRules.BarricadeStopMm;
            var r = new Rig(Track(new[] { Barricade(5000) }), pos: stop - 10);
            r.Tick();
            long touched = r.Now - 1;
            Assert.AreEqual(touched, r.S.ObstacleTick);
            Assert.AreEqual(stop, r.S.PosMm);
            while (r.Now < touched + Tuning.BarricadeWaitTicks) r.Tick();
            Assert.AreEqual(stop, r.S.PosMm, "기다리는 동안 서 있다");
            Assert.AreEqual(0, r.S.ObstacleIndex);
            r.Tick();
            Assert.AreEqual(1, r.S.ObstacleIndex);
            Assert.AreEqual((int)CardRushObstacleEvent.Lifted, r.S.ObstacleEvent);
            Assert.AreEqual(touched + Tuning.BarricadeWaitTicks, r.S.ObstacleEventTick);
            Assert.AreEqual(stop + CardRushRules.RunMmPerTick, r.S.PosMm, "올라간 틱에 바로 달린다");
            Assert.AreEqual(1, r.S.CleanStreak);
        }

        // ── 투사체 ──

        [Test]
        public void 투사체는_틱마다_300mm_사거리_끝에서_꺼진다()
        {
            var r = new Rig(Track(), pos: 1000);
            long t0 = r.Use(CardColor.Red);
            Assert.AreEqual(t0, r.S.ShotTick);
            Assert.AreEqual(1000 + CardRushRules.ShotStartAheadMm, r.S.ShotFromMm, "손에서 떠나는 자리 = 쓴 틱의 몸 앞 0.3 m");
            Assert.AreEqual(1300 + 10 * CardRushRules.ShotMmPerTick, CardRushRunnerSim.ShotPos(r.S, t0 + 10));
            r.RunUntil(x => x.S.ShotTick == 0, 40);
            Assert.AreEqual(t0 + ShotFlightTicks, r.Now - 1);
            Assert.AreEqual(t0 + ShotFlightTicks, r.S.WhiffTick);
            Assert.AreEqual(1300, r.S.ShotFromMm, "꺼진 자리를 그리려고 출발 자리는 남긴다");
            Assert.AreEqual((int)CardRushObstacleEvent.None, r.S.ObstacleEvent, "헛방은 장애물 결과 칸에 안 적는다");
            Assert.AreEqual(1, r.S.UseCount, "헛방도 카드를 쓴 것");
        }

        [TestCase(1000)]
        [TestCase(3000)]
        [TestCase(CardRushRules.ShotRangeMm)]
        public void 사거리_안_바리케이드를_부수면_멈추지_않는다(int distance)
        {
            const int at = 30000;
            var r = new Rig(Track(new[] { Barricade(at) }), pos: at - CardRushRules.ShotStartAheadMm - distance);
            int before = r.S.PosMm;
            long t0 = r.Use(CardColor.Red);
            r.RunUntil(x => x.S.ObstacleEvent != 0, 40);
            long hit = t0 + (distance + CardRushRules.ShotMmPerTick - 1) / CardRushRules.ShotMmPerTick;
            Assert.AreEqual((int)CardRushObstacleEvent.Smashed, r.S.ObstacleEvent);
            Assert.AreEqual(hit, r.S.ObstacleEventTick);
            Assert.AreEqual(1, r.S.ObstacleIndex);
            Assert.AreEqual(0, r.S.ShotTick, "맞힌 투사체는 사라진다");
            Assert.AreEqual(0, r.S.WhiffTick);
            Assert.AreEqual(before + (hit - t0 + 1) * CardRushRules.RunMmPerTick, r.S.PosMm, "한 번도 안 멈췄다");
            Assert.AreEqual(1, r.S.CleanStreak);
        }

        [Test]
        public void 바리케이드_앞에_서서_던져도_부순다()
        {
            int stop = 5000 - CardRushRules.BarricadeStopMm;
            var r = new Rig(Track(new[] { Barricade(5000) }), pos: stop - 10);
            r.Tick();
            Assert.AreNotEqual(0, r.S.ObstacleTick);
            long t0 = r.Use(CardColor.Red);
            r.RunUntil(x => x.S.ObstacleEvent != 0, 5);
            Assert.AreEqual((int)CardRushObstacleEvent.Smashed, r.S.ObstacleEvent);
            Assert.AreEqual(t0 + 1, r.S.ObstacleEventTick, "출발 자리(앞 0.19 m) 다음 틱에 닿는다");
        }

        [Test]
        public void 사거리_밖_바리케이드는_헛방이고_번호_그대로()
        {
            const int at = 30000;
            var r = new Rig(Track(new[] { Barricade(at) }), pos: at - CardRushRules.ShotStartAheadMm - 8400);
            long t0 = r.Use(CardColor.Red);
            r.RunUntil(x => x.S.ShotTick == 0, 40);
            Assert.AreEqual(t0 + ShotFlightTicks, r.S.WhiffTick);
            Assert.AreEqual(0, r.S.ObstacleIndex);
            Assert.AreEqual((int)CardRushObstacleEvent.None, r.S.ObstacleEvent);
        }

        [Test]
        public void 서_있는_굴러오는_것도_부순다()
        {
            //  아직 안 풀린 굴러오는 것의 앞면 = 그 자리. 던지는 동안 몸이 풀리는 자리(7 m 앞)에 안 닿게 고른다.
            const int at = 20000, pos = 11800;
            var r = new Rig(Track(new[] { Roller(at) }), pos: pos);
            Assert.IsTrue(CardRushRunnerSim.TryFront(r.S, r.Now, r.Track, out int front));
            Assert.AreEqual(at, front);
            Assert.AreEqual(0, r.S.ObstacleTick);
            long t0 = r.Use(CardColor.Red);
            r.RunUntil(x => x.S.ObstacleEvent != 0, 40);
            Assert.AreEqual((int)CardRushObstacleEvent.Smashed, r.S.ObstacleEvent);
            int distance = at - (pos + CardRushRules.ShotStartAheadMm);
            Assert.AreEqual(t0 + (distance + CardRushRules.ShotMmPerTick - 1) / CardRushRules.ShotMmPerTick, r.S.ObstacleEventTick,
                "서 있는 자리로 맞았다(굴러오기 시작했다면 더 일찍 맞는다)");
            Assert.AreEqual(0, r.S.Hits);
            Assert.AreEqual(0, r.S.StunEndTick);
        }

        [Test]
        public void 굴러오는_것과_같은_틱이면_투사체가_이긴다()
        {
            //  이 틱: 몸 p + 36, 굴러오는 것 p + 300 → 264 mm(닿음). 투사체 출발 자리 p + 300 ≥ 앞면(맞음). 둘 다면 부숨이다.
            const int p = 13000;
            var r = new Rig(Track(new[] { Roller(p + CardRushRules.ShotStartAheadMm) }), pos: p);
            r.S.ObstacleTick = r.Now;
            long t0 = r.Use(CardColor.Red);
            Assert.AreEqual((int)CardRushObstacleEvent.Smashed, r.S.ObstacleEvent);
            Assert.AreEqual(t0, r.S.ObstacleEventTick);
            Assert.AreEqual(0, r.S.Hits);
            Assert.AreEqual(0, r.S.StunEndTick);
        }

        [Test]
        public void 투사체는_구덩이_위로_지나간다()
        {
            var r = new Rig(Track(new[] { Pit(16000, 1200) }), pos: 10000);
            Assert.IsFalse(CardRushRunnerSim.TryFront(r.S, r.Now, r.Track, out _), "구덩이엔 앞면이 없다");
            long t0 = r.Use(CardColor.Red);
            r.RunUntil(x => x.S.ShotTick == 0, 40);
            Assert.AreEqual(t0 + ShotFlightTicks, r.S.WhiffTick);
            Assert.AreEqual(0, r.S.ObstacleIndex);
            Assert.AreEqual((int)CardRushObstacleEvent.None, r.S.ObstacleEvent);
        }

        [Test]
        public void 다시_던지면_날던_투사체를_새것으로_바꾼다()
        {
            var r = new Rig(Track(), pos: 1000);
            long first = r.Use(CardColor.Red);
            r.Ticks(5);
            int pos = r.S.PosMm;
            long second = r.Use(CardColor.Red);
            Assert.AreEqual(second, r.S.ShotTick);
            Assert.AreEqual(pos + CardRushRules.ShotStartAheadMm, r.S.ShotFromMm);
            r.RunUntil(x => x.S.ShotTick == 0, 40);
            Assert.AreEqual(second + ShotFlightTicks, r.S.WhiffTick, "꺼짐은 새 투사체 기준");
            Assert.Greater(r.S.WhiffTick, first + ShotFlightTicks);
        }

        // ── 점프 ──

        [Test]
        public void 점프는_40틱에_2400mm_마지막_틱에_내려앉는다()
        {
            var r = new Rig(Track(), pos: 5000);
            long t0 = r.Use(CardColor.Yellow);
            Assert.AreEqual(t0, r.S.JumpStartTick);
            Assert.IsTrue(CardRushRunnerSim.IsAirborne(r.S));
            Assert.AreEqual(5000 + CardRushRules.JumpMmPerTick, r.S.PosMm, "쓴 틱부터 뛴다");
            r.Ticks(CardRushRules.JumpTicks - 2);
            Assert.AreEqual(5000 + CardRushRules.JumpMmPerTick * (CardRushRules.JumpTicks - 1), r.S.PosMm);
            Assert.IsTrue(CardRushRunnerSim.IsAirborne(r.S));
            r.Tick();
            Assert.AreEqual(5000 + 2400, r.S.PosMm);
            Assert.AreEqual(0, r.S.JumpStartTick, "40틱째에 내려앉는다");
            r.Tick();
            Assert.AreEqual(5000 + 2400 + CardRushRules.RunMmPerTick, r.S.PosMm, "다시 달린다");
        }

        [Test]
        public void 점프는_대시를_끊는다()
        {
            var r = new Rig(Track(), pos: 1000);
            r.Use(CardColor.Blue);
            int pos = r.S.PosMm;
            long jump = r.Use(CardColor.Yellow);
            Assert.AreEqual(0, r.S.DashStartTick);
            Assert.AreEqual(jump, r.S.JumpStartTick);
            Assert.AreEqual(pos + CardRushRules.JumpMmPerTick, r.S.PosMm);
        }

        //  뛰는 자리(쓴 틱 직전 자리)가 (먼 가장자리 − 2400, 앞 가장자리) 안이면 넘는다.
        [TestCase(1000, -1, CardRushObstacleEvent.Jumped)]
        [TestCase(1400, -1, CardRushObstacleEvent.Jumped)]
        [TestCase(1000, 1000 - 2400 + 1, CardRushObstacleEvent.Jumped)]
        [TestCase(1400, 1400 - 2400 + 1, CardRushObstacleEvent.Jumped)]
        [TestCase(1000, 1000 - 2400, CardRushObstacleEvent.Fell)]
        [TestCase(1400, 1400 - 2400, CardRushObstacleEvent.Fell)]
        [TestCase(1200, -2000, CardRushObstacleEvent.Fell)]
        public void 구덩이는_창_안에서_뛰면_넘고_밖이면_빠진다(int width, int jumpFromRelAt, CardRushObstacleEvent expected)
        {
            const int at = 20000;
            var r = new Rig(Track(new[] { Pit(at, width) }), pos: at + jumpFromRelAt);
            long t0 = r.Use(CardColor.Yellow);
            r.RunUntil(x => x.S.ObstacleEvent != 0, 60);
            Assert.AreEqual((int)expected, r.S.ObstacleEvent);
            if (expected == CardRushObstacleEvent.Jumped)
            {
                Assert.AreEqual(1, r.S.ObstacleIndex);
                Assert.AreEqual(0, r.S.Hits);
                Assert.AreEqual(1, r.S.CleanStreak);
            }
            else
            {
                Assert.AreEqual(t0 + CardRushRules.JumpTicks - 1, r.S.ObstacleEventTick, "내려앉은 틱에 빠진다");
                Assert.AreEqual(0, r.S.ObstacleIndex);
                Assert.AreEqual(1, r.S.Hits);
            }
        }

        [Test]
        public void 땅에서_구덩이에_들어가면_1_5m_뒤에서_기절_번호_그대로()
        {
            const int at = 20000;
            var r = new Rig(Track(new[] { Pit(at, 1200) }), pos: at - 50);
            r.Tick();
            Assert.AreEqual(0, r.S.ObstacleEvent, "아직 앞");
            r.Tick();
            long fell = r.Now - 1;
            Assert.AreEqual((int)CardRushObstacleEvent.Fell, r.S.ObstacleEvent);
            Assert.AreEqual(fell, r.S.ObstacleEventTick);
            Assert.AreEqual(at - CardRushRules.PitBackMm, r.S.PosMm);
            Assert.AreEqual(fell + Tuning.PitStunTicks, r.S.StunEndTick);
            Assert.AreEqual(0, r.S.ObstacleIndex, "다시 넘어야 한다");
            Assert.AreEqual(1, r.S.Hits);
            Assert.AreEqual(0, r.S.CleanStreak);

            while (r.Now < fell + Tuning.PitStunTicks) r.Tick();
            Assert.AreEqual(at - CardRushRules.PitBackMm, r.S.PosMm, "기절 중엔 서 있다");
            r.RunUntil(x => x.S.Hits == 2, 200);
            Assert.AreEqual((int)CardRushObstacleEvent.Fell, r.S.ObstacleEvent, "안 뛰면 또 빠진다");
        }

        [Test]
        public void 구덩이_자리가_0보다_앞이면_0에서_다시()
        {
            var r = new Rig(Track(new[] { Pit(1000, 1000) }), pos: 990);
            r.Tick();
            Assert.AreEqual((int)CardRushObstacleEvent.Fell, r.S.ObstacleEvent);
            Assert.AreEqual(0, r.S.PosMm);
        }

        [Test]
        public void 대시로_구덩이를_건너뛸_수_없다([Range(0, 3600, 50)] int before)
        {
            const int at = 20000;
            var r = new Rig(Track(new[] { Pit(at, CardRushRules.PitWidthMinMm) }), pos: at - 1 - before);
            r.Use(CardColor.Blue);
            r.RunUntil(x => x.S.ObstacleEvent != 0, 200);
            Assert.AreEqual((int)CardRushObstacleEvent.Fell, r.S.ObstacleEvent);
        }

        [Test]
        public void 바리케이드는_못_넘는다_부딪혀_내려앉는다()
        {
            const int at = 20000;
            int stop = at - CardRushRules.BarricadeStopMm;
            var r = new Rig(Track(new[] { Barricade(at) }), pos: stop - 1000);
            long t0 = r.Use(CardColor.Yellow);
            r.RunUntil(x => x.S.ObstacleTick != 0, 40);
            Assert.Less(r.S.ObstacleTick, t0 + CardRushRules.JumpTicks - 1, "공중에서 부딪혔다");
            Assert.AreEqual(stop, r.S.PosMm);
            Assert.AreEqual(0, r.S.JumpStartTick);
            Assert.AreEqual(0, r.S.ObstacleIndex);
            r.Tick();
            Assert.AreEqual(stop, r.S.PosMm, "바리케이드 앞에 서 있다");
        }

        [Test]
        public void 공중이면_굴러오는_것을_넘는다()
        {
            //  공중에선 몸 60 + 굴러오는 것 38 = 98 mm씩 가까워진다 — 열 틱째쯤 닿게 놓는다.
            const int p = 13000;
            var r = new Rig(Track(new[] { Roller(p + CardRushRules.RollerContactMm + 98 * 10) }), pos: p);
            r.S.ObstacleTick = r.Now;
            long t0 = r.Use(CardColor.Yellow);
            r.RunUntil(x => x.S.ObstacleEvent != 0, 40);
            Assert.AreEqual((int)CardRushObstacleEvent.Jumped, r.S.ObstacleEvent);
            Assert.Less(r.S.ObstacleEventTick, t0 + CardRushRules.JumpTicks - 1);
            Assert.AreEqual(1, r.S.ObstacleIndex);
            Assert.AreEqual(0, r.S.Hits);
            Assert.AreEqual(0, r.S.StunEndTick);
            Assert.AreEqual(1, r.S.CleanStreak);
        }

        [TestCase(CardColor.Yellow)]
        [TestCase(CardColor.Blue)]
        public void 공중에선_노랑_파랑_탭을_버리고_잡은_카드는_남는다(CardColor color)
        {
            var r = new Rig(Track(), pos: 5000);
            long jump = r.Use(CardColor.Yellow);
            r.Ticks(5);
            r.Arm(color);
            long held = r.S.LockTick;
            r.Tick(true);
            Assert.AreEqual(held, r.S.LockTick, "잡은 카드가 그대로 남는다");
            Assert.AreEqual(1, r.S.UseCount);
            Assert.AreEqual(jump, r.S.JumpStartTick, "공중 점프 없음");
            Assert.AreEqual(0, r.S.DashStartTick, "공중 대시 없음");

            r.RunUntil(x => !CardRushRunnerSim.IsAirborne(x.S), 40);
            long used = r.Now;
            r.Tick(true);
            Assert.AreEqual(0, r.S.LockTick);
            Assert.AreEqual(2, r.S.UseCount);
            if (color == CardColor.Blue) Assert.AreEqual(used, r.S.DashStartTick, "내려앉으면 쓴다");
            else Assert.AreEqual(used, r.S.JumpStartTick, "내려앉으면 쓴다");
        }

        [Test]
        public void 공중에서도_빨강은_던진다()
        {
            var r = new Rig(Track(), pos: 5000);
            r.Use(CardColor.Yellow);
            r.Ticks(5);
            int pos = r.S.PosMm;
            long thrown = r.Use(CardColor.Red);
            Assert.AreEqual(thrown, r.S.ShotTick);
            Assert.AreEqual(pos + CardRushRules.ShotStartAheadMm, r.S.ShotFromMm);
            Assert.IsTrue(CardRushRunnerSim.IsAirborne(r.S));
        }

        // ── 굴러오는 것 ──

        [Test]
        public void 굴러오는_것은_7m_앞에서_풀린다()
        {
            int reveal = 20000 - CardRushRules.RollerRevealMm;
            var r = new Rig(Track(new[] { Roller(20000) }), pos: reveal - 3 * CardRushRules.RunMmPerTick + 1);
            r.Ticks(2);
            Assert.AreEqual(0, r.S.ObstacleTick, "아직 7 m 밖");
            r.Tick();
            Assert.AreEqual(r.Now - 1, r.S.ObstacleTick);
            Assert.GreaterOrEqual(r.S.PosMm, reveal);
            r.Tick();
            Assert.IsTrue(CardRushRunnerSim.TryFront(r.S, r.Now - 1, r.Track, out int front));
            Assert.AreEqual(20000 - CardRushRules.RollerMmPerTick, front, "풀린 다음 틱에 38 mm 굴러왔다");
        }

        [Test]
        public void 땅에서_닿으면_꽈당_밀리고_기절()
        {
            var r = new Rig(Track(new[] { Roller(20000) }), pos: 13000);
            int prev = r.S.PosMm;
            while (r.S.ObstacleEvent == 0) { prev = r.S.PosMm; r.Tick(); }
            long hit = r.Now - 1;
            Assert.AreEqual((int)CardRushObstacleEvent.Tumbled, r.S.ObstacleEvent);
            Assert.AreEqual(hit, r.S.ObstacleEventTick);
            Assert.AreEqual(prev + CardRushRules.RunMmPerTick - Tuning.RollKnockMm, r.S.PosMm);
            Assert.AreEqual(hit + Tuning.RollStunTicks, r.S.StunEndTick);
            Assert.AreEqual(1, r.S.Hits);
            Assert.AreEqual(0, r.S.CleanStreak);
            Assert.AreEqual(1, r.S.ObstacleIndex);
            int knocked = r.S.PosMm;
            while (r.Now < hit + Tuning.RollStunTicks) r.Tick();
            Assert.AreEqual(knocked, r.S.PosMm, "기절 중엔 서 있다");
            r.Tick();
            Assert.AreEqual(knocked + CardRushRules.RunMmPerTick, r.S.PosMm);
        }

        [Test]
        public void 대시로_들이받아도_꽈당()
        {
            var r = new Rig(Track(new[] { Roller(20000) }), pos: 13000);
            r.RunUntil(x => x.RollerGap() <= 2000);
            r.Use(CardColor.Blue);
            r.RunUntil(x => x.S.ObstacleEvent != 0, 30);
            Assert.AreEqual((int)CardRushObstacleEvent.Tumbled, r.S.ObstacleEvent);
            Assert.AreEqual(0, r.S.DashStartTick, "꽈당이면 대시가 끝난다");
            Assert.AreEqual(1, r.S.Hits);
        }

        [Test]
        public void 대시로_굴러오는_것을_건너뛸_수_없다([Range(0, 3000, 50)] int extraGap)
        {
            var r = new Rig(Track(new[] { Roller(20000) }), pos: 13000);
            const int closing = CardRushRules.RunMmPerTick + CardRushRules.RollerMmPerTick;
            r.RunUntil(x => x.RollerGap() - closing <= CardRushRules.RollerContactMm + extraGap);
            Assert.AreEqual(0, r.S.ObstacleEvent, "대시 전엔 아직 안 닿았다");
            r.Use(CardColor.Blue);
            r.RunUntil(x => x.S.ObstacleEvent != 0, 200);
            Assert.AreEqual((int)CardRushObstacleEvent.Tumbled, r.S.ObstacleEvent);
        }

        // ── 구역 ──

        private static CardRushZone Zone(int trigger, int from, int to, int warn = 60) => new CardRushZone(trigger, from, to, warn);

        [Test]
        public void 구역은_트리거에_닿으면_켜진다()
        {
            var r = new Rig(Track(zones: new[] { Zone(10000, 11000, 14000) }), pos: 10000 - 2 * CardRushRules.RunMmPerTick + 1);
            r.Tick();
            Assert.AreEqual(0, r.S.ZoneTick);
            r.Tick();
            Assert.AreEqual(r.Now - 1, r.S.ZoneTick);
            Assert.AreEqual(0, r.S.ZoneIndex);
        }

        [Test]
        public void 구역_안에서_터지면_으악_밀리고_기절()
        {
            var r = new Rig(Track(zones: new[] { Zone(10000, 10500, 20000) }), pos: 9990);
            r.Tick();
            long on = r.S.ZoneTick;
            Assert.AreNotEqual(0, on);
            int prev = r.S.PosMm;
            while (r.S.ZoneEvent == 0) { prev = r.S.PosMm; r.Tick(); }
            long boom = r.Now - 1;
            Assert.AreEqual(on + 60, boom, "경고가 끝나는 틱에 터진다");
            Assert.AreEqual((int)CardRushZoneEvent.Hit, r.S.ZoneEvent);
            Assert.AreEqual(boom, r.S.ZoneEventTick);
            Assert.AreEqual(prev + CardRushRules.RunMmPerTick - Tuning.ZoneKnockMm, r.S.PosMm);
            Assert.AreEqual(boom + Tuning.ZoneStunTicks, r.S.StunEndTick);
            Assert.AreEqual(1, r.S.ZoneIndex);
            Assert.AreEqual(0, r.S.ZoneTick);
            Assert.AreEqual(1, r.S.Hits);
        }

        [Test]
        public void 대시로_빠져나가면_피함()
        {
            var r = new Rig(Track(zones: new[] { Zone(10000, 10500, 12000) }), pos: 9990);
            r.Tick();
            long on = r.S.ZoneTick;
            while (r.Now < on + 40) r.Tick();
            Assert.LessOrEqual(r.S.PosMm, 12000 + CardRushRules.ZoneMarginMm, "대시 전엔 아직 안");
            r.Use(CardColor.Blue);
            r.RunUntil(x => x.S.ZoneEvent != 0, 30);
            Assert.AreEqual((int)CardRushZoneEvent.Dodged, r.S.ZoneEvent);
            Assert.Greater(r.S.PosMm, 12000 + CardRushRules.ZoneMarginMm);
            Assert.AreEqual(0, r.S.Hits);
            Assert.AreEqual(1, r.S.CleanStreak);
        }

        [TestCase(0, CardRushZoneEvent.Dodged)]
        [TestCase(20, CardRushZoneEvent.Dodged)]
        [TestCase(40, CardRushZoneEvent.Dodged)]
        [TestCase(-1, CardRushZoneEvent.Hit)]
        public void 가장_짧은_경고_가장_긴_구역도_제때_대시하면_피한다(int dashAfterOn, CardRushZoneEvent expected)
        {
            //  4차엔 버팀이 없다 — 대시(또는 짧으면 달리기)로 빠져나가야 한다. -1 = 대시 안 함.
            const int trigger = 10000;
            int from = trigger + CardRushRules.ZoneAheadMaxMm;
            var z = Zone(trigger, from, from + CardRushRules.ZoneLengthMaxMm, CardRushRules.ZoneWarnEndTicks);
            var r = new Rig(Track(zones: new[] { z }), pos: trigger - 10);
            r.Tick();
            long on = r.S.ZoneTick;
            Assert.AreNotEqual(0, on);
            if (dashAfterOn >= 0)
            {
                while (r.Now < on + 1 + dashAfterOn) r.Tick();
                r.Use(CardColor.Blue);
            }
            r.RunUntil(x => x.S.ZoneEvent != 0, 100);
            Assert.AreEqual((int)expected, r.S.ZoneEvent);
        }

        [Test]
        public void 공중에서_구역에_맞으면_내려앉아_기절한다()
        {
            var r = new Rig(Track(zones: new[] { Zone(10000, 10500, 20000) }), pos: 9990);
            r.Tick();
            long boom = r.S.ZoneTick + 60;
            while (r.Now < boom - 10) r.Tick();
            r.Use(CardColor.Yellow);
            r.RunUntil(x => x.S.ZoneEvent != 0, 20);
            Assert.AreEqual((int)CardRushZoneEvent.Hit, r.S.ZoneEvent);
            Assert.AreEqual(0, r.S.JumpStartTick, "밀려나면 내려앉는다");
            int knocked = r.S.PosMm;
            r.Ticks(5);
            Assert.AreEqual(knocked, r.S.PosMm, "기절 중 공중으로 날아가지 않는다");
        }

        [Test]
        public void 구역은_한_번만_터진다()
        {
            var r = new Rig(Track(zones: new[] { Zone(10000, 10500, 20000) }), pos: 9990);
            r.RunUntil(x => x.S.ZoneEvent != 0);
            Assert.Less(r.S.PosMm, 10000, "밀려서 트리거 뒤로 왔다 — 다시 켜지면 안 된다");
            r.Ticks(300);
            Assert.AreEqual(1, r.S.ZoneIndex);
            Assert.AreEqual(0, r.S.ZoneTick);
            Assert.AreEqual(1, r.S.Hits);
        }

        // ── 끝·연속·경계 ──

        [Test]
        public void 완주_틱에_자리를_결승에_고정()
        {
            var r = new Rig(Track(trackMm: 40000), pos: 40000 - 10);
            r.Tick();
            Assert.AreEqual(40000, r.S.PosMm);
            Assert.AreEqual(r.Now - 1, r.S.FinishTick);
            var after = r.S;
            r.Tick(true);
            r.Ticks(10);
            Assert.AreEqual(after, r.S, "완주 뒤엔 아무것도 안 한다");
        }

        [Test]
        public void 무사_통과는_맞으면_0()
        {
            var r = new Rig(Track(new[] { Barricade(5000), Barricade(15000), Roller(30000) }), pos: 3500);
            r.Use(CardColor.Red);
            r.RunUntil(x => x.S.ObstacleIndex == 1, 20);
            Assert.AreEqual((int)CardRushObstacleEvent.Smashed, r.S.ObstacleEvent);
            Assert.AreEqual(1, r.S.CleanStreak);
            r.RunUntil(x => x.S.ObstacleIndex == 2);
            Assert.AreEqual((int)CardRushObstacleEvent.Lifted, r.S.ObstacleEvent);
            Assert.AreEqual(2, r.S.CleanStreak);
            r.RunUntil(x => x.S.ObstacleIndex == 3);
            Assert.AreEqual((int)CardRushObstacleEvent.Tumbled, r.S.ObstacleEvent);
            Assert.AreEqual(0, r.S.CleanStreak);
            Assert.AreEqual(2, r.S.BestCleanStreak);
            Assert.AreEqual(1, r.S.Hits);
        }

        [Test]
        public void 출발_전_틱0_완주_뒤엔_아무것도_안_한다()
        {
            var track = Track(new[] { Barricade(5000) });
            var s = new CardRushRunnerState { PosMm = 4800 };
            var before = s;
            CardRushRunnerSim.Step(ref s, G - 1, true, G, 0, track, Tuning);
            Assert.AreEqual(before, s, "출발 전");
            CardRushRunnerSim.Step(ref s, 0, true, 0, 0, track, Tuning);
            Assert.AreEqual(before, s, "틱 0");
            CardRushRunnerSim.Step(ref s, G, true, G, 0, null, Tuning);
            Assert.AreEqual(before, s, "코스 없음");
            var done = new CardRushRunnerState { PosMm = 140000, FinishTick = 900, LockTick = 880 };
            var doneBefore = done;
            CardRushRunnerSim.Step(ref done, 1000, true, G, 0, track, Tuning);
            Assert.AreEqual(doneBefore, done, "완주 뒤 — 잡은 카드도 안 사라진다");
        }

        [Test]
        public void 쿨다운_중_탭은_버린다_쌓지_않는다()
        {
            var r = new Rig(Track());
            long used = r.Use(CardColor.Blue);
            while (r.Now < used + CardRushRules.CooldownTicks) r.Tick(true);
            Assert.AreEqual(1, r.S.UseCount);
            Assert.AreEqual(0, r.S.RollStartTick, "쿨다운 중 탭은 시작도 안 한다");
            Assert.AreEqual(0, r.S.LockTick, "쿨다운 중 탭은 안 잡는다");
            r.Tick();
            Assert.AreEqual(0, r.S.RollStartTick, "쌓아 둔 탭이 쿨다운 끝에 나가지 않는다");
            r.Tick(true);
            Assert.AreEqual(r.Now - 1, r.S.RollStartTick, "쿨다운 뒤 첫 탭은 시작");
            Assert.AreEqual(0, r.S.LockTick);
            Assert.AreEqual(1, r.S.UseCount);
        }

        [Test]
        public void 막누르면_시작_잡기_쓰기를_틱마다_한_번씩()
        {
            var r = new Rig(Track(), pos: 1000);
            r.Tick(true);
            Assert.AreEqual(G, r.S.RollStartTick);
            Assert.AreEqual(0, r.S.LockTick);
            r.Tick(true);
            Assert.AreEqual(G + 1, r.S.LockTick);
            Assert.AreEqual(0, r.S.UseCount);
            r.Tick(true);
            Assert.AreEqual(0, r.S.LockTick);
            Assert.AreEqual(1, r.S.UseCount);
            Assert.AreEqual(G + 2 + CardRushRules.CooldownTicks, r.S.SpinStartTick);
        }

        // ── 성질 ──

        [Test]
        public void 노랑만_제때_써도_완주한다()
        {
            //  구덩이는 노랑으로만 넘는다 — 탭 없이는 영영 못 간다. 노랑만 잡아 구덩이·굴러오는 것 앞에서 쓰면 완주한다.
            int pitsSeen = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var track = new CardRushTrack(seed, Tuning);
                pitsSeen += track.Obstacles.Count(o => o.Kind == CardRushObstacleKind.Pit);
                var s = new CardRushRunnerState();
                for (long t = G; t < G + 30000 && s.FinishTick == 0; t++)
                    CardRushRunnerSim.Step(ref s, t, YellowOnly(s, t, track), G, 0, track, Tuning);
                Assert.AreNotEqual(0, s.FinishTick, $"seed {seed}: 완주 — 자리 {s.PosMm} 장애물 {s.ObstacleIndex}/{track.Obstacles.Count}");
                Assert.AreEqual(track.Obstacles.Count, s.ObstacleIndex, $"seed {seed}");
            }
            Assert.Greater(pitsSeen, 20, "구덩이가 실제로 깔려야 시험이 뜻이 있다");
        }

        [Test]
        public void 막누름이_읽는_것보다_느리다()
        {
            var mash = new List<long>();
            var read = new List<long>();
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var track = new CardRushTrack(seed, Tuning);
                mash.Add(FinishTicks(track, (in CardRushRunnerState s, long t) => true));
                read.Add(FinishTicks(track, (in CardRushRunnerState s, long t) => ReaderTaps(s, t, track)));
            }
            long mashMedian = Median(mash), readMedian = Median(read);
            Assert.Less(readMedian, mashMedian, $"읽기 중앙 {readMedian}틱, 막누름 중앙 {mashMedian}틱");
        }

        private delegate bool Policy(in CardRushRunnerState s, long tick);

        private static long FinishTicks(CardRushTrack track, Policy tap)
        {
            var s = new CardRushRunnerState();
            for (long t = G; t < G + 60000; t++)
            {
                CardRushRunnerSim.Step(ref s, t, tap(s, t), G, 0, track, Tuning);
                if (s.FinishTick != 0) return s.FinishTick - G;
            }
            Assert.Fail("완주 못 함");
            return 0;
        }

        private static long Median(List<long> xs)
        {
            var sorted = xs.OrderBy(x => x).ToList();
            return sorted[sorted.Count / 2];
        }

        //  뛰는 창의 가운데 — 공중 2.4 m가 구덩이를 양쪽 같은 여유로 덮는 자리.
        private static int PitJumpCenter(in CardRushObstacle pit)
            => pit.AtMm - (CardRushRules.JumpMmPerTick * CardRushRules.JumpTicks - pit.WidthMm) / 2;

        private static bool PitJumpNow(in CardRushRunnerState s, in CardRushObstacle pit)
            => s.PosMm >= PitJumpCenter(pit) - CardRushRules.RunMmPerTick && s.PosMm < pit.AtMm;

        //  굴러오는 것: 점프 반쯤에 닿을 때 뛴다.
        private static bool RollerJumpNow(in CardRushRunnerState s, long tick, in CardRushObstacle o)
            => s.ObstacleTick != 0
               && CardRushRunnerSim.RollerPos(o, s.ObstacleTick, tick) - s.PosMm - CardRushRules.RollerContactMm
                  <= (CardRushRules.JumpMmPerTick + CardRushRules.RollerMmPerTick) * CardRushRules.JumpTicks / 2;

        private static bool YellowOnly(in CardRushRunnerState s, long tick, CardRushTrack track)
        {
            if (CardRushCard.CanStart(s, tick, G)) return true;
            if (CardRushCard.CanLock(s, tick, G))
                return CardRushCard.CardAt(s, tick, G, 0, track, out var seen) && seen == CardColor.Yellow;
            if (!CardRushCard.CanUse(s, tick, G, 0, track) || s.ObstacleIndex >= track.Obstacles.Count) return false;
            var o = track.Obstacles[s.ObstacleIndex];
            if (o.Kind == CardRushObstacleKind.Pit) return PitJumpNow(s, o);
            if (o.Kind == CardRushObstacleKind.Roller) return RollerJumpNow(s, tick, o);
            return false;
        }

        /// <summary>
        /// 읽는 사람(반응 0): 필요한 색을 잡아 두고 쓸 때 쓴다 — 켜진 구역에 갇힐 것 같으면 파랑(바로), 바리케이드·굴러오는 것은 빨강(사거리 안),
        /// 구덩이는 노랑(창 가운데), 앞이 비면 파랑. 안 맞는 카드는 해가 없을 때 써서 비운다.
        /// </summary>
        private static bool ReaderTaps(in CardRushRunnerState s, long tick, CardRushTrack track)
        {
            CardColor? want = Want(s, tick, track, out bool useNow);
            if (CardRushCard.CanStart(s, tick, G)) return true;
            if (CardRushCard.CanLock(s, tick, G))
                return want != null && CardRushCard.CardAt(s, tick, G, 0, track, out var seen) && seen == want;
            if (!CardRushCard.CanUse(s, tick, G, 0, track) || !CardRushCard.CardAt(s, tick, G, 0, track, out var held)) return false;
            if (held == want) return useNow;
            return Harmless(held, s, tick, track);
        }

        private static CardColor? Want(in CardRushRunnerState s, long tick, CardRushTrack track, out bool useNow)
        {
            useNow = false;
            if (s.ZoneIndex < track.Zones.Count && s.ZoneTick != 0)
            {
                var z = track.Zones[s.ZoneIndex];
                long left = s.ZoneTick + z.WarnTicks - tick;
                int runAt = s.PosMm + (int)(CardRushRules.RunMmPerTick * left);
                if (runAt >= z.FromMm - CardRushRules.ZoneMarginMm && runAt <= z.ToMm + CardRushRules.ZoneMarginMm)
                {
                    useNow = true;
                    return CardColor.Blue;
                }
            }
            if (s.ObstacleIndex >= track.Obstacles.Count)
            {
                useNow = true;
                return CardColor.Blue;
            }
            var o = track.Obstacles[s.ObstacleIndex];
            if (o.Kind == CardRushObstacleKind.Pit)
            {
                if (o.AtMm - s.PosMm > CardRushRules.ShotRangeMm)
                {
                    useNow = o.AtMm - s.PosMm > CardRushRules.DashMm + 2400;
                    return CardColor.Blue;
                }
                useNow = PitJumpNow(s, o);
                return CardColor.Yellow;
            }
            CardRushRunnerSim.TryFront(s, tick, track, out int front);
            int shotGap = front - (s.PosMm + CardRushRules.ShotStartAheadMm);
            if (shotGap > CardRushRules.ShotRangeMm + 2000)
            {
                useNow = front - s.PosMm > CardRushRules.DashMm + 1400 + 2000;
                return CardColor.Blue;
            }
            useNow = shotGap <= CardRushRules.ShotRangeMm - 500;
            return CardColor.Red;
        }

        private static bool Harmless(CardColor held, in CardRushRunnerState s, long tick, CardRushTrack track)
        {
            if (held == CardColor.Red) return true;
            if (s.ZoneTick != 0) return false;
            if (s.ObstacleIndex >= track.Obstacles.Count) return true;
            var o = track.Obstacles[s.ObstacleIndex];
            int near = o.Kind == CardRushObstacleKind.Pit ? o.AtMm
                     : CardRushRunnerSim.TryFront(s, tick, track, out int front) ? front : o.AtMm;
            return near - s.PosMm > CardRushRules.DashMm + 2400;
        }

        [Test]
        public void 완주하면_날고_있던_투사체를_지운다()
        {
            var r = new Rig(Track(trackMm: 40000), pos: 40000 - 400);
            r.Use(CardColor.Red);
            Assert.AreNotEqual(0, r.S.ShotTick);
            r.RunUntil(x => x.S.FinishTick != 0, 40);
            Assert.AreEqual(0, r.S.ShotTick, "결승을 넘었으면 투사체는 없다");
            Assert.AreEqual(0, r.S.ShotFromMm);
        }
    }
}
