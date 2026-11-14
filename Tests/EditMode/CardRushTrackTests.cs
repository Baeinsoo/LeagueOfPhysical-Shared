using System.Linq;
using NUnit.Framework;

namespace LOP.Tests
{
    //  시험용 튜닝 — 시작 값(spec §6.1). 실제 표 값으로 도는 시험은 서버 BotMatch가 맡는다.
    internal static class CardRushTestTuning
    {
        public static CardRushTuning Default => new CardRushTuning(
            trackMm: 140000, obstacleGapMinMm: 11000, obstacleGapMaxMm: 19000, barricadePermille: 500,
            zoneGapMinMm: 28000, zoneGapMaxMm: 48000, barricadeWaitTicks: 60,
            rollKnockMm: 1390, rollStunTicks: 35, zoneKnockMm: 2770, zoneStunTicks: 50,
            pitPermille: 350, pitStunTicks: 40);
    }

    public class CardRushTrackTests
    {
        private static CardRushTrack Make(ulong seed) => new CardRushTrack(seed, CardRushTestTuning.Default);

        [Test]
        public void 같은_씨앗이면_같은_줄()
        {
            var a = Make(12345); var b = Make(12345);
            Assert.AreEqual(a.Fingerprint, b.Fingerprint);
            CollectionAssert.AreEqual(a.Obstacles.ToArray(), b.Obstacles.ToArray());
            CollectionAssert.AreEqual(a.Zones.ToArray(), b.Zones.ToArray());
        }

        [Test]
        public void 다른_씨앗이면_다른_줄()
        {
            var a = Make(12345); var b = Make(54321);
            Assert.AreNotEqual(a.Fingerprint, b.Fingerprint);
            CollectionAssert.AreNotEqual(a.Obstacles.ToArray(), b.Obstacles.ToArray());
        }

        [Test]
        public void 지문은_줄이_바뀌면_바뀐다()
        {
            var obstacles = new[] { new CardRushObstacle(CardRushObstacleKind.Barricade, 8000, 0) };
            var moved = new[] { new CardRushObstacle(CardRushObstacleKind.Barricade, 8001, 0) };
            var zones = new[] { new CardRushZone(14000, 15000, 18000, 70) };
            var a = CardRushTrack.ForTests(140000, obstacles, zones, 1);
            Assert.AreNotEqual(a.Fingerprint, CardRushTrack.ForTests(140000, moved, zones, 1).Fingerprint);
            Assert.AreNotEqual(a.Fingerprint, CardRushTrack.ForTests(140000, obstacles, new[] { new CardRushZone(14000, 15000, 18000, 69) }, 1).Fingerprint);

            var pit = new[] { new CardRushObstacle(CardRushObstacleKind.Pit, 8000, 0, 1200) };
            var widerPit = new[] { new CardRushObstacle(CardRushObstacleKind.Pit, 8000, 0, 1201) };
            Assert.AreNotEqual(CardRushTrack.ForTests(140000, pit, zones, 1).Fingerprint,
                               CardRushTrack.ForTests(140000, widerPit, zones, 1).Fingerprint, "구덩이 폭도 지문에 든다");
        }

        //  최종 리뷰 m4: 줄이 같아도 벌(기다림·밀림·기절)이 클·서에서 다르면 첫 바리케이드·꽈당부터 끝없이 보정한다 — 지문이 달라야 로그로 잡힌다.
        [TestCase("barricadeWait")]
        [TestCase("rollKnock")]
        [TestCase("rollStun")]
        [TestCase("zoneKnock")]
        [TestCase("zoneStun")]
        [TestCase("pitStun")]
        public void 지문은_벌_튜닝이_다르면_다르다(string field)
        {
            var d = CardRushTestTuning.Default;
            var other = new CardRushTuning(d.TrackMm, d.ObstacleGapMinMm, d.ObstacleGapMaxMm, d.BarricadePermille,
                d.ZoneGapMinMm, d.ZoneGapMaxMm,
                d.BarricadeWaitTicks + (field == "barricadeWait" ? 1 : 0),
                d.RollKnockMm + (field == "rollKnock" ? 1 : 0),
                d.RollStunTicks + (field == "rollStun" ? 1 : 0),
                d.ZoneKnockMm + (field == "zoneKnock" ? 1 : 0),
                d.ZoneStunTicks + (field == "zoneStun" ? 1 : 0),
                d.PitPermille,
                d.PitStunTicks + (field == "pitStun" ? 1 : 0));
            var a = new CardRushTrack(12345, d);
            var b = new CardRushTrack(12345, other);
            CollectionAssert.AreEqual(a.Obstacles.ToArray(), b.Obstacles.ToArray(), "줄은 같다 — 벌만 다르다");
            CollectionAssert.AreEqual(a.Zones.ToArray(), b.Zones.ToArray());
            Assert.AreNotEqual(a.Fingerprint, b.Fingerprint);
        }

