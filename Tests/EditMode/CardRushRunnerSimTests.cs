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
        private static CardRushObstacle Goblin(int at) => new CardRushObstacle(CardRushObstacleKind.Goblin, at, 0);

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
                //  첫 장이 무작위라 그 색이 처음 나오는 장을 찾는다(자동 잡기 전까지의 장 안에서).
                int offset = 0;
                while (Track.CardOf(Lane, S.UseCount, offset) != color)
                {
                    offset++;
                    Assert.Less(offset * CardRushRules.CardDwellTicks, CardRushRules.RollMaxTicks, $"{color}이 도는 줄에 없다");
                }
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
        public void 탭이_없으면_틱마다_RunMmPerTick_달린다()
        {
            var s = new CardRushRunnerState();
            var track = Track();
            for (long t = G; t < G + 10; t++) CardRushRunnerSim.Step(ref s, t, false, G, 0, track, Tuning);
            Assert.AreEqual(10 * 60, s.PosMm, "틱마다 60 mm");
            Assert.AreEqual(10 * CardRushRules.RunMmPerTick, s.PosMm);
        }

        [Test]
        public void 대시는_14틱에_DashMm()
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
            //  던지는 동안 몸이 풀리는 자리(7 m 앞)를 지나 굴러오기 시작하므로 서 있는 자리 기준 틱보다 같거나 일찍 맞는다.
            long standingHit = t0 + (distance + CardRushRules.ShotMmPerTick - 1) / CardRushRules.ShotMmPerTick;
            Assert.LessOrEqual(r.S.ObstacleEventTick, standingHit);
            Assert.Greater(r.S.ObstacleEventTick, t0);
            Assert.AreEqual(0, r.S.Hits);
            Assert.AreEqual(0, r.S.StunEndTick);
        }

        [Test]
        public void 굴러오는_것과_같은_틱이면_투사체가_이긴다()
        {
            //  이 틱: 몸 p + 60, 굴러오는 것 p + 300 − 38 → 202 mm(닿음). 투사체 출발 자리 p + 300 ≥ 앞면(맞음). 둘 다면 부숨이다.
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

        // ── 대시와 구덩이 ──

        //  대시는 쓴 틱부터 DashMm를 간다 — 시작 자리 p에서 p + DashMm에 내려앉는다. 중간엔 구덩이를 지난다.
        [Test]
        public void 대시로_구덩이를_넘는다_앞_가장자리_바로_앞에서도([Values(1000, 1400)] int width, [Range(0, 3600, 50)] int before)
        {
            const int at = 20000;
            var r = new Rig(Track(new[] { Pit(at, width) }), pos: at - 1 - before);
            r.Use(CardColor.Blue);
            r.RunUntil(x => x.S.ObstacleEvent != 0, 60);
            Assert.AreEqual((int)CardRushObstacleEvent.Jumped, r.S.ObstacleEvent, "빠지지 않고 넘었다");
            Assert.AreEqual(1, r.S.ObstacleIndex);
            Assert.AreEqual(0, r.S.Hits, "한 번도 안 빠졌다");
            Assert.AreEqual(1, r.S.CleanStreak);
            Assert.AreEqual(0, r.S.StunEndTick);
            Assert.Greater(r.S.PosMm, at + width);
        }

        //  내려앉는 자리(시작 + DashMm)가 구덩이 안이면 마지막 틱에 땅으로 보고 빠진다.
        [TestCase(1200, 0)]
        [TestCase(1200, 600)]
        [TestCase(1200, 1200)]
        [TestCase(1000, 1000)]
        public void 대시가_구덩이_안에서_끝나면_빠진다(int width, int landingRelAt)
        {
            const int at = 20000;
            var r = new Rig(Track(new[] { Pit(at, width) }), pos: at + landingRelAt - CardRushRules.DashMm);
            long t0 = r.Use(CardColor.Blue);
            r.RunUntil(x => x.S.ObstacleEvent != 0, 60);
            Assert.AreEqual((int)CardRushObstacleEvent.Fell, r.S.ObstacleEvent);
            Assert.AreEqual(t0 + CardRushRules.DashTicks - 1, r.S.ObstacleEventTick, "내려앉은 틱에 빠진다");
            Assert.AreEqual(at - CardRushRules.PitBackMm, r.S.PosMm);
            Assert.AreEqual(0, r.S.ObstacleIndex, "다시 넘어야 한다");
            Assert.AreEqual(1, r.S.Hits);
        }

        [Test]
        public void 대시가_구덩이_앞에서_끝나면_이어_달려_빠진다()
        {
            const int at = 20000;
            var r = new Rig(Track(new[] { Pit(at, 1200) }), pos: at - 1000 - CardRushRules.DashMm);
            r.Use(CardColor.Blue);
            r.Ticks(CardRushRules.DashTicks - 1);
            Assert.AreEqual(0, r.S.DashStartTick, "대시가 끝났다");
            Assert.AreEqual(0, r.S.ObstacleEvent, "대시는 구덩이 앞에 내려앉았다");
            Assert.AreEqual(at - 1000, r.S.PosMm);
            r.RunUntil(x => x.S.ObstacleEvent != 0, 60);
            Assert.AreEqual((int)CardRushObstacleEvent.Fell, r.S.ObstacleEvent, "대시가 끝나면 다시 구덩이에 빠진다");
        }

        [Test]
        public void 대시_없이_달려_들어가면_빠진다_노랑과_빨강은_구덩이를_못_넘는다([Values(CardColor.Red, CardColor.Yellow)] CardColor color)
        {
            const int at = 20000;
            var r = new Rig(Track(new[] { Pit(at, 1200) }), pos: at - 1000);
            r.Use(color);
            r.RunUntil(x => x.S.ObstacleEvent != 0, 60);
            Assert.AreEqual((int)CardRushObstacleEvent.Fell, r.S.ObstacleEvent);
            Assert.AreEqual(1, r.S.Hits);
        }

        [Test]
        public void 땅에서_구덩이에_들어가면_2_5m_뒤에서_기절_번호_그대로()
        {
            const int at = 20000;
            var r = new Rig(Track(new[] { Pit(at, 1200) }), pos: at - 100);
            r.Tick();
            Assert.AreEqual(0, r.S.ObstacleEvent, "아직 앞");
            r.Tick();
            long fell = r.Now - 1;
            Assert.AreEqual((int)CardRushObstacleEvent.Fell, r.S.ObstacleEvent);
            Assert.AreEqual(fell, r.S.ObstacleEventTick);
            Assert.AreEqual(at - 2500, r.S.PosMm);
            Assert.AreEqual(at - CardRushRules.PitBackMm, r.S.PosMm);
            Assert.AreEqual(fell + Tuning.PitStunTicks, r.S.StunEndTick);
            Assert.AreEqual(0, r.S.ObstacleIndex, "다시 넘어야 한다");
            Assert.AreEqual(1, r.S.Hits);
            Assert.AreEqual(0, r.S.CleanStreak);

            while (r.Now < fell + Tuning.PitStunTicks) r.Tick();
            Assert.AreEqual(at - CardRushRules.PitBackMm, r.S.PosMm, "기절 중엔 서 있다");
            r.RunUntil(x => x.S.Hits == 2, 200);
            Assert.AreEqual((int)CardRushObstacleEvent.Fell, r.S.ObstacleEvent, "안 피하면 또 빠진다");
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
        public void 바리케이드는_대시로도_못_넘는다_부딪혀_멈춘다()
        {
            const int at = 20000;
            int stop = at - CardRushRules.BarricadeStopMm;
            var r = new Rig(Track(new[] { Barricade(at) }), pos: stop - 1000);
            long t0 = r.Use(CardColor.Blue);
            r.RunUntil(x => x.S.ObstacleTick != 0, 40);
            Assert.Less(r.S.ObstacleTick, t0 + CardRushRules.DashTicks - 1, "대시 도중에 부딪혔다");
            Assert.AreEqual(stop, r.S.PosMm);
            Assert.AreEqual(0, r.S.DashStartTick);
            Assert.AreEqual(0, r.S.ObstacleIndex);
            r.Tick();
            Assert.AreEqual(stop, r.S.PosMm, "바리케이드 앞에 서 있다");
        }

        [Test]
        public void 대시_중이면_굴러오는_것을_넘는다()
        {
            //  대시 중에 닿으면 꽈당이 아니라 넘은 것(Jumped) — 옛 점프와 같은 취급.
            const int p = 13000;
            var r = new Rig(Track(new[] { Roller(p + CardRushRules.RollerContactMm + 98 * 10) }), pos: p);
            r.S.ObstacleTick = r.Now;
            r.Use(CardColor.Blue);
            r.RunUntil(x => x.S.ObstacleEvent != 0, 40);
            Assert.AreEqual((int)CardRushObstacleEvent.Jumped, r.S.ObstacleEvent);
            Assert.AreEqual(1, r.S.ObstacleIndex);
            Assert.AreEqual(0, r.S.Hits);
            Assert.AreEqual(0, r.S.StunEndTick);
            Assert.AreEqual(1, r.S.CleanStreak);
        }

        [Test]
        public void 대시_중에도_다음_카드를_쓴다_빨강은_던진다()
        {
            var r = new Rig(Track(), pos: 5000);
            long dash = r.Use(CardColor.Blue);
            r.Ticks(5);
            int pos = r.S.PosMm;
            long thrown = r.Use(CardColor.Red);
            Assert.AreEqual(thrown, r.S.ShotTick);
            Assert.AreEqual(pos + CardRushRules.ShotStartAheadMm, r.S.ShotFromMm);
            Assert.AreEqual(dash, r.S.DashStartTick, "대시는 그대로 이어진다");
            Assert.AreEqual(2, r.S.UseCount);
        }

        [Test]
        public void 대시_중에_파랑을_또_쓰면_대시를_새로_시작한다()
        {
            var r = new Rig(Track(), pos: 5000);
            r.Use(CardColor.Blue);
            r.Ticks(5);
            long second = r.Use(CardColor.Blue);
            Assert.AreEqual(second, r.S.DashStartTick);
        }

        // ── 황금 고블린 ──

        [Test]
        public void 고블린은_5m_앞에서_나타난다_그전엔_없다()
        {
            const int at = 20000;
            int reveal = at - CardRushRules.GoblinRevealMm;
            var r = new Rig(Track(new[] { Goblin(at) }), pos: reveal - 3 * CardRushRules.RunMmPerTick + 1);
            r.Ticks(2);
            Assert.AreEqual(0, r.S.ObstacleTick, "아직 5 m 밖");
            Assert.Less(r.S.PosMm, reveal);
            r.Tick();
            Assert.AreEqual(r.Now - 1, r.S.ObstacleTick, "나타난 틱");
            Assert.GreaterOrEqual(r.S.PosMm, reveal);
            Assert.AreEqual(0, r.S.ObstacleIndex, "나타났을 뿐 아직 지금 장애물");
            Assert.AreEqual(0, r.S.ObstacleEvent);
        }

        [Test]
        public void 고블린_자리는_나타난_틱부터_틱마다_40mm씩_달아난다()
        {
            var o = Goblin(20000);
            Assert.AreEqual(20000, CardRushRunnerSim.GoblinPos(o, 500, 500));
            Assert.AreEqual(20000 + 40 * 10, CardRushRunnerSim.GoblinPos(o, 500, 510));
            Assert.AreEqual(20000 + 40 * 150, CardRushRunnerSim.GoblinPos(o, 500, 650));
        }

        [Test]
        public void 고블린은_나타나고_정확히_150틱째에_놓친다()
        {
            var r = new Rig(Track(new[] { Goblin(20000) }), pos: 15000 - 1);
            r.S.CleanStreak = 2;
            r.S.BestCleanStreak = 2;
            r.Tick();
            long reveal = r.S.ObstacleTick;
            Assert.AreEqual(r.Now - 1, reveal);
            r.Ticks(CardRushRules.GoblinLifeTicks - 1);
            Assert.AreEqual(reveal + CardRushRules.GoblinLifeTicks - 1, r.Now - 1);
            Assert.AreEqual(0, r.S.ObstacleIndex, "149틱째까지는 산다");
            Assert.AreEqual(0, r.S.ObstacleEvent);
            r.Tick();
            Assert.AreEqual(reveal + CardRushRules.GoblinLifeTicks, r.Now - 1);
            Assert.AreEqual(1, r.S.ObstacleIndex, "150틱째에 사라진다");
            Assert.AreEqual((int)CardRushObstacleEvent.Escaped, r.S.ObstacleEvent);
            Assert.AreEqual(9, r.S.ObstacleEvent, "와이어 값");
            Assert.AreEqual(reveal + CardRushRules.GoblinLifeTicks, r.S.ObstacleEventTick);
            Assert.AreEqual(0, r.S.ObstacleTick);
            Assert.AreEqual(0, r.S.RushEndTick, "놓치면 러시도 없다");
            Assert.AreEqual(0, r.S.Hits);
            Assert.AreEqual(2, r.S.CleanStreak, "놓침은 연속 무사 통과를 건드리지 않는다");
            Assert.AreEqual(2, r.S.BestCleanStreak);
        }

        //  나타난 지 elapsed틱에 잡으면 150 − 100·elapsed/150 (정수 나눗셈) — 바로 150, 사라지는 틱 50.
        [TestCase(0, 1, 150)]
        [TestCase(30, 31, 130)]
        [TestCase(74, 75, 100)]
        [TestCase(100, 101, 83)]
        [TestCase(148, 149, 51)]
        [TestCase(149, 150, 50)]
        public void 고블린을_노랑으로_잡으면_빨리_잡을수록_러시가_길다(int waitTicks, int elapsed, int expectedRush)
        {
            const int at = 20000;
            var r = new Rig(Track(new[] { Goblin(at) }), pos: at - CardRushRules.GoblinRevealMm - 1);
            r.S.CleanStreak = 3;
            r.Tick();
            long reveal = r.S.ObstacleTick;
            Assert.AreNotEqual(0, reveal);
            r.Ticks(waitTicks);
            int before = r.S.PosMm;
            long t0 = r.Use(CardColor.Yellow);
            Assert.AreEqual(reveal + elapsed, t0, "쓴 틱 = 나타난 틱 + 경과");
            Assert.AreEqual(expectedRush, CardRushRunnerSim.RushTicksFor(elapsed));
            Assert.AreEqual(t0 + expectedRush, r.S.RushEndTick);
            Assert.AreEqual((int)CardRushObstacleEvent.Caught, r.S.ObstacleEvent);
            Assert.AreEqual(8, r.S.ObstacleEvent, "와이어 값");
            Assert.AreEqual(t0, r.S.ObstacleEventTick);
            Assert.AreEqual(1, r.S.ObstacleIndex);
            Assert.AreEqual(0, r.S.ObstacleTick);
            Assert.AreEqual(0, r.S.Hits);
            Assert.AreEqual(3, r.S.CleanStreak, "잡음은 연속 무사 통과를 건드리지 않는다");
            Assert.AreEqual(before + CardRushRules.RushMmPerTick, r.S.PosMm, "쓴 틱부터 러시 속도");
        }

        [Test]
        public void 러시_중엔_틱마다_96mm_끝나면_다시_60mm()
        {
            const int at = 20000;
            var r = new Rig(Track(new[] { Goblin(at) }), pos: at - CardRushRules.GoblinRevealMm - 1);
            r.Tick();
            long reveal = r.S.ObstacleTick;
            int before = r.S.PosMm;
            long t0 = r.Use(CardColor.Yellow);
            int rush = CardRushRunnerSim.RushTicksFor(t0 - reveal);
            Assert.AreEqual(150, rush, "바로 잡았다");
            Assert.IsTrue(CardRushRunnerSim.IsRushing(r.S, t0));
            r.Ticks(rush - 1);
            Assert.AreEqual(before + 96 * rush, r.S.PosMm, "러시 동안 96 mm씩");
            Assert.IsTrue(CardRushRunnerSim.IsRushing(r.S, r.Now - 1), "마지막 러시 틱");
            Assert.IsFalse(CardRushRunnerSim.IsRushing(r.S, r.Now), "RushEndTick 틱부터 러시가 아니다");
            r.Tick();
            Assert.AreEqual(before + 96 * rush + 60, r.S.PosMm, "러시가 끝나면 다시 60 mm");
            r.Tick();
            Assert.AreEqual(before + 96 * rush + 120, r.S.PosMm);
        }

        [Test]
        public void 러시여도_바리케이드는_못_넘는다()
        {
            //  러시는 달리기 속도만 바꾼다 — 바리케이드 앞에서 멈추는 건 그대로다.
            const int at = 20000;
            int stop = at - CardRushRules.BarricadeStopMm;
            var r = new Rig(Track(new[] { Barricade(at) }), pos: stop - 100);
            r.S.RushEndTick = r.Now + 500;
            r.Tick();
            r.Tick();
            Assert.AreEqual(stop, r.S.PosMm, "러시여도 바리케이드를 못 넘는다");
        }

        [Test]
        public void 노랑이어도_고블린이_없으면_헛손질이다()
        {
            var empty = new Rig(Track(), pos: 5000);
            empty.Use(CardColor.Yellow);
            Assert.AreEqual(0, empty.S.RushEndTick, "장애물이 없다");
            Assert.AreEqual(1, empty.S.UseCount, "카드는 쓴 것으로 센다");
            Assert.AreEqual((int)CardRushObstacleEvent.None, empty.S.ObstacleEvent);

            var barricade = new Rig(Track(new[] { Barricade(30000) }), pos: 5000);
            barricade.Use(CardColor.Yellow);
            Assert.AreEqual(0, barricade.S.RushEndTick);
            Assert.AreEqual(0, barricade.S.ObstacleIndex, "다른 장애물은 못 치운다");
            Assert.AreEqual((int)CardRushObstacleEvent.None, barricade.S.ObstacleEvent);

            //  아직 안 나타난 고블린도 못 잡는다.
            var hidden = new Rig(Track(new[] { Goblin(30000) }), pos: 5000);
            hidden.Use(CardColor.Yellow);
            Assert.AreEqual(0, hidden.S.ObstacleTick, "아직 안 나타났다");
            Assert.AreEqual(0, hidden.S.RushEndTick);
            Assert.AreEqual(0, hidden.S.ObstacleIndex);
            Assert.AreEqual((int)CardRushObstacleEvent.None, hidden.S.ObstacleEvent);

            //  양성 대조 — 나타난 뒤엔 같은 노랑이 잡는다.
            var shown = new Rig(Track(new[] { Goblin(30000) }), pos: 30000 - CardRushRules.GoblinRevealMm);
            shown.Tick();
            Assert.AreNotEqual(0, shown.S.ObstacleTick);
            shown.Use(CardColor.Yellow);
            Assert.AreNotEqual(0, shown.S.RushEndTick);
        }

        [TestCase(CardColor.Red)]
        [TestCase(CardColor.Blue)]
        public void 빨강_파랑은_고블린을_못_잡는다(CardColor color)
        {
            var r = new Rig(Track(new[] { Goblin(30000) }), pos: 30000 - CardRushRules.GoblinRevealMm);
            r.Tick();
            r.Use(color);
            Assert.AreEqual(0, r.S.RushEndTick);
            Assert.AreEqual(0, r.S.ObstacleIndex);
        }

        [Test]
        public void RushTicksFor는_끝점이_정해져_있고_늦을수록_줄어든다()
        {
            Assert.AreEqual(150, CardRushRunnerSim.RushTicksFor(0));
            Assert.AreEqual(50, CardRushRunnerSim.RushTicksFor(150));
            Assert.AreEqual(150, CardRushRunnerSim.RushTicksFor(-5), "범위 밖은 끝점으로");
            Assert.AreEqual(50, CardRushRunnerSim.RushTicksFor(400), "범위 밖은 끝점으로");
            int prev = int.MaxValue;
            bool dropped = false;
            for (int e = 0; e <= CardRushRules.GoblinLifeTicks; e++)
            {
                int v = CardRushRunnerSim.RushTicksFor(e);
                Assert.LessOrEqual(v, prev, $"e={e}");
                Assert.That(v, Is.InRange(CardRushRules.RushMinTicks, CardRushRules.RushMaxTicks));
                dropped |= v < prev && prev != int.MaxValue;
                prev = v;
            }
            Assert.IsTrue(dropped, "늦을수록 정말 줄어든다");
            Assert.Less(CardRushRunnerSim.RushTicksFor(100), CardRushRunnerSim.RushTicksFor(30));
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
        public void 대시로_굴러오는_것에_닿으면_꽈당이_아니라_넘는다()
        {
            var r = new Rig(Track(new[] { Roller(20000) }), pos: 13000);
            r.RunUntil(x => x.RollerGap() <= 2000);
            r.Use(CardColor.Blue);
            r.RunUntil(x => x.S.ObstacleEvent != 0, 30);
            Assert.AreEqual((int)CardRushObstacleEvent.Jumped, r.S.ObstacleEvent);
            Assert.AreEqual(0, r.S.Hits);
            Assert.AreEqual(0, r.S.StunEndTick);
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
            var r = new Rig(Track(zones: new[] { Zone(10000, 10500, 14000) }), pos: 9990);
            r.Tick();
            long on = r.S.ZoneTick;
            while (r.Now < on + 40) r.Tick();
            Assert.LessOrEqual(r.S.PosMm, 14000 + CardRushRules.ZoneMarginMm, "대시 전엔 아직 안");
            r.Use(CardColor.Blue);
            r.RunUntil(x => x.S.ZoneEvent != 0, 30);
            Assert.AreEqual((int)CardRushZoneEvent.Dodged, r.S.ZoneEvent);
            Assert.Greater(r.S.PosMm, 14000 + CardRushRules.ZoneMarginMm);
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
        public void 대시_중에_구역에_맞으면_대시가_끊기고_기절한다()
        {
            var r = new Rig(Track(zones: new[] { Zone(10000, 10500, 20000) }), pos: 9990);
            r.Tick();
            long boom = r.S.ZoneTick + 60;
            while (r.Now < boom - 10) r.Tick();
            r.Use(CardColor.Blue);
            Assert.AreNotEqual(0, r.S.DashStartTick, "대시 중이다");
            r.RunUntil(x => x.S.ZoneEvent != 0, 20);
            Assert.AreEqual((int)CardRushZoneEvent.Hit, r.S.ZoneEvent);
            Assert.AreEqual(0, r.S.DashStartTick, "밀려나면 대시가 끝난다");
            int knocked = r.S.PosMm;
            r.Ticks(5);
            Assert.AreEqual(knocked, r.S.PosMm, "기절 중 앞으로 날아가지 않는다");
        }

        [Test]
        public void 구역은_한_번만_터진다()
        {
            var r = new Rig(Track(zones: new[] { Zone(10000, 10500, 20000, warn: 45) }), pos: 9990);
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
        public void 파랑만_제때_써도_완주한다()
        {
            //  구덩이는 파랑 대시로만 넘는다 — 탭 없이는 영영 못 간다. 파랑만 잡아 구덩이 앞에서 쓰면 완주한다.
            //  바리케이드는 기다리면 올라가고 고블린은 놓쳐도 지나간다.
            int pitsSeen = 0, goblinsSeen = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var track = new CardRushTrack(seed, Tuning);
                pitsSeen += track.Obstacles.Count(o => o.Kind == CardRushObstacleKind.Pit);
                goblinsSeen += track.Obstacles.Count(o => o.Kind == CardRushObstacleKind.Goblin);
                var s = new CardRushRunnerState();
                for (long t = G; t < G + 30000 && s.FinishTick == 0; t++)
                    CardRushRunnerSim.Step(ref s, t, BlueOnly(s, t, track), G, 0, track, Tuning);
                Assert.AreNotEqual(0, s.FinishTick, $"seed {seed}: 완주 — 자리 {s.PosMm} 장애물 {s.ObstacleIndex}/{track.Obstacles.Count}");
                Assert.AreEqual(track.Obstacles.Count, s.ObstacleIndex, $"seed {seed}");
            }
            Assert.Greater(pitsSeen, 20, "구덩이가 실제로 깔려야 시험이 뜻이 있다");
            Assert.Greater(goblinsSeen, 0, "고블린도 실제로 깔려야 한다");
        }

        [Test]
        public void 탭을_안_하면_첫_구덩이를_못_넘는다()
        {
            //  음성 대조 — 위 시험이 대시 덕에 완주하는 것이지 구덩이가 공짜가 아니다.
            int stuck = 0;
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var track = new CardRushTrack(seed, Tuning);
                if (!track.Obstacles.Any(o => o.Kind == CardRushObstacleKind.Pit)) continue;
                var s = new CardRushRunnerState();
                for (long t = G; t < G + 6000 && s.FinishTick == 0; t++)
                    CardRushRunnerSim.Step(ref s, t, false, G, 0, track, Tuning);
                if (s.FinishTick == 0) stuck++;
            }
            Assert.Greater(stuck, 0, "구덩이가 있는 코스는 가만히 달려선 완주 못 한다");
        }

        //  파랑을 잡아 두었다가 구덩이 앞 2.5 m 안에서 쓴다 — 내려앉는 자리(+5.2 m)가 가장 넓은 구덩이(1.4 m)도 지난다.
        private static bool BlueOnly(in CardRushRunnerState s, long tick, CardRushTrack track)
        {
            if (CardRushCard.CanStart(s, tick, G)) return true;
            if (CardRushCard.CanLock(s, tick, G))
                return CardRushCard.CardAt(s, tick, G, 0, track, out var seen) && seen == CardColor.Blue;
            if (!CardRushCard.CanUse(s, tick, G, 0, track) || s.ObstacleIndex >= track.Obstacles.Count) return false;
            var o = track.Obstacles[s.ObstacleIndex];
            return o.Kind == CardRushObstacleKind.Pit && o.AtMm > s.PosMm && o.AtMm - s.PosMm <= 2500;
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
