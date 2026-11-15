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
        public void 장애물_간격은_min_max_안_고블린_뒤는_GoblinGap_굴러오는_것은_없다()
        {
            var t = CardRushTestTuning.Default;
            Assert.GreaterOrEqual(t.ObstacleGapMinMm, CardRushRules.MinObstacleGapMm, "시험 표 값이 코스 규칙의 최소 간격을 지킨다");
            bool sawAll = false, afterGoblin = false;
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var o = Make(seed).Obstacles;
                Assert.Greater(o.Count, 3, $"seed {seed}");
                for (int i = 0; i < o.Count; i++)
                {
                    Assert.That(o[i].Variant, Is.InRange(0, 2));
                    if (i > 0)
                    {
                        int gap = o[i].AtMm - o[i - 1].AtMm;
                        if (o[i - 1].Kind == CardRushObstacleKind.Goblin)
                        {
                            Assert.AreEqual(CardRushRules.GoblinGapMm, gap, $"seed {seed} #{i} — 고블린 뒤는 GoblinGapMm");
                            afterGoblin = true;
                        }
                        else
                        {
                            Assert.That(gap, Is.InRange(t.ObstacleGapMinMm, t.ObstacleGapMaxMm), $"seed {seed} #{i}");
                            Assert.GreaterOrEqual(gap, CardRushRules.MinObstacleGapMm, $"seed {seed} #{i}");
                        }
                    }
                }
                Assert.IsFalse(o.Any(x => x.Kind == CardRushObstacleKind.Roller), $"seed {seed} — 굴러오는 것은 코스에 안 깔린다");
                sawAll |= o.Any(x => x.Kind == CardRushObstacleKind.Barricade) && o.Any(x => x.Kind == CardRushObstacleKind.Pit);
            }
            Assert.IsTrue(sawAll);
            Assert.IsTrue(afterGoblin, "고블린 뒤 간격을 한 번도 못 봤다");
        }

        //  4.5차: 확률이면 다섯 판 중 한 판쯤 0~1마리였다(사용자 "금카 한 번 나오는거야") — 이제 판마다 정해진 수, 구간마다 하나.
        [Test]
        public void 고블린은_판마다_정해진_수_구간마다_하나씩_폭이_없다()
        {
            var t = CardRushTestTuning.Default;
            int start = CardRushRules.FirstObstacleMm;
            int usable = t.TrackMm - CardRushRules.FinishClearMm - start - t.ObstacleGapMaxMm;
            int count = System.Math.Min(CardRushRules.GoblinsPerTrack, usable / (CardRushRules.GoblinGapMm + t.ObstacleGapMaxMm));
            Assert.Greater(count, 1, "시험 표가 고블린을 여럿 넣을 만큼 길다");
            int section = usable / count;
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var goblins = Make(seed).Obstacles.Where(x => x.Kind == CardRushObstacleKind.Goblin).ToArray();
                Assert.AreEqual(count, goblins.Length, $"seed {seed}");
                for (int k = 0; k < goblins.Length; k++)
                {
                    Assert.AreEqual(0, goblins[k].WidthMm, "고블린은 폭이 없다");
                    Assert.GreaterOrEqual(goblins[k].AtMm, start + section * k, $"seed {seed} #{k}");
                    Assert.LessOrEqual(goblins[k].AtMm, start + section * (k + 1) - CardRushRules.GoblinGapMm + t.ObstacleGapMaxMm, $"seed {seed} #{k}");
                }
            }
        }

        [Test]
        public void 출시_표면_고블린은_정확히_다섯()
        {
            var d = CardRushTestTuning.Default;
            var t = new CardRushTuning(180000, 6500, 9500, 350, d.ZoneGapMinMm, d.ZoneGapMaxMm, d.BarricadeWaitTicks,
                                       d.RollKnockMm, d.RollStunTicks, d.ZoneKnockMm, d.ZoneStunTicks, 200, d.PitStunTicks);
            for (ulong seed = 1; seed <= 200; seed++)
                Assert.AreEqual(5, new CardRushTrack(seed, t).Obstacles.Count(x => x.Kind == CardRushObstacleKind.Goblin), $"seed {seed}");
        }

        //  4.5차 리뷰 I1: 짧은 코스·넓은 간격에선 다섯을 다 못 넣는다 — 들어갈 만큼만, 고블린끼리 붙지 않고, 결승 앞 빈터 안에.
        [TestCase(40000, 6000, 6000)]
        [TestCase(60000, 6000, 6000)]
        [TestCase(40000, 6000, 30000)]
        [TestCase(190000, 6000, 40000)]
        [TestCase(180000, 6500, 9500)]
        public void 짧은_코스나_넓은_간격에도_고블린은_붙지_않고_들어갈_만큼만(int trackMm, int gapMin, int gapMax)
        {
            var d = CardRushTestTuning.Default;
            var t = new CardRushTuning(trackMm, gapMin, gapMax, d.BarricadePermille, d.ZoneGapMinMm, d.ZoneGapMaxMm, d.BarricadeWaitTicks,
                                       d.RollKnockMm, d.RollStunTicks, d.ZoneKnockMm, d.ZoneStunTicks, d.PitPermille, d.PitStunTicks);
            int usable = System.Math.Max(0, trackMm - CardRushRules.FinishClearMm - CardRushRules.FirstObstacleMm - gapMax);
            int expected = System.Math.Min(CardRushRules.GoblinsPerTrack, usable / (CardRushRules.GoblinGapMm + gapMax));
            for (ulong seed = 1; seed <= 50; seed++)
            {
                var o = new CardRushTrack(seed, t).Obstacles;
                Assert.AreEqual(expected, o.Count(x => x.Kind == CardRushObstacleKind.Goblin), $"seed {seed}");
                for (int i = 1; i < o.Count; i++)
                    Assert.IsFalse(o[i].Kind == CardRushObstacleKind.Goblin && o[i - 1].Kind == CardRushObstacleKind.Goblin, $"seed {seed} #{i} 고블린 연달아");
                Assert.IsTrue(o.All(x => x.AtMm < trackMm - CardRushRules.FinishClearMm), $"seed {seed} 결승 앞 빈터 밖");
            }
        }

        [Test]
        public void 같은_장애물은_세_번_넘게_잇지_않는다_고블린을_사이에_둬도()
        {
            bool sawPair = false;
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var kinds = Make(seed).Obstacles.Where(x => x.Kind != CardRushObstacleKind.Goblin).Select(x => x.Kind).ToArray();
                for (int i = 2; i < kinds.Length; i++)
                    Assert.IsFalse(kinds[i] == kinds[i - 1] && kinds[i] == kinds[i - 2], $"seed {seed} #{i} — {kinds[i]} 세 번 연속");
                for (int i = 1; i < kinds.Length; i++) sawPair |= kinds[i] == kinds[i - 1];
            }
            Assert.IsTrue(sawPair, "두 번 연속은 나온다 — 막은 것이 셋째부터인지 본다");
        }

        [Test]
        public void 구덩이_비율과_폭()
        {
            int pits = 0, all = 0;
            for (ulong seed = 1; seed <= 200; seed++)
            {
                foreach (var o in Make(seed).Obstacles)
                {
                    if (o.Kind == CardRushObstacleKind.Goblin) continue;
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
            //  같은 종류 셋째를 다른 종류로 바꿔 흔한 쪽(바리케이드)이 줄어든다 — 41% → 47%쯤.
            Assert.That(share, Is.InRange(0.42, 0.52), $"구덩이 {pits}/{all}(고블린 제외) — 바리케이드 500 : 구덩이 350, 세 번 연속 금지 → 47%");
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
        public void 도는_카드는_첫_장_뒤로_빨강_노랑_파랑_차례로_돈다()
        {
            Assert.AreEqual(0, (int)CardColor.Red);
            Assert.AreEqual(1, (int)CardColor.Yellow);
            Assert.AreEqual(2, (int)CardColor.Blue);
            var a = Make(7); var b = Make(7);
            for (int lane = 0; lane < CardRushLanes.MaxLanes; lane++)
            {
                for (int n = 0; n < 10; n++)
                {
                    Assert.AreEqual(a.FirstCard(lane, n), a.CardOf(lane, n, 0), "0장째 = 첫 장");
                    for (int turn = 0; turn < 60; turn++)
                    {
                        var c = a.CardOf(lane, n, turn);
                        Assert.AreEqual(c, b.CardOf(lane, n, turn), "같은 씨앗이면 같은 줄");
                        Assert.AreEqual(CardColors.Next(c), a.CardOf(lane, n, turn + 1), $"lane {lane} n {n} turn {turn} — 다음 장은 차례대로");
                    }
                    Assert.AreEqual(a.CardOf(lane, n, 0), a.CardOf(lane, n, 3), "세 장이면 한 바퀴");
                    Assert.AreEqual(a.CardOf(lane, n, 1), a.CardOf(lane, n, 31), "31장째 = 1장째");
                }
            }
        }

        [Test]
        public void 첫_장은_쓸_때마다_달라질_수_있고_모든_색이_첫_장이_된다()
        {
            var track = Make(7);
            for (int lane = 0; lane < 4; lane++)
            {
                var firsts = new System.Collections.Generic.HashSet<CardColor>();
                for (int n = 0; n < 40; n++) firsts.Add(track.CardOf(lane, n, 0));
                Assert.Greater(firsts.Count, 1, $"lane {lane} — 첫 장이 늘 같은 색이면 박자만 외우면 된다");
            }
            var all = new System.Collections.Generic.HashSet<CardColor>();
            for (int lane = 0; lane < CardRushLanes.MaxLanes; lane++)
                for (int n = 0; n < 40; n++) all.Add(track.CardOf(lane, n, 0));
            Assert.AreEqual(3, all.Count);
        }
    }
}
