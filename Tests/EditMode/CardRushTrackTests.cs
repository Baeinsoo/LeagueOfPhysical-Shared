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
        public void 장애물_간격은_min_max_안_세_종류가_다_나온다()
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
                sawAll |= o.Any(x => x.Kind == CardRushObstacleKind.Barricade) && o.Any(x => x.Kind == CardRushObstacleKind.Roller)
                          && o.Any(x => x.Kind == CardRushObstacleKind.Pit);
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
            Assert.That(share, Is.InRange(0.30, 0.40), $"구덩이 {pits}/{all} — 표는 350‰");
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
        public void 첫_장애물과_첫_구역_자리()
        {
            for (ulong seed = 1; seed <= 50; seed++)
            {
                var track = Make(seed);
                Assert.AreEqual(CardRushRules.FirstObstacleMm, track.Obstacles[0].AtMm, $"seed {seed}");
                Assert.GreaterOrEqual(track.Zones[0].TriggerMm, CardRushRules.FirstZoneMm, $"seed {seed}");
            }
            //  틈이 넉넉하면 첫 구역은 정확히 14 m에서 켜진다 — 50개 중 적어도 하나.
            Assert.IsTrue(Enumerable.Range(1, 50).Any(i => Make((ulong)i).Zones[0].TriggerMm == CardRushRules.FirstZoneMm));
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
        public void 구역_안과_구역_끝_3m_안엔_장애물이_없다()
        {
            //  방패가 없어 구역에서 다른 장애물을 만나면 답이 둘 필요하다. 장애물이 선수와 얽히는 구간:
            //  바리케이드 = 앞에 서는 자리 ~ 그 자리, 구덩이 = 빈 바닥, 굴러오는 것 = 풀리는 자리(7 m 앞) ~ 그 자리.
            int zonesSeen = 0;
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var track = Make(seed);
                foreach (var z in track.Zones)
                {
                    zonesSeen++;
                    int lo = z.FromMm - CardRushRules.ZoneMarginMm, hi = z.ToMm + CardRushRules.ZoneEscapeSlackMm;
                    Assert.GreaterOrEqual(z.TriggerMm, 0);
                    foreach (var o in track.Obstacles)
                    {
                        int from = o.Kind == CardRushObstacleKind.Barricade ? o.AtMm - CardRushRules.BarricadeStopMm
                                 : o.Kind == CardRushObstacleKind.Roller ? o.AtMm - CardRushRules.RollerRevealMm
                                 : o.AtMm;
                        int to = o.EndMm;
                        Assert.IsFalse(from <= hi && to >= lo, $"seed {seed} 구역 [{z.FromMm}, {z.ToMm}] 장애물 {o.Kind} {o.AtMm}~{o.EndMm}");
                        //  켜진 동안 지나는 곳(트리거 ~ 구역)에도 없다 — 바리케이드 앞에 갇힌 채 터지지 않게.
                        Assert.IsFalse(from <= z.FromMm && to >= z.TriggerMm, $"seed {seed} 트리거 {z.TriggerMm} 장애물 {o.Kind} {o.AtMm}");
                    }
                }
            }
            Assert.Greater(zonesSeen, 200, "구역이 실제로 깔려야 시험이 뜻이 있다");
        }

        [Test]
        public void 경고는_90에서_60으로_줄고_뒤_4분의1은_60()
        {
            var t = CardRushTestTuning.Default;
            long ramp = (long)t.TrackMm * 3 / 4;
            bool sawLate = false;
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var zones = Make(seed).Zones;
                for (int i = 0; i < zones.Count; i++)
                {
                    var z = zones[i];
                    Assert.That(z.WarnTicks, Is.InRange(CardRushRules.ZoneWarnEndTicks, CardRushRules.ZoneWarnStartTicks));
                    if (i > 0) Assert.LessOrEqual(z.WarnTicks, zones[i - 1].WarnTicks, $"seed {seed} — 뒤 구역일수록 짧다");
                    if (z.TriggerMm >= ramp) { Assert.AreEqual(CardRushRules.ZoneWarnEndTicks, z.WarnTicks); sawLate = true; }
                }
            }
            Assert.IsTrue(sawLate, "뒤 4분의 1의 구역이 하나는 있어야 한다");
            //  14 m에서 켜지는 구역의 경고: 90 − 30 × 14000 / 105000 = 86
            Assert.AreEqual(86, CardRushRules.ZoneWarnStartTicks - (CardRushRules.ZoneWarnStartTicks - CardRushRules.ZoneWarnEndTicks) * 14000 / 105000);
        }

        [Test]
        public void 구역은_서로_겹치지_않고_간격_이상()
        {
            var t = CardRushTestTuning.Default;
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var zones = Make(seed).Zones;
                for (int i = 0; i < zones.Count; i++)
                {
                    var z = zones[i];
                    Assert.That(z.FromMm - z.TriggerMm, Is.InRange(CardRushRules.ZoneAheadMinMm, CardRushRules.ZoneAheadMaxMm));
                    Assert.That(z.ToMm - z.FromMm, Is.InRange(CardRushRules.ZoneLengthMinMm, CardRushRules.ZoneLengthMaxMm));
                    if (i > 0) Assert.GreaterOrEqual(z.TriggerMm - zones[i - 1].ToMm, t.ZoneGapMinMm, $"seed {seed} #{i}");
                }
            }
        }

        [Test]
        public void 뽑는_순서는_계약이다()
        {
            //  4차 구현 첫 통과 때 나온 값을 적어 고정한다(바꾸면 옛 클라와 줄이 갈린다).
            var track = Make(42);
            var obstacles = track.Obstacles.Take(6).Select(o => ((int)o.Kind, o.AtMm, o.Variant, o.WidthMm)).ToArray();
            var zones = track.Zones.Take(3).Select(z => (z.TriggerMm, z.FromMm, z.ToMm, z.WarnTicks)).ToArray();
            CollectionAssert.AreEqual(ExpectedObstacles, obstacles);
            CollectionAssert.AreEqual(ExpectedZones, zones);
        }

        private static readonly (int, int, int, int)[] ExpectedObstacles =
        {
            (0, 8000, 2, 0), (0, 26935, 0, 0), (0, 42053, 1, 0), (0, 53663, 0, 0), (2, 71010, 2, 1318), (0, 87027, 1, 0),
        };

        private static readonly (int, int, int, int)[] ExpectedZones =
        {
            (14000, 14838, 16977, 86), (45071, 45687, 47673, 78), (91863, 92251, 94746, 64),
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
        public void 구덩이_바로_뒤_구역은_밀려나도_구덩이_위로_돌아가지_않는다()
        {
            var t = CardRushTestTuning.Default;
            int checkedZones = 0;
            for (ulong seed = 1; seed <= 400; seed++)
            {
                var track = new CardRushTrack(seed, t);
                var obs = track.Obstacles.ToArray();
                foreach (var z in track.Zones.ToArray())
                {
                    for (int i = 0; i < obs.Length; i++)
                    {
                        if (obs[i].Kind != CardRushObstacleKind.Pit || obs[i].EndMm > z.TriggerMm) continue;
                        if (i + 1 < obs.Length && obs[i + 1].AtMm < z.TriggerMm) continue;
                        checkedZones++;
                        int worst = z.FromMm - CardRushRules.ZoneMarginMm - t.ZoneKnockMm;
                        Assert.GreaterOrEqual(worst, obs[i].EndMm, $"씨앗 {seed}: 구역에 맞아 밀려나면 구덩이 위로 돌아간다");
                    }
                }
            }
            Assert.Greater(checkedZones, 20, "구덩이 뒤 구역이 충분히 표본에 든다");
        }
    }
}