        [Test]
        public void 장애물_간격은_min_max_안_바리케이드와_구덩이만_나온다()
        {
            var t = CardRushTestTuning.Default;
            bool sawAll = false;
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var o = Make(seed).Obstacles;
                Assert.Greater(o.Count, 3, $"seed {seed}");
                for (int i = 0; i < o.Count; i++)
                {
                    Assert.That(o[i].Variant, Is.InRange(0, 2));
                    if (i > 0) Assert.That(o[i].AtMm - o[i - 1].AtMm, Is.InRange(t.ObstacleGapMinMm, t.ObstacleGapMaxMm), $"seed {seed} #{i}");
                }
                Assert.IsFalse(o.Any(x => x.Kind == CardRushObstacleKind.Roller), $"seed {seed} — 4.2차엔 굴러오는 것이 없다");
                sawAll |= o.Any(x => x.Kind == CardRushObstacleKind.Barricade) && o.Any(x => x.Kind == CardRushObstacleKind.Pit);
            }
            Assert.IsTrue(sawAll);
        }

        [Test]
        public void 구덩이_비율과_폭()
        {
            int pits = 0, all = 0;
            for (ulong seed = 1; seed <= 200; seed++)
            {
                foreach (var o in Make(seed).Obstacles)
                {
                    all++;
                    if (o.Kind == CardRushObstacleKind.Pit)
                    {
                        pits++;
                        Assert.That(o.WidthMm, Is.InRange(CardRushRules.PitWidthMinMm, CardRushRules.PitWidthMaxMm), $"seed {seed}");
                        Assert.AreEqual(o.AtMm + o.WidthMm, o.EndMm);
                    }
                    else
                    {
                        Assert.AreEqual(0, o.WidthMm, "구덩이가 아니면 폭 0");
                    }
                }
            }
            double share = (double)pits / all;
            Assert.That(share, Is.InRange(0.36, 0.46), $"구덩이 {pits}/{all} — 바리케이드 500 : 구덩이 350 → 41%");
        }

        [Test]
        public void 구덩이_비율_0이면_구덩이가_없다()
        {
            var d = CardRushTestTuning.Default;
            var noPit = new CardRushTuning(d.TrackMm, d.ObstacleGapMinMm, d.ObstacleGapMaxMm, d.BarricadePermille, d.ZoneGapMinMm, d.ZoneGapMaxMm,
                d.BarricadeWaitTicks, d.RollKnockMm, d.RollStunTicks, d.ZoneKnockMm, d.ZoneStunTicks, pitPermille: 0, pitStunTicks: d.PitStunTicks);
            for (ulong seed = 1; seed <= 50; seed++)
                Assert.IsFalse(new CardRushTrack(seed, noPit).Obstacles.Any(o => o.Kind == CardRushObstacleKind.Pit), $"seed {seed}");
        }

