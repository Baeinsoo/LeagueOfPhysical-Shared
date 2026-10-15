using System.Collections.Generic;
using GameFramework.Rng;
using NUnit.Framework;
using UnityEngine;
using static LOP.Tests.ArcheryCatapultFixture;

namespace LOP.Tests
{
    public class ArcheryCatapultGeneratorTests
    {
        private static List<ArcheryTarget> Fill(ulong seed, int volley, ArcheryConfig config = null)
        {
            var list = new List<ArcheryTarget>();
            ArcheryCatapultGenerator.Fill(list, seed, volley, config ?? Config(), LaneOrigin, Forward, StartTick, Dt);
            return list;
        }

        private static float Lateral(Vector3 p) => Vector3.Dot(p - LaneOrigin, Right);
        private static float Depth(Vector3 p) => Vector3.Dot(p - LaneOrigin, Forward);

        //  시험 쪽에서 가중치 뽑기를 따로 적는다 — 생성기와 같은 코드를 부르면 순서 계약을 못 잰다.
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

        [Test]
        public void 난수를_꺼내는_순서가_계약대로다()
        {
            var config = Config();
            const ulong seed = 0xC0FFEEUL;
            const int volley = 3;
            var rng = new DeterministicRandom(Hashing.Combine(Hashing.Combine(seed, ArcheryCatapultGenerator.Salt), (ulong)volley));

            bool trap = rng.NextFloat01() < config.Catapult.TrapChance;
            var kind = ExpectedPick(config.CleanKinds, ref rng);
            float distance = rng.Range(kind.DistanceMinM, kind.DistanceMaxM);
            float height = rng.Range(config.SpawnMinY, config.SpawnMaxY);
            float lateral = rng.Range(-1f, 1f);
            float direction = rng.NextFloat01() < 0.5f ? -1f : 1f;
            float speed = rng.Range(kind.DriftMinMps, kind.DriftMaxMps);
            float arc = rng.Range(config.RiseHeightMin, config.RiseHeightMax);

            var targets = Fill(seed, volley, config);
            var a = targets[0];
            Assert.AreEqual(kind.Id, a.KindId);
            Assert.AreEqual(distance, Depth(a.Origin), 1e-4f);
            Assert.AreEqual(height, a.Origin.y, 1e-4f);
            Assert.AreEqual(direction * speed, Vector3.Dot(a.Drift, Right), 1e-4f);
            Assert.AreEqual(arc, a.ArcHeight, 1e-5f);
            //  가로 위치 u는 길의 가운데를 정한다 — 출발점 = u·(16 − 이동/2) − 방향·이동/2.
            float travel = speed * config.Catapult.TargetLifeTicks * Dt;
            float start = lateral * Mathf.Max(0f, 16f - travel * 0.5f) - direction * travel * 0.5f;
            Assert.AreEqual(start, Lateral(a.Origin), 1e-3f);
            Assert.AreEqual(trap ? 3 : 2, targets.Count);
        }

        [Test]
        public void 같은_씨앗이면_같은_목록이다()
        {
            //  클·서가 각자 부르는 상황 — 새 목록, 같은 입력.
            for (int v = 0; v < 21; v++)
            {
                var x = Fill(77UL, v);
                var y = Fill(77UL, v);
                Assert.AreEqual(x.Count, y.Count, $"무리 {v}");
                for (int i = 0; i < x.Count; i++)
                {
                    Assert.AreEqual(x[i].Origin, y[i].Origin);
                    Assert.AreEqual(x[i].Drift, y[i].Drift);
                    Assert.AreEqual(x[i].KindId, y[i].KindId);
                    Assert.AreEqual(x[i].ArcHeight, y[i].ArcHeight);
                    Assert.AreEqual(x[i].SpawnTick, y[i].SpawnTick);
                }
            }
        }

        [Test]
        public void 다른_씨앗이면_목록이_달라진다()
        {
            int differ = 0;
            for (int v = 0; v < 21; v++)
            {
                if (Fill(1UL, v)[0].Origin != Fill(2UL, v)[0].Origin) { differ++; }
            }
            Assert.Greater(differ, 15);
        }

        [Test]
        public void 성한_짝은_레인_중심을_두고_정확히_거울이다()
        {
            for (ulong seed = 1; seed <= 20; seed++)
            {
                for (int v = 0; v < 21; v++)
                {
                    var t = Fill(seed, v);
                    var a = t[ArcheryCatapultGenerator.PairSlotA];
                    var b = t[ArcheryCatapultGenerator.PairSlotB];
                    Assert.AreEqual(a.KindId, b.KindId);
                    Assert.AreEqual(a.SpawnTick, b.SpawnTick);
                    Assert.AreEqual(a.ArcHeight, b.ArcHeight);
                    Assert.AreEqual(-Lateral(a.Origin), Lateral(b.Origin), 1e-4f, $"씨앗 {seed} 무리 {v}");
                    Assert.AreEqual(Depth(a.Origin), Depth(b.Origin), 1e-4f);
                    Assert.AreEqual(a.Origin.y, b.Origin.y, 1e-4f);
                    Assert.AreEqual(-Vector3.Dot(a.Drift, Right), Vector3.Dot(b.Drift, Right), 1e-4f);
                    //  날아가는 내내 거울이다.
                    double mid = a.SpawnTick + 65;
                    Assert.AreEqual(-Lateral(ArcheryTargetMotion.PositionAt(a, mid, Dt)),
                                    Lateral(ArcheryTargetMotion.PositionAt(b, mid, Dt)), 1e-3f);
                }
            }
        }

