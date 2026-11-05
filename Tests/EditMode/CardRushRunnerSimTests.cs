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

            /// <summary>지금 틱에 그 색이 보이게 카드 도는 기준을 맞춘다(쿨다운이 끝난 상태에서만 — 시험이 자리를 정확히 고르려고).</summary>
            public void ShowNow(CardColor color)
            {
                Assert.IsTrue(CardRushCard.CanUse(S, Now, G), "쓸 수 있는 틱이어야 한다");
                int first = (int)Track.FirstCard(Lane, S.UseCount);
                int offset = ((int)color - first + CardColors.Count) % CardColors.Count;
                S.SpinStartTick = Now - (long)offset * CardRushRules.CardDwellTicks;
                Assert.IsTrue(CardRushCard.TryCardAt(S, Now, G, Lane, Track, out var shown));
                Assert.AreEqual(color, shown);
            }

            public long Use(CardColor color)
            {
                ShowNow(color);
                long t = Now;
                Tick(true);
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

        /// <summary>그 색이 처음 보이고 쓸 수 있는 틱(from 이후).</summary>
        private static long TickShowing(in CardRushRunnerState s, CardRushTrack track, int lane, CardColor color, long from)
        {
            for (long t = from; t < from + 500; t++)
                if (CardRushCard.CanUse(s, t, G) && CardRushCard.TryCardAt(s, t, G, lane, track, out var c) && c == color) return t;
            return -1;
        }

        // ── 달리기·대시 ──

        [Test]
        public void 탭이_없으면_틱마다_36mm_달린다()
        {
            var s = new CardRushRunnerState();
            var track = Track(obstacles: new CardRushObstacle[0], zones: new CardRushZone[0]);
            for (long t = G; t < G + 10; t++) CardRushRunnerSim.Step(ref s, t, false, G, 0, track, Tuning);
            Assert.AreEqual(10 * CardRushRules.RunMmPerTick, s.PosMm);
        }

        [Test]
        public void 대시는_14틱에_3120mm()
        {
            var r = new Rig(Track());
            long blue = TickShowing(r.S, r.Track, r.Lane, CardColor.Blue, G);
            while (r.Now < blue) r.Tick();
            int before = r.S.PosMm;
            r.Tick(true);
            Assert.AreEqual(blue, r.S.DashStartTick);
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
            Assert.AreNotEqual(0, r.S.ObstacleTick, "닿았다");
            Assert.Greater(r.S.ObstacleTick, used);
            Assert.AreEqual(0, r.S.ObstacleIndex, "대시로는 못 뚫는다");
        }

        // ── 바리케이드 ──

        [Test]
        public void 바리케이드에_닿고_60틱_뒤_올라가_그_틱에_다시_달린다()
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

        [TestCase(1500)]
        [TestCase(CardRushRules.SmashReachMm)]
        [TestCase(-CardRushRules.SmashBackMm + 1)]
        public void 손_닿는_바리케이드를_빨강으로_치우면_멈추지_않는다(int distance)
        {
            var r = new Rig(Track(new[] { Barricade(5000) }), pos: 5000 - distance);
            int before = r.S.PosMm;
            long used = r.Use(CardColor.Red);
            Assert.AreEqual(1, r.S.ObstacleIndex);
            Assert.AreEqual((int)CardRushObstacleEvent.Smashed, r.S.ObstacleEvent);
            Assert.AreEqual(used, r.S.ObstacleEventTick);
            Assert.AreEqual(before + CardRushRules.RunMmPerTick, r.S.PosMm);
            r.Ticks(50);
            Assert.AreEqual(before + 51 * CardRushRules.RunMmPerTick, r.S.PosMm, "바리케이드 자리를 그냥 지나간다");
        }

        [TestCase(CardRushRules.SmashReachMm + 1)]
        [TestCase(3000)]
        public void 손_밖_빨강은_헛방이고_번호_그대로(int distance)
        {
            var r = new Rig(Track(new[] { Barricade(9000) }), pos: 9000 - distance);
            long used = r.Use(CardColor.Red);
            Assert.AreEqual(0, r.S.ObstacleIndex);
            Assert.AreEqual((int)CardRushObstacleEvent.Whiff, r.S.ObstacleEvent);
            Assert.AreEqual(used, r.S.ObstacleEventTick);
            Assert.AreEqual(1, r.S.UseCount, "헛방도 카드를 쓴 것");
            Assert.AreEqual(used + CardRushRules.CooldownTicks, r.S.SpinStartTick);
            Assert.AreEqual(0, r.S.CleanStreak);
        }

        // ── 굴러오는 것 ──

        [Test]
        public void 굴러오는_것은_7m_앞에서_풀린다()
        {
            int reveal = 20000 - CardRushRules.RollerRevealMm;
            var r = new Rig(Track(new[] { Roller(20000) }), pos: reveal - 3 * CardRushRules.RunMmPerTick + 1);
            r.Ticks(2);
            Assert.AreEqual(0, r.S.ObstacleTick, "아직 7 m 밖");
            Assert.IsFalse(CardRushRunnerSim.TryFront(r.S, r.Now, r.Track, out _), "풀리기 전엔 손이 안 닿는다");
            r.Tick();
            Assert.AreEqual(r.Now - 1, r.S.ObstacleTick);
            Assert.GreaterOrEqual(r.S.PosMm, reveal);
            r.Tick();
            Assert.IsTrue(CardRushRunnerSim.TryFront(r.S, r.Now - 1, r.Track, out int front));
            Assert.AreEqual(20000 - CardRushRules.RollerMmPerTick, front, "풀린 다음 틱에 38 mm 굴러왔다");
        }

        [Test]
        public void 방패면_굴러오는_것을_막는다_그동안_서_있다()
        {
            var r = new Rig(Track(new[] { Roller(20000) }), pos: 13000);
            r.RunUntil(x => x.RollerGap() <= 1000);
            int pos = r.S.PosMm;
            long used = r.Use(CardColor.Yellow);
            Assert.AreEqual(pos, r.S.PosMm, "방패를 든 틱부터 선다");
            r.RunUntil(x => x.S.ObstacleEvent != 0, 60);
            Assert.AreEqual((int)CardRushObstacleEvent.Blocked, r.S.ObstacleEvent);
            Assert.Less(r.S.ObstacleEventTick, used + CardRushRules.BlockTicks);
            Assert.AreEqual(pos, r.S.PosMm, "막는 동안 서 있다");
            Assert.AreEqual(0, r.S.StunEndTick);
            Assert.AreEqual(0, r.S.Hits);
            Assert.AreEqual(1, r.S.CleanStreak);
            while (r.Now < used + CardRushRules.BlockTicks) r.Tick();
            Assert.AreEqual(pos, r.S.PosMm, "방패가 끝날 때까지 서 있다");
            r.Tick();
            Assert.AreEqual(pos + CardRushRules.RunMmPerTick, r.S.PosMm);
        }

        [Test]
        public void 가만있으면_꽈당_1390_뒤로_35틱()
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
            //  굴러오는 것이 풀린 뒤 여러 거리에서 대시를 쓴다 — 어느 거리에서도 몸이 굴러오는 것 앞을 지나가지 못한다.
            var r = new Rig(Track(new[] { Roller(20000) }), pos: 13000);
            const int closing = CardRushRules.RunMmPerTick + CardRushRules.RollerMmPerTick;
            r.RunUntil(x => x.RollerGap() - closing <= CardRushRules.RollerContactMm + extraGap);
            Assert.AreEqual(0, r.S.ObstacleEvent, "대시 전엔 아직 안 닿았다");
            r.Use(CardColor.Blue);
            r.RunUntil(x => x.S.ObstacleEvent != 0, 200);
            Assert.AreEqual((int)CardRushObstacleEvent.Tumbled, r.S.ObstacleEvent);
        }

        [Test]
        public void 닿기_직전_빨강이면_안_멈춘다()
        {
            var r = new Rig(Track(new[] { Roller(20000) }), pos: 13000);
            r.RunUntil(x => x.RollerGap() <= 800);
            int pos = r.S.PosMm;
            r.Use(CardColor.Red);
            Assert.AreEqual((int)CardRushObstacleEvent.Smashed, r.S.ObstacleEvent);
            Assert.AreEqual(pos + CardRushRules.RunMmPerTick, r.S.PosMm);
            Assert.AreEqual(0, r.S.StunEndTick);
            Assert.AreEqual(1, r.S.CleanStreak);
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
        public void 구역_안에서_터지면_으악_2770_뒤로_50틱()
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
        public void 방패면_버팀()
        {
            var r = new Rig(Track(zones: new[] { Zone(10000, 10500, 20000) }), pos: 9990);
            r.Tick();
            long on = r.S.ZoneTick;
            while (r.Now < on + 60 - 10) r.Tick();
            int pos = r.S.PosMm;
            r.Use(CardColor.Yellow);
            r.RunUntil(x => x.S.ZoneEvent != 0, 20);
            Assert.AreEqual((int)CardRushZoneEvent.Held, r.S.ZoneEvent);
            Assert.AreEqual(pos, r.S.PosMm);
            Assert.AreEqual(0, r.S.StunEndTick);
            Assert.AreEqual(0, r.S.Hits);
            Assert.AreEqual(1, r.S.CleanStreak);
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
            var done = new CardRushRunnerState { PosMm = 140000, FinishTick = 900 };
            var doneBefore = done;
            CardRushRunnerSim.Step(ref done, 1000, true, G, 0, track, Tuning);
            Assert.AreEqual(doneBefore, done, "완주 뒤");
        }

        [Test]
        public void 쿨다운_중_탭은_버린다_쌓지_않는다()
        {
            var r = new Rig(Track());
            long used = r.Use(CardColor.Yellow);
            while (r.Now < used + CardRushRules.CooldownTicks) r.Tick(true);
            Assert.AreEqual(1, r.S.UseCount, "쿨다운 중 탭은 안 먹는다");
            r.Tick();
            Assert.AreEqual(1, r.S.UseCount, "쌓아 둔 탭이 쿨다운 끝에 나가지 않는다");
            r.Tick(true);
            Assert.AreEqual(2, r.S.UseCount);
        }

        [Test]
        public void 방패_중에도_쿨다운이_끝나면_대시한다()
        {
            //  쓴 뒤 새 첫 장이 파랑인 레인을 고른다 — 쿨다운이 끝나는 바로 그 틱(방패 50틱 안)에 대시.
            var track = Track();
            int lane = Enumerable.Range(0, CardRushLanes.MaxLanes).First(l => track.FirstCard(l, 1) == CardColor.Blue);
            var r = new Rig(track) { Lane = lane };
            long used = r.Use(CardColor.Yellow);
            while (r.Now < used + CardRushRules.CooldownTicks) r.Tick();
            int pos = r.S.PosMm;
            Assert.Less(r.Now, r.S.BlockEndTick, "아직 방패 중");
            r.Tick(true);
            Assert.AreEqual(2, r.S.UseCount);
            Assert.AreEqual(pos + CardRushRules.DashOffset(1), r.S.PosMm, "방패를 든 채 미끄러진다");
            Assert.AreEqual(used + CardRushRules.BlockTicks, r.S.BlockEndTick, "방패는 그대로");
        }

        // ── 성질 ──

        [Test]
        public void 기다리면_언젠가_완주한다()
        {
            //  바리케이드는 기다리면 열리고 굴러오는 것은 결국 만난다 — 탭 없이도 막혀 영영 못 가는 판이 없다.
            for (ulong seed = 1; seed <= 30; seed++)
            {
                var track = new CardRushTrack(seed, Tuning);
                var s = new CardRushRunnerState();
                for (long t = G; t < G + 6000 && s.FinishTick == 0; t++) CardRushRunnerSim.Step(ref s, t, false, G, 0, track, Tuning);
                Assert.AreNotEqual(0, s.FinishTick, $"seed {seed}: 6000틱 안에 완주 — 자리 {s.PosMm}");
                Assert.AreEqual(track.Obstacles.Count, s.ObstacleIndex, $"seed {seed}");
                Assert.AreEqual(track.Zones.Count, s.ZoneIndex, $"seed {seed}");
            }
        }

        [Test]
        public void 연타는_읽는_것보다_느리다()
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
            Assert.Less(readMedian, mashMedian, $"읽기 중앙 {readMedian}틱, 연타 중앙 {mashMedian}틱");
        }

        private delegate bool Policy(in CardRushRunnerState s, long tick);

        private static long FinishTicks(CardRushTrack track, Policy tap)
        {
            var s = new CardRushRunnerState();
            for (long t = G; t < G + 20000; t++)
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

        /// <summary>
        /// 읽는 사람(반응 0): 손 닿는 장애물엔 빨강, 굴러오는 것 접촉 반 장 전이면 노랑도, 터질 구역 안이면 끝이 가까울 때 파랑·아니면 노랑,
        /// 그 밖엔 파랑(굴러오는 것이 풀려 있으면 들이받지 않게 기다린다).
        /// </summary>
        private static bool ReaderTaps(in CardRushRunnerState s, long tick, CardRushTrack track)
        {
            if (!CardRushCard.CanUse(s, tick, G) || !CardRushCard.TryCardAt(s, tick, G, 0, track, out var card)) return false;
            const int halfCard = CardRushRules.CardDwellTicks / 2;
            const int closing = CardRushRules.RunMmPerTick + CardRushRules.RollerMmPerTick;

            if (s.ZoneIndex < track.Zones.Count && s.ZoneTick != 0)
            {
                var z = track.Zones[s.ZoneIndex];
                long left = s.ZoneTick + z.WarnTicks - tick;
                int exit = z.ToMm + CardRushRules.ZoneMarginMm + 1 - s.PosMm;
                int reachAtBoom = s.PosMm + (int)(CardRushRules.RunMmPerTick * left);
                bool willBeInside = reachAtBoom >= z.FromMm - CardRushRules.ZoneMarginMm && exit > CardRushRules.RunMmPerTick * left;
                if (willBeInside)
                {
                    if (exit <= CardRushRules.DashMm - CardRushRules.RunMmPerTick) return card == CardColor.Blue;
                    if (left < CardRushRules.BlockTicks) return card == CardColor.Yellow;
                }
            }

            if (CardRushRunnerSim.TryFront(s, tick, track, out int front))
            {
                int d = front - s.PosMm;
                bool roller = track.Obstacles[s.ObstacleIndex].Kind == CardRushObstacleKind.Roller;
                if (d > -CardRushRules.SmashBackMm && d <= CardRushRules.SmashReachMm)
                {
                    if (card == CardColor.Red) return true;
                    return roller && (d - CardRushRules.RollerContactMm) <= closing * halfCard && card == CardColor.Yellow;
                }
                if (roller) return false;
            }
            return card == CardColor.Blue;
        }
    }
}
