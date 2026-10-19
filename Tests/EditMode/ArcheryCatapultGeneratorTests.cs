using System.Collections.Generic;
using System.Linq;
using GameFramework.Rng;
using NUnit.Framework;
using UnityEngine;
using static LOP.Tests.ArcheryCatapultFixture;

namespace LOP.Tests
{
    public class ArcheryCatapultGeneratorTests
    {
        private static List<ArcheryTarget> Fill(ulong seed, int volley, int players = 2, ArcheryConfig config = null)
        {
            var list = new List<ArcheryTarget>();
            ArcheryCatapultGenerator.Fill(list, seed, volley, config ?? Config(), players, LaneOrigin, Forward, StartTick, Dt);
            return list;
        }

        private static float Lateral(Vector3 p) => Vector3.Dot(p - LaneOrigin, Right);
        private static float Depth(Vector3 p) => Vector3.Dot(p - LaneOrigin, Forward);
        private static bool IsNormal(in ArcheryTarget t) => t.IsTrap == false && t.IsJackpot == false;

        //  시험 쪽에서 뽑기를 따로 적는다 — 생성기와 같은 코드를 부르면 순서 계약을 못 잰다.
        private static ArcheryTargetKind ExpectedPick(IReadOnlyList<ArcheryTargetKind> pool, ref DeterministicRandom rng)
        {
            int total = 0;
            foreach (var k in pool) { total += Mathf.Max(k.Weight, 0); }
            int roll = rng.Range(0, total);
            foreach (var k in pool)
            {
                roll -= Mathf.Max(k.Weight, 0);
                if (roll < 0) { return k; }
            }
            return pool[pool.Count - 1];
        }

        private struct Drawn
        {
            public ArcheryTargetKind Kind;
            public float Distance, Height, Lateral, Direction, Speed, Arc, Climb, Phase;
        }

        //  계약: 종류 → 거리 → 시작 높이 → 가로 위치 → 방향 → 가로 속도 → 호 → 솟음 → 위상(늘 아홉 번).
        private static Drawn ExpectedDraw(IReadOnlyList<ArcheryTargetKind> pool, ref DeterministicRandom rng)
        {
            var d = new Drawn { Kind = ExpectedPick(pool, ref rng) };
            var m = d.Kind.Motion;
            d.Distance = rng.Range(d.Kind.DistanceMinM, d.Kind.DistanceMaxM);
            d.Height = rng.Range(m.HeightMinM, m.HeightMaxM);
            d.Lateral = rng.Range(-1f, 1f);
            d.Direction = rng.NextFloat01() < 0.5f ? -1f : 1f;
            d.Speed = rng.Range(d.Kind.DriftMinMps, d.Kind.DriftMaxMps);
            d.Arc = rng.Range(m.ArcMinM, m.ArcMaxM);
            d.Climb = rng.Range(m.ClimbMinMps, m.ClimbMaxMps);
            d.Phase = rng.NextFloat01();
            return d;
        }

        //  거울 +1 쪽(짝 A·외톨이·대박·금지물)과 맞는지. half = 그 종류의 가로 범위.
        private static void AssertMatches(in Drawn d, in ArcheryTarget t, float half, string label)
        {
            Assert.AreEqual(d.Kind.Id, t.KindId, label);
            Assert.AreEqual(d.Distance, Depth(t.Origin), 1e-4f, label);
            Assert.AreEqual(d.Height, t.Origin.y, 1e-4f, label);
            Assert.AreEqual(d.Direction * d.Speed, Vector3.Dot(t.Drift, Right), 1e-4f, label);
            Assert.AreEqual(d.Arc, t.ArcHeight, 1e-5f, label);
            Assert.AreEqual(d.Climb, t.Climb, 1e-5f, label);
            Assert.AreEqual(d.Phase, t.MotionPhase, 1e-6f, label);
            float travel = d.Speed * Settings().TargetLifeTicks * Dt;
            float start = d.Lateral * Mathf.Max(0f, half - travel * 0.5f) - d.Direction * travel * 0.5f;
            Assert.AreEqual(start, Lateral(t.Origin), 1e-3f, label);
        }

