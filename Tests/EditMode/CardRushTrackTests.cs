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
            rollKnockMm: 1390, rollStunTicks: 35, zoneKnockMm: 2770, zoneStunTicks: 50);
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
        }

        [Test]
        public void 장애물_간격은_min_max_안()
        {
            var t = CardRushTestTuning.Default;
            bool sawBoth = false;
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var o = Make(seed).Obstacles;
                Assert.Greater(o.Count, 3, $"seed {seed}");
                for (int i = 1; i < o.Count; i++)
                {
                    int gap = o[i].AtMm - o[i - 1].AtMm;
                    Assert.That(gap, Is.InRange(t.ObstacleGapMinMm, t.ObstacleGapMaxMm), $"seed {seed} #{i}");
                    Assert.That(o[i].Variant, Is.InRange(0, 2));
                }
                sawBoth |= o.Any(x => x.Kind == CardRushObstacleKind.Barricade) && o.Any(x => x.Kind == CardRushObstacleKind.Roller);
            }
            Assert.IsTrue(sawBoth);
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
            //  밀리지 않은 첫 구역은 정확히 14 m에서 켜진다 — 50개 중 적어도 하나.
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
        public void 구역_바닥_안에_바리케이드가_없다()
        {
            //  바리케이드 앞에 서는 자리(At − 490)가 구역 판정 범위 [From − 280, To + 280] 안이면 갇힌 채 맞는다(답이 노랑 하나뿐).
            int zonesSeen = 0;
            for (ulong seed = 1; seed <= 200; seed++)
            {
                var track = Make(seed);
                foreach (var z in track.Zones)
                {
                    zonesSeen++;
                    foreach (var b in track.Obstacles.Where(o => o.Kind == CardRushObstacleKind.Barricade))
                    {
                        int stop = b.AtMm - CardRushRules.BarricadeStopMm;
                        Assert.IsFalse(stop >= z.FromMm - CardRushRules.ZoneMarginMm && stop <= z.ToMm + CardRushRules.ZoneMarginMm,
                            $"seed {seed} 구역 [{z.FromMm}, {z.ToMm}] 바리케이드 {b.AtMm}");
                    }
                }
            }
            Assert.Greater(zonesSeen, 200, "구역이 실제로 깔려야 시험이 뜻이 있다");
        }

        [Test]
        public void 경고는_75에서_45로_줄고_뒤_4분의1은_45()
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
            //  첫 구역(14 m)의 경고: 75 − 30 × 14000 / 105000 = 71
            Assert.AreEqual(71, CardRushRules.ZoneWarnStartTicks - (CardRushRules.ZoneWarnStartTicks - CardRushRules.ZoneWarnEndTicks) * 14000 / 105000);
            Assert.IsTrue(Enumerable.Range(1, 50).Select(i => Make((ulong)i).Zones[0]).Any(z => z.TriggerMm == 14000 && z.WarnTicks == 71));
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
            //  구현 첫 통과 때 나온 값을 적어 고정한다(바꾸면 옛 클라와 줄이 갈린다).
            var track = Make(42);
            var obstacles = track.Obstacles.Take(6).Select(o => ((int)o.Kind, o.AtMm, o.Variant)).ToArray();
            var zones = track.Zones.Take(3).Select(z => (z.TriggerMm, z.FromMm, z.ToMm, z.WarnTicks)).ToArray();
            CollectionAssert.AreEqual(ExpectedObstacles, obstacles);
            CollectionAssert.AreEqual(ExpectedZones, zones);
        }

        private static readonly (int, int, int)[] ExpectedObstacles =
        {
            (0, 8000, 2), (0, 26935, 0), (0, 42053, 1), (0, 53663, 0), (1, 71010, 2), (0, 89623, 0),
        };

        private static readonly (int, int, int, int)[] ExpectedZones =
        {
            (14000, 15049, 18076, 71), (46170, 46665, 51192, 62), (95382, 96339, 99088, 48),
        };

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
    }
}