        [Test]
        public void 첫_장애물은_8m에_있고_구역은_없다()
        {
            //  4.2차: 빨간 바닥(구역)은 뺐다 — 판정 코드는 남지만 코스엔 깔리지 않는다.
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var track = Make(seed);
                Assert.AreEqual(CardRushRules.FirstObstacleMm, track.Obstacles[0].AtMm, $"seed {seed}");
                Assert.AreEqual(0, track.Zones.Count, $"seed {seed}");
            }
        }

        [Test]
        public void 결승_앞_5m엔_없다()
        {
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var track = Make(seed);
                int limit = track.TrackMm - CardRushRules.FinishClearMm;
                Assert.Less(track.Obstacles.Last().AtMm, limit, $"seed {seed}");
                if (track.Zones.Count > 0) Assert.Less(track.Zones.Last().ToMm, limit, $"seed {seed}");
            }
        }

        [Test]
        public void 뽑는_순서는_계약이다()
        {
            //  4.2차 구현 첫 통과 때 나온 값을 적어 고정한다(바꾸면 옛 클라와 줄이 갈린다).
            var track = Make(42);
            var obstacles = track.Obstacles.Take(6).Select(o => ((int)o.Kind, o.AtMm, o.Variant, o.WidthMm)).ToArray();
            CollectionAssert.AreEqual(ExpectedObstacles, obstacles, "지금 값: " + string.Join(", ", obstacles));
        }

        private static readonly (int, int, int, int)[] ExpectedObstacles =
        {
            (0, 8000, 2, 0), (2, 26935, 0, 1256), (0, 38276, 1, 0), (0, 52179, 2, 0), (2, 66246, 2, 1268), (2, 77348, 1, 1102),
        };

        [Test]
        public void 첫_장_뽑는_식은_계약이다()
        {
            //  구현 첫 통과 때 나온 값을 적어 고정한다 — 솔트 문자열이나 lane·쓴 횟수의 Combine 순서를 바꾸면 클·서가 다른 카드를 본다.
            var track = Make(42);
            var expected = new[,] { { 0, 0, 0, 2 }, { 2, 1, 0, 2 }, { 1, 0, 2, 1 } };
            for (int lane = 0; lane < 3; lane++)
                for (int n = 0; n < 4; n++)
                    Assert.AreEqual(expected[lane, n], (int)track.FirstCard(lane, n), $"lane {lane} n {n}");
        }

        [Test]
        public void 지문은_첫_장_씨앗도_담는다()
        {
            //  줄이 같아도 첫 장이 다르면 지문이 달라야 클·서 Shared가 갈린 것을 로그로 잡는다.
            var obstacles = new[] { new CardRushObstacle(CardRushObstacleKind.Barricade, 8000, 0) };
            var zones = new[] { new CardRushZone(14000, 15000, 18000, 70) };
            var a = CardRushTrack.ForTests(140000, obstacles, zones, 1);
            var b = CardRushTrack.ForTests(140000, obstacles, zones, 2);
            Assert.AreNotEqual(a.Fingerprint, b.Fingerprint);
            Assert.AreEqual(a.Fingerprint, CardRushTrack.ForTests(140000, obstacles, zones, 1).Fingerprint);
        }

        [Test]
        public void 첫_장은_레인과_쓴_횟수로_결정된다()
        {
            var a = Make(7); var b = Make(7);
            var seen = new System.Collections.Generic.HashSet<CardColor>();
            bool laneMatters = false, countMatters = false;
            for (int lane = 0; lane < 8; lane++)
            {
                for (int n = 0; n < 30; n++)
                {
                    Assert.AreEqual(a.FirstCard(lane, n), b.FirstCard(lane, n));
                    seen.Add(a.FirstCard(lane, n));
                    if (n > 0) countMatters |= a.FirstCard(lane, n) != a.FirstCard(lane, n - 1);
                    if (lane > 0) laneMatters |= a.FirstCard(lane, n) != a.FirstCard(lane - 1, n);
                }
            }
            Assert.AreEqual(3, seen.Count, "세 색이 모두 나온다");
            Assert.IsTrue(laneMatters);
            Assert.IsTrue(countMatters);
        }

        [Test]
        public void 도는_카드는_무작위지만_같은_색이_연달아_안_나온다()
        {
            var a = Make(7); var b = Make(7);
            bool sawRepeatFree = true, sawAll = false, notCycle = false;
            for (int lane = 0; lane < CardRushLanes.MaxLanes; lane++)
            {
                for (int n = 0; n < 6; n++)
                {
                    Assert.AreEqual(a.FirstCard(lane, n), a.CardOf(lane, n, 0), "0장째 = 첫 장");
                    var seen = new System.Collections.Generic.HashSet<CardColor>();
                    for (int turn = 0; turn < 14; turn++)
                    {
                        var c = a.CardOf(lane, n, turn);
                        Assert.AreEqual(c, b.CardOf(lane, n, turn), "같은 씨앗이면 같은 줄");
                        seen.Add(c);
                        if (turn > 0)
                        {
                            var prev = a.CardOf(lane, n, turn - 1);
                            sawRepeatFree &= c != prev;
                            //  옛 규칙(빨 → 노 → 파 고정 순서)이면 늘 prev + 1이다 — 한 번이라도 아니면 무작위다.
                            notCycle |= (int)c != ((int)prev + 1) % CardColors.Count;
                        }
                    }
                    sawAll |= seen.Count == CardColors.Count;
                }
            }
            Assert.IsTrue(sawRepeatFree, "바로 앞 장과 같은 색이 나왔다");
            Assert.IsTrue(sawAll, "세 색이 다 나온다");
            Assert.IsTrue(notCycle, "고정 순서가 아니다");
        }
    }
}