        [Test]
        public void 난수를_꺼내는_순서가_계약대로다()
        {
            //  다섯 명 = 일반 셋(짝 하나 + 외톨이 하나). 대박·금지물이 둘 다 낀 무리까지 끝까지 잰다.
            var config = Config();
            var s = config.Catapult;
            const int players = 5;
            const int volley = 3;
            int full = 0;
            for (ulong seed = 1; seed <= 60; seed++)
            {
                var rng = new DeterministicRandom(Hashing.Combine(Hashing.Combine(seed, ArcheryCatapultGenerator.Salt), (ulong)volley));
                bool trap = rng.NextFloat01() < s.TrapChance;
                bool jackpot = rng.NextFloat01() < s.JackpotChance;
                var pair = ExpectedDraw(config.NormalKinds, ref rng);
                var lone = ExpectedDraw(config.NormalKinds, ref rng);

                var targets = Fill(seed, volley, players, config);
                AssertMatches(pair, targets[0], 16f, $"씨앗 {seed} 짝 A");
                Assert.AreEqual(1, targets[1].SlotIndex);
                AssertMatches(lone, targets[2], 16f, $"씨앗 {seed} 외톨이");
                int at = 3;
                if (jackpot)
                {
                    var j = ExpectedDraw(config.JackpotKinds, ref rng);
                    Assert.AreEqual(ArcheryCatapultGenerator.JackpotSlot, targets[at].SlotIndex);
                    AssertMatches(j, targets[at], 8f, $"씨앗 {seed} 대박");
                    at++;
                }
                if (trap)
                {
                    var x = ExpectedDraw(config.TrapKinds, ref rng);
                    Assert.AreEqual(ArcheryCatapultGenerator.TrapSlot, targets[at].SlotIndex);
                    AssertMatches(x, targets[at], 10f, $"씨앗 {seed} 금지물");
                    at++;
                }
                Assert.AreEqual(at, targets.Count, $"씨앗 {seed}");
                if (trap && jackpot) { full++; }
            }
            Assert.Greater(full, 0, "대박·금지물이 함께 낀 무리를 하나도 못 쟀다 — 씨앗 범위를 늘릴 것");
        }

        [TestCase(0, 2)]
        [TestCase(1, 2)]
        [TestCase(2, 2)]
        [TestCase(3, 2)]
        [TestCase(4, 2)]
        [TestCase(5, 3)]
        [TestCase(6, 3)]
        [TestCase(7, 4)]
        [TestCase(8, 4)]
        [TestCase(12, 6)]
        [TestCase(13, 6)]
        [TestCase(20, 6)]
        public void 일반_표적_수는_두_명에_하나_꼴이고_둘보다_적지_않다(int players, int expected)
        {
            Assert.AreEqual(expected, ArcheryCatapultGenerator.NormalCountFor(players));
            for (int v = 0; v < Settings().VolleyCount; v++)
            {
                Assert.AreEqual(expected, Fill(4UL, v, players).Count(t => IsNormal(t)), $"무리 {v}");
            }
        }

        [Test]
        public void 짝은_거울이고_홀수면_마지막_하나만_뜬다()
        {
            for (ulong seed = 1; seed <= 20; seed++)
            {
                for (int v = 0; v < Settings().VolleyCount; v++)
                {
                    var normals = Fill(seed, v, players: 7).Where(t => IsNormal(t)).ToList();   // 일반 넷
                    CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, normals.Select(t => t.SlotIndex).ToArray());
                    for (int k = 0; k < 4; k += 2)
                    {
                        var a = normals[k];
                        var b = normals[k + 1];
                        Assert.AreEqual(a.KindId, b.KindId);
                        Assert.AreEqual(a.MotionPhase, b.MotionPhase);
                        Assert.AreEqual(a.ArcHeight, b.ArcHeight);
                        Assert.AreEqual(a.Climb, b.Climb);
                        Assert.AreEqual(-Lateral(a.Origin), Lateral(b.Origin), 1e-4f, $"씨앗 {seed} 무리 {v} 짝 {k / 2}");
                        Assert.AreEqual(Depth(a.Origin), Depth(b.Origin), 1e-4f);
                        Assert.AreEqual(a.Origin.y, b.Origin.y, 1e-4f);
                        //  날아가는 내내(출렁·솟음·회전까지) 거울이다.
                        double mid = a.SpawnTick + Settings().TargetLifeTicks / 2;
                        Vector3 pa = ArcheryTargetMotion.PositionAt(a, mid, Dt), pb = ArcheryTargetMotion.PositionAt(b, mid, Dt);
                        Assert.AreEqual(-Lateral(pa), Lateral(pb), 1e-3f);
                        Assert.AreEqual(pa.y, pb.y, 1e-4f);
                        Assert.AreEqual(ArcheryTargetMotion.RadiusAt(a, mid, Dt), ArcheryTargetMotion.RadiusAt(b, mid, Dt));
                    }
                    var odd = Fill(seed, v, players: 5).Where(t => IsNormal(t)).ToList();       // 일반 셋
                    CollectionAssert.AreEqual(new[] { 0, 1, 2 }, odd.Select(t => t.SlotIndex).ToArray());
                }
            }
        }