        [Test]
        public void 표적의_길_전체가_가로_16미터_안이고_30미터보다_멀다()
        {
            for (ulong seed = 1; seed <= 30; seed++)
            {
                for (int v = 0; v < 21; v++)
                {
                    foreach (var t in Fill(seed, v))
                    {
                        double end = t.SpawnTick + t.LifetimeSeconds / Dt;
                        Assert.LessOrEqual(Mathf.Abs(Lateral(t.Origin)), 16f + 1e-3f);
                        Assert.LessOrEqual(Mathf.Abs(Lateral(ArcheryTargetMotion.PositionAt(t, end, Dt))), 16f + 1e-3f);
                        Assert.GreaterOrEqual(Depth(t.Origin), 30f);
                    }
                }
            }
        }

        [Test]
        public void 표적은_발사_틱에_떠서_2점6초_살고_주인이_없다()
        {
            var t = Fill(5UL, 0)[0];
            Assert.AreEqual(1115L, t.SpawnTick);
            Assert.AreEqual(2.6f, t.LifetimeSeconds, 1e-4f);
            Assert.AreEqual(string.Empty, t.OwnerUserId);
            Assert.IsFalse(t.IsShared);
            Assert.IsTrue(ArcheryHitRules.ConsumedOnHit(t));   // 먼저 맞힌 사람이 먹고 사라진다
            Assert.AreEqual(-Forward, t.Facing);

            //  판정·화면은 LiveVolleyAt으로 목록을 고른다 — 뜨는 틱부터 수명 마지막 틱까지 같은 무리를 가리켜야
            //  표적이 끝까지 살아 있다(준비 기준 VolleyIndexAt을 쓰면 마지막 5틱이 잘린다).
            var s = Settings();
            for (int v = 0; v < s.VolleyCount; v++)
            {
                var first = Fill(5UL, v)[0];
                long lastAlive = first.SpawnTick + s.TargetLifeTicks - 1;
                Assert.AreEqual(v, ArcheryCatapultSchedule.LiveVolleyAt(s, first.SpawnTick, StartTick), $"무리 {v} 뜨는 틱");
                Assert.AreEqual(v, ArcheryCatapultSchedule.LiveVolleyAt(s, lastAlive, StartTick), $"무리 {v} 마지막 틱");
            }
        }

        [Test]
        public void 쏘면_안_되는_것은_세_번째_자리에_사격_금지를_달고_뜬다()
        {
            int found = 0;
            for (int v = 0; v < 21; v++)
            {
                var t = Fill(9UL, v);
                Assert.IsFalse(t[0].IsTrap);
                Assert.IsFalse(t[1].IsTrap);
                if (t.Count == 3)
                {
                    found++;
                    Assert.AreEqual(ArcheryCatapultGenerator.TrapSlot, t[2].SlotIndex);
                    Assert.IsTrue(t[2].IsTrap);
                    Assert.AreEqual(75, t[2].StunTicks);
                    Assert.That(Depth(t[2].Origin), Is.InRange(32f, 50f));
                }
            }
            //  35%라 21무리에 대략 7번 — 하나도 없으면 확률 뽑기가 죽은 것이다.
            Assert.Greater(found, 0);
            Assert.Less(found, 21);
        }

        [Test]
        public void 쏘면_안_되는_종류가_없어도_짝은_그대로다()
        {
            //  함정 뽑기가 짝 뒤에 있으므로 함정 데이터가 달라도 짝은 안 흔들린다.
            for (int v = 0; v < 21; v++)
            {
                var with = Fill(11UL, v, Config(withTraps: true));
                var without = Fill(11UL, v, Config(withTraps: false));
                Assert.AreEqual(2, without.Count);
                Assert.AreEqual(with[0].Origin, without[0].Origin);
                Assert.AreEqual(with[1].Drift, without[1].Drift);
            }
        }

        [Test]
        public void 무리_번호가_범위_밖이면_비어_있다()
        {
            Assert.AreEqual(0, Fill(1UL, -1).Count);
            Assert.AreEqual(0, Fill(1UL, 21).Count);
        }

        //  ---- 공정성 (spec §3.3) ----

        //  자리별 평균 거리를 판 하나(21무리) 단위로 잰다. 수명 동안 5번 재서 평균 — 옆으로 나는 동안 거리가 바뀐다.
        //  쏘면 안 되는 것은 노릴 표적이 아니라 빼고 잰다.
        private static float WorstSeatGap(int seats, float spacing, ulong seed)
        {
            var config = Config();
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
                ArcheryCatapultGenerator.Fill(list, seed, v, config, LaneOrigin, Forward, StartTick, Dt);
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
        public void 여덟_자리의_표적_평균_거리_차가_반_미터_안이다()
        {
            for (ulong seed = 1; seed <= 50; seed++)
            {
                Assert.Less(WorstSeatGap(8, ArcheryShootOffSeats.SpacingMeters, seed), 0.5f, $"씨앗 {seed}");
            }
        }

        [Test]
        public void 간격을_3미터로_벌리면_같은_재기가_반_미터를_넘는다()
        {
            //  양성 대조 — 재는 방법이 실제로 불공정을 잡는지. (계산상 약 1.2 m)
            Assert.Greater(WorstSeatGap(8, 3f, 1UL), 0.5f);
        }

        [Test]
        public void 두_자리는_거울_짝이라_평균_거리가_똑같다()
        {
            Assert.Less(WorstSeatGap(2, ArcheryShootOffSeats.SpacingMeters, 3UL), 1e-3f);
        }
    }
}