        [Test]
        public void 같은_씨앗_같은_인원이면_같은_목록이다()
        {
            for (int v = 0; v < Settings().VolleyCount; v++)
            {
                var x = Fill(77UL, v, 8);
                var y = Fill(77UL, v, 8);
                Assert.AreEqual(x.Count, y.Count, $"무리 {v}");
                for (int i = 0; i < x.Count; i++)
                {
                    Assert.AreEqual(x[i].SlotIndex, y[i].SlotIndex);
                    Assert.AreEqual(x[i].Origin, y[i].Origin);
                    Assert.AreEqual(x[i].Drift, y[i].Drift);
                    Assert.AreEqual(x[i].KindId, y[i].KindId);
                    Assert.AreEqual(x[i].ArcHeight, y[i].ArcHeight);
                    Assert.AreEqual(x[i].Climb, y[i].Climb);
                    Assert.AreEqual(x[i].MotionPhase, y[i].MotionPhase);
                    Assert.AreEqual(x[i].SpawnTick, y[i].SpawnTick);
                }
            }
        }

        [Test]
        public void 인원이_달라도_앞쪽_짝은_같다()
        {
            //  여부 둘을 맨 앞에 뽑으므로 짝 0은 인원과 상관없다 — 계약의 이유를 지킨다.
            for (int v = 0; v < Settings().VolleyCount; v++)
            {
                var two = Fill(9UL, v, 2);
                var eight = Fill(9UL, v, 8);
                Assert.AreEqual(two[0].Origin, eight[0].Origin, $"무리 {v}");
                Assert.AreEqual(two[1].Drift, eight[1].Drift, $"무리 {v}");
            }
        }

        [Test]
        public void 대박은_무리의_약_28퍼센트이고_종과_피냐타가_둘_다_나온다()
        {
            int volleys = 0, jackpots = 0;
            var kinds = new HashSet<int>();
            for (ulong seed = 1; seed <= 20; seed++)
            {
                for (int v = 0; v < Settings().VolleyCount; v++, volleys++)
                {
                    var j = Fill(seed, v).Where(t => t.IsJackpot).ToList();
                    Assert.LessOrEqual(j.Count, 1);
                    if (j.Count == 0) { continue; }
                    jackpots++;
                    kinds.Add(j[0].KindId);
                    Assert.AreEqual(ArcheryCatapultGenerator.JackpotSlot, j[0].SlotIndex);
                    Assert.AreEqual(5, j[0].Points);
                    Assert.IsFalse(j[0].IsTrap);
                }
            }
            Assert.That(jackpots / (float)volleys, Is.InRange(0.22f, 0.34f));   // 360무리, 기대 0.28
            CollectionAssert.AreEquivalent(new[] { 18, 19 }, kinds);
        }

        [Test]
        public void 금지물은_무리의_약_35퍼센트이고_셋_모두_사격_금지를_달고_나온다()
        {
            int volleys = 0, traps = 0;
            var kinds = new HashSet<int>();
            for (ulong seed = 1; seed <= 20; seed++)
            {
                for (int v = 0; v < Settings().VolleyCount; v++, volleys++)
                {
                    var x = Fill(seed, v).Where(t => t.IsTrap).ToList();
                    Assert.LessOrEqual(x.Count, 1);
                    if (x.Count == 0) { continue; }
                    traps++;
                    kinds.Add(x[0].KindId);
                    Assert.AreEqual(ArcheryCatapultGenerator.TrapSlot, x[0].SlotIndex);
                    Assert.AreEqual(75, x[0].StunTicks);
                    Assert.That(Depth(x[0].Origin), Is.InRange(32f, 48f));
                }
            }
            Assert.That(traps / (float)volleys, Is.InRange(0.29f, 0.41f));
            CollectionAssert.AreEquivalent(new[] { 20, 21, 22 }, kinds);
        }

        [Test]
        public void 종류의_움직임이_표적에_옮겨진다()
        {
            var seen = new HashSet<int>();
            for (ulong seed = 1; seed <= 10; seed++)
            {
                for (int v = 0; v < Settings().VolleyCount; v++)
                {
                    foreach (var t in Fill(seed, v, 8))
                    {
                        seen.Add(t.KindId);
                        string k = $"종류 {t.KindId}";
                        switch (t.KindId)
                        {
                            case 14:   // 물풍선 — 출렁
                                Assert.AreEqual(0.6f, t.BobAmplitude, k); Assert.AreEqual(0.48f, t.BobHz, k);
                                Assert.AreEqual(0f, t.ArcHeight, k); Assert.AreEqual(0f, t.Climb, k); Assert.AreEqual(0f, t.SpinHz, k);
                                break;
                            case 15:   // 수박 — 높은 포물선
                                Assert.That(t.ArcHeight, Is.InRange(3f, 4.5f), k); Assert.That(t.Origin.y, Is.InRange(2.5f, 5f), k);
                                Assert.AreEqual(0f, t.BobAmplitude, k);
                                break;
                            case 16:   // 접시 — 회전
                                Assert.AreEqual(1.1f, t.SpinHz, k); Assert.AreEqual(0f, t.ArcHeight, k);
                                break;
                            case 17:   // 로켓 — 솟음
                                Assert.That(t.Climb, Is.InRange(3.5f, 5f), k); Assert.That(t.Origin.y, Is.InRange(1f, 3f), k);
                                break;
                            case 18:
                            case 19:   // 대박 — 느린 출렁
                                Assert.AreEqual(0.3f, t.BobAmplitude, k); Assert.AreEqual(0.32f, t.BobHz, k); Assert.IsTrue(t.IsJackpot, k);
                                break;
                            default:   // 금지물 — 옆으로만
                                Assert.IsTrue(t.IsTrap, k); Assert.AreEqual(0f, t.BobAmplitude + t.Climb + t.SpinHz + t.ArcHeight, k);
                                break;
                        }
                    }
                }
            }
            Assert.AreEqual(9, seen.Count, "열 씨앗 동안 아홉 종류가 다 나와야 한다");
        }

        [Test]
        public void 표적의_길_전체가_자기_가로_범위_안이고_30미터보다_멀다()
        {
            for (ulong seed = 1; seed <= 30; seed++)
            {
                for (int v = 0; v < Settings().VolleyCount; v++)
                {
                    foreach (var t in Fill(seed, v, 8))
                    {
                        float half = t.IsJackpot ? 8f : t.IsTrap ? 10f : 16f;
                        double end = t.SpawnTick + t.LifetimeSeconds / Dt;
                        Assert.LessOrEqual(Mathf.Abs(Lateral(t.Origin)), half + 1e-3f);
                        Assert.LessOrEqual(Mathf.Abs(Lateral(ArcheryTargetMotion.PositionAt(t, end, Dt))), half + 1e-3f);
                        Assert.GreaterOrEqual(Depth(t.Origin), 30f);
                    }
                }
            }
        }

        [Test]
        public void 대박과_금지물_데이터가_없어도_일반_표적은_그대로다()
        {
            //  일반 뽑기가 대박·금지물 뽑기보다 앞이므로 그 데이터가 바뀌어도 짝은 안 흔들린다.
            for (int v = 0; v < Settings().VolleyCount; v++)
            {
                var with = Fill(11UL, v, 6, Config());
                var without = Fill(11UL, v, 6, Config(withTraps: false, withJackpots: false));
                Assert.AreEqual(3, without.Count);
                for (int i = 0; i < 3; i++)
                {
                    Assert.AreEqual(with[i].Origin, without[i].Origin);
                    Assert.AreEqual(with[i].Drift, without[i].Drift);
                }
            }
        }

        [Test]
        public void 표적은_발사_틱에_떠서_3점6초_살고_주인이_없다()
        {
            var t = Fill(5UL, 0)[0];
            Assert.AreEqual(1115L, t.SpawnTick);
            Assert.AreEqual(3.6f, t.LifetimeSeconds, 1e-4f);
            Assert.AreEqual(string.Empty, t.OwnerUserId);
            Assert.IsFalse(t.IsShared);
            Assert.IsTrue(ArcheryHitRules.ConsumedOnHit(t));
            Assert.AreEqual(-Forward, t.Facing);
        }

        [Test]
        public void 무리_번호가_범위_밖이면_비어_있다()
        {
            Assert.AreEqual(0, Fill(1UL, -1).Count);
            Assert.AreEqual(0, Fill(1UL, Settings().VolleyCount).Count);
        }

        //  ---- 공정성 (1차 spec §3.3) ----

        //  자리별 평균 거리를 판 하나(18무리) 단위로 잰다. 수명 동안 5번 재서 평균. 금지물은 노릴 표적이 아니라 뺀다.
        private static float WorstSeatGap(int seats, float spacing, ulong seed, ArcheryConfig config)
        {
            var at = new Vector3[seats];
            for (int i = 0; i < seats; i++)
            {
                at[i] = LaneOrigin + Right * ((i - (seats - 1) * 0.5f) * spacing) + Vector3.up * ArcheryAimSystem.EyeHeight;
            }
            var sum = new double[seats];
            int samples = 0;
            var list = new List<ArcheryTarget>();
            for (int v = 0; v < config.Catapult.VolleyCount; v++)
            {
                ArcheryCatapultGenerator.Fill(list, seed, v, config, seats, LaneOrigin, Forward, StartTick, Dt);
                foreach (var t in list)
                {
                    if (t.IsTrap) { continue; }
                    for (int k = 0; k < 5; k++)
                    {
                        double tick = t.SpawnTick + (k + 0.5) / 5.0 * t.LifetimeSeconds / Dt;
                        Vector3 p = ArcheryTargetMotion.PositionAt(t, tick, Dt);
                        for (int s = 0; s < seats; s++) { sum[s] += Vector3.Distance(at[s], p); }
                        samples++;
                    }
                }
            }
            double min = double.MaxValue, max = double.MinValue;
            for (int s = 0; s < seats; s++)
            {
                double mean = sum[s] / samples;
                min = System.Math.Min(min, mean);
                max = System.Math.Max(max, mean);
            }
            return (float)(max - min);
        }

        [Test]
        public void 두_명에서_여덟_명까지_자리별_평균_거리_차가_반_미터_안이다()
        {
            var config = Config();
            for (int seats = 2; seats <= 8; seats++)
            {
                for (ulong seed = 1; seed <= 50; seed++)
                {
                    Assert.Less(WorstSeatGap(seats, ArcheryShootOffSeats.SpacingMeters, seed, config), 0.5f, $"{seats}명 씨앗 {seed}");
                }
            }
        }

        [Test]
        public void 간격을_3미터로_벌리면_같은_재기가_반_미터를_넘는다()
        {
            //  양성 대조 — 재는 방법이 실제로 불공정을 잡는지. (계산상 약 1.25 m)
            Assert.Greater(WorstSeatGap(8, 3f, 1UL, Config()), 0.5f);
        }

        [Test]
        public void 대박이_없으면_두_자리는_거울_짝이라_평균_거리가_똑같다()
        {
            //  대박은 짝이 없어 두 자리 사이에 아주 작은 차를 만든다(계산상 0.02 m) — 그것을 빼면 정확히 같아야 한다.
            Assert.Less(WorstSeatGap(2, ArcheryShootOffSeats.SpacingMeters, 3UL, Config(withJackpots: false)), 1e-3f);
        }
    }
}
