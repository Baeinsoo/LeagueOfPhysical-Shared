using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 과녁이 언제 어디 서는지를 정하는 한 곳. <b>사거리 맵은 난수를 판 시작에 딱 한 번</b>
    /// (순서 뽑기) 쓰고 그 뒤는 전부 산수다 — 그래서 여기 시험은 대부분 "산수가 맞나"를 잰다.
    /// </summary>
    public class ArcheryCourseTests
    {
        private sealed class FixedSeed : IMatchSeed
        {
            public FixedSeed(ulong value) { Value = value; }
            public ulong Value { get; }
        }

        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned) { Object.DestroyImmediate(go); }
            spawned.Clear();
        }

        //  레인 둘, 자리 셋. 사대는 원점과 x=4, 과녁은 앞(+z)으로 10/20/30m.
        private ArcheryRangeLayout Layout(int laneCount = 2, int standCount = 3)
        {
            var lanes = new List<ArcheryLane>();
            for (int i = 0; i < laneCount; i++)
            {
                var root = new GameObject("lane" + i);
                spawned.Add(root);
                root.transform.position = new Vector3(i * 4f, 0f, 0f);
                var lane = root.AddComponent<ArcheryLane>();
                lane.Order = i;
                lane.Stands = new Transform[standCount];
                for (int s = 0; s < standCount; s++)
                {
                    var stand = new GameObject("stand" + s);
                    stand.transform.SetParent(root.transform);
                    stand.transform.localPosition = new Vector3(0f, 1.3f, 10f * (s + 1));
                    lane.Stands[s] = stand.transform;
                }
                lanes.Add(lane);
            }
            return ArcheryRangeLayout.From(lanes);
        }

        private static ArcheryTargetKind FaceKind()
        {
            var bands = new List<ArcheryRingBand>
            {
                new ArcheryRingBand(0.2f, 10),
                new ArcheryRingBand(0.5f, 8),
                new ArcheryRingBand(1.0f, 5),
            };
            return new ArcheryTargetKind(0.61f, 5, 0, false, ArcheryTargetShape.Face, bands);
        }

        //  자리마다 노출이 다르다 — 단계 경계가 누적합이라는 것을 재려면 달라야 한다.
        private static ArcheryConfig RangeConfig(int standCount = 3, float lateralSpan = 0f,
                                                 float lateralPeriod = 0f, int arrowsPerStand = 1)
        {
            var stands = new List<ArcheryRangeStand>();
            for (int s = 0; s < standCount; s++)
            {
                stands.Add(new ArcheryRangeStand(s, 10f * (s + 1), 100 + s * 50, lateralSpan, lateralPeriod));
            }
            var range = new ArcheryRangeSettings(FaceKind(), stands, stepGapTicks: 25, arrowsPerStand);

            return new ArcheryConfig(120, 3, 5, 3.5f, 0.3f, 0.6f, 1.2f, 0f, 1f,
                                     1.2f, 2.5f, 0f, 1.2f, 2.4f, 12, 20,
                                     new List<ArcheryTargetKind> { FaceKind() },
                                     ArcheryCourseKind.Range, 0, range);
        }

        private ArcheryCourse RangeCourse(ulong seed = 777UL, int laneCount = 2, int standCount = 3,
                                          float lateralSpan = 0f, float lateralPeriod = 0f,
                                          int arrowsPerStand = 1)
        {
            var layout = Layout(laneCount, standCount);
            return new ArcheryCourse(RangeConfig(standCount, lateralSpan, lateralPeriod, arrowsPerStand),
                                     new FixedSeed(seed),
                                     new[] { "user-a", "user-b" }, 0.02f, () => layout);
        }

        [Test]
        public void 같은_씨앗이면_같은_순서가_나온다()
        {
            var a = new List<ArcheryTarget>();
            var b = new List<ArcheryTarget>();
            RangeCourse(seed: 42UL).Fill(a, 0, 1000L);
            RangeCourse(seed: 42UL).Fill(b, 0, 1000L);

            Assert.AreEqual(a[0].Origin, b[0].Origin, "같은 씨앗인데 첫 과녁이 다른 자리에 섰다");
        }

        [Test]
        public void 다른_씨앗이면_순서가_달라진다()
        {
            //  자리가 셋뿐이라 우연히 같은 순열이 나올 수 있다 — 여러 씨앗 중 하나라도 달라지면 된다.
            var baseline = new List<ArcheryTarget>();
            RangeCourse(seed: 1UL).Fill(baseline, 0, 0L);

            bool anyDifferent = false;
            for (ulong seed = 2UL; seed <= 12UL; seed++)
            {
                var other = new List<ArcheryTarget>();
                RangeCourse(seed).Fill(other, 0, 0L);
                if (other[0].Origin != baseline[0].Origin) { anyDifferent = true; break; }
            }
            Assert.IsTrue(anyDifferent, "씨앗을 열한 번 바꿔도 첫 과녁이 늘 같은 자리다 — 순서를 안 뽑고 있다");
        }

        [Test]
        public void 모든_거리가_정확히_한_번씩_쓰인다()
        {
            var course = RangeCourse(standCount: 3);
            var seen = new List<float>();

            for (int step = 0; step < course.StepCount; step++)
            {
                var targets = new List<ArcheryTarget>();
                course.Fill(targets, step, 0L);
                Assert.IsNotEmpty(targets);
                seen.Add(targets[0].Origin.z);   // 레인 0의 과녁 거리
            }

            seen.Sort();
            CollectionAssert.AreEqual(new[] { 10f, 20f, 30f }, seen,
                "거리 목록의 순열이어야 한다 — 빠지거나 겹치면 화살 수와 과녁 수가 어긋난다");
        }

        [Test]
        public void 사수마다_자기_레인에_자기_과녁이_선다()
        {
            var course = RangeCourse(laneCount: 2);
            var targets = new List<ArcheryTarget>();
            course.Fill(targets, 0, 0L);

            Assert.AreEqual(2, targets.Count, "사수가 둘이면 과녁도 둘이다");
            Assert.AreEqual(0, targets[0].SlotIndex);
            Assert.AreEqual(1, targets[1].SlotIndex);
            Assert.AreEqual("user-a", targets[0].OwnerUserId);
            Assert.AreEqual("user-b", targets[1].OwnerUserId);
            Assert.AreEqual(0f, targets[0].Origin.x, 1e-4f, "첫 사수의 과녁은 첫 레인 위에 선다");
            Assert.AreEqual(4f, targets[1].Origin.x, 1e-4f);
            Assert.AreEqual(targets[0].Origin.z, targets[1].Origin.z, 1e-4f,
                "같은 단계에서는 모두가 같은 거리를 본다 — 같은 시험지다");
        }

        //  레인은 4m 간격, 폭 3m으로 흔들면 양쪽 끝에서 1.5m씩만 벗어난다 — 겹칠 여지가 없어야
        //  하는데, 실제로 겹치지 않는지는 흔들리는 축(레인이 보는 쪽의 수평 수직선)이 맞아야 확인된다.
        [Test]
        public void 레인이_다르면_흔들려도_서로의_레인을_넘지_않는다()
        {
            var course = RangeCourse(laneCount: 2, standCount: 1, lateralSpan: 3f, lateralPeriod: 3f);
            var targets = new List<ArcheryTarget>();
            course.Fill(targets, 0, 0L);

            Assert.AreEqual(2, targets.Count);
            var laneA = targets[0];
            var laneB = targets[1];
            //  레인 사이 딱 가운데 — 어느 쪽도 이 선을 넘으면 안 된다.
            float midway = (laneA.Origin.x + laneB.Origin.x) / 2f;

            for (long tick = 0; tick <= 300; tick += 3)
            {
                float xa = ArcheryTargetMotion.PositionAt(laneA, tick, 0.02f).x;
                float xb = ArcheryTargetMotion.PositionAt(laneB, tick, 0.02f).x;

                Assert.Less(xa, midway, $"틱 {tick}: 레인 A(x={xa})가 레인 사이 가운데({midway})를 넘었다");
                Assert.Greater(xb, midway, $"틱 {tick}: 레인 B(x={xb})가 레인 사이 가운데({midway})를 넘었다");
            }
        }

        [Test]
        public void 과녁은_사수_쪽을_바라본다()
        {
            var course = RangeCourse();
            var targets = new List<ArcheryTarget>();
            course.Fill(targets, 0, 0L);

            //  레인이 +z를 보므로 과녁은 −z를 본다. 뒤에서 온 화살은 통과한다(토대 슬라이스).
            Assert.AreEqual(Vector3.back, targets[0].Facing);
            Assert.AreEqual(ArcheryTargetShape.Face, targets[0].Shape);
            Assert.AreEqual(0f, targets[0].RiseSpeed, "사거리 과녁은 솟지 않는다");
        }

        [Test]
        public void 단계_경계는_노출과_간격의_누적합이다()
        {
            var course = RangeCourse(standCount: 3);
            long start = 1000L;

            Assert.AreEqual(-1, course.IndexAt(start - 1, start), "출발 전에는 −1이다");
            Assert.AreEqual(0, course.IndexAt(start, start));

            //  단계 0의 길이 = 그 자리의 노출 + 간격. 순서가 씨앗마다 다르므로 값을 직접 읽어 잰다.
            var first = new List<ArcheryTarget>();
            course.Fill(first, 0, start);
            long firstLength = (long)Mathf.Round(first[0].LifetimeSeconds / 0.02f) + 25;

            Assert.AreEqual(0, course.IndexAt(start + firstLength - 1, start));
            Assert.AreEqual(1, course.IndexAt(start + firstLength, start));
        }

        [Test]
        public void 순서가_끝나면_판도_끝난다()
        {
            var course = RangeCourse(standCount: 3);
            long start = 0L;

            Assert.AreEqual(3, course.StepCount);
            Assert.GreaterOrEqual(course.IndexAt(course.MatchDurationTicks, start), course.StepCount,
                "코스 길이만큼 지나면 더 이상 단계가 없어야 한다");

            //  노출(100+150+200) + 간격(25×3) = 525틱
            Assert.AreEqual(525L, course.MatchDurationTicks);
        }

        //  화살은 **자리 수 × 자리당 발수**다. 자리당 여러 발이 필요한 이유는 이 게임의 실력이
        //  **리드 추정**이기 때문이다 — 화살이 날아가는 동안 과녁이 움직이니 빈 공간을 겨눠야 하고,
        //  그건 *빗나간 걸 보고 고치면서* 는다. 한 과녁에 한 발이면 표본이 하나뿐이라 고칠 기회가 없다.
        //  ── 자리가 바뀌면 화살이 다시 채워진다 ──────────────────────────────
        //
        //  **전체를 한 주머니로 두면 제일 쉬운 자리에 다 붓는 것이 최적**이 된다 —
        //  12m의 10점 링은 화면에서 18.8px, 90m는 7.6px이라 같은 화살의 기대 점수가
        //  비교가 안 된다. 그러면 거리를 여섯 개 둔 의미가 사라진다.
        //  (과녁이 맞으면 사라지던 시절엔 그 제동이 저절로 걸려 있었다 — 2026-09-22에 뗐다.)
        [Test]
        public void 자리가_바뀌면_화살이_다시_채워진다()
        {
            var registry = new GameFramework.World.EntityRegistry();
            var course = RangeCourse(standCount: 3, arrowsPerStand: 3);
            var world = ArcheryWorldFixture.Still(registry,
                                         new ArcheryAimSystem(RangeConfig(3, arrowsPerStand: 3)),
                                         course, 0.02f);
            world.GameplayStartTick = 0;   // 기본값이 long.MaxValue라 안 세우면 "아직 출발 전"이다

            var archer = new GameFramework.World.Entity("archer");
            archer.Add(new ArcheryAim());
            archer.Add(new ArcheryQuiver { Remaining = 0, RefilledWave = -1 });
            archer.Add(new GameFramework.World.Simulated());
            registry.Add(archer);

            //  첫 자리: 바닥난 화살통이 그 자리 몫으로 채워진다.
            world.Tick(0, 0.02f);
            Assert.AreEqual(3, archer.Get<ArcheryQuiver>().Remaining, "첫 자리에서 안 채워졌다");

            //  같은 자리 안에서는 다시 안 채운다 — 채우면 무제한이 된다.
            archer.Get<ArcheryQuiver>().Remaining = 1;
            world.Tick(1, 0.02f);
            Assert.AreEqual(1, archer.Get<ArcheryQuiver>().Remaining, "같은 자리인데 다시 채웠다");

            //  다음 자리로 넘어가면 다시 3발. **남은 1발은 안 넘어간다.**
            long next = FirstTickOfStep(course, 1);
            world.Tick(next, 0.02f);
            Assert.AreEqual(3, archer.Get<ArcheryQuiver>().Remaining, "자리가 바뀌었는데 안 채워졌다");
        }

        //  그 자리가 시작하는 첫 틱. 경계가 누적합이라 코스에 직접 묻는다.
        private static long FirstTickOfStep(ArcheryCourse course, int step)
        {
            for (long t = 0; t < 100000; t++)
            {
                if (course.IndexAt(t, 0) == step)
                {
                    return t;
                }
            }
            throw new System.InvalidOperationException("그 자리가 시작하는 틱을 못 찾았다");
        }

        [Test]
        public void 화살은_자리_하나마다_주어진다()
        {
            //  자리 수와 무관하다 — 전체 주머니가 아니라 **자리마다** 받는 수다.
            Assert.AreEqual(1, RangeCourse(standCount: 3, arrowsPerStand: 1).ArrowsPerStand);
            Assert.AreEqual(3, RangeCourse(standCount: 3, arrowsPerStand: 3).ArrowsPerStand);
            Assert.AreEqual(2, RangeCourse(standCount: 5, arrowsPerStand: 2).ArrowsPerStand);
        }

        //  자리당 발수가 0이면 화살이 0발이 되는데, 0은 **무제한**이라는 뜻이라
        //  "한 발도 못 쏜다"가 아니라 "무한정 쏜다"로 뒤집힌다. 데이터 실수 한 칸이
        //  판을 통째로 망가뜨리지 않게 최소 1로 본다.
        [Test]
        public void 자리당_발수가_0이어도_무제한이_되지_않는다()
        {
            Assert.AreEqual(1, RangeCourse(standCount: 3, arrowsPerStand: 0).ArrowsPerStand);
        }

        [Test]
        public void 씬이_아직_안_떴으면_빈_레이아웃을_굳히지_않는다()
        {
            //  판이 시작한 뒤에도 맵 씬이 아직 없을 수 있다 — 중간에 들어온 클라, 또는
            //  gameplayStartTick 직후에야 끝나는 additive 로드. 그때 읽은 빈 레이아웃을 캐시하면
            //  이 인스턴스는 영영 과녁을 못 만드는데 예외도 로그도 없다.
            var ready = Layout(laneCount: 2, standCount: 3);
            var current = ArcheryRangeLayout.From(new ArcheryLane[0]);
            var course = new ArcheryCourse(RangeConfig(), new FixedSeed(3UL),
                                           new[] { "user-a", "user-b" }, 0.02f, () => current);

            //  씬이 없는 동안은 몇 번을 물어도 과녁이 없고, 터지지도 않는다.
            var tooEarly = new List<ArcheryTarget>();
            course.Fill(tooEarly, 0, 0L);
            Assert.IsEmpty(tooEarly, "씬에 레인이 없는데 과녁이 나왔다");
            Assert.AreEqual(-1, course.IndexAt(0L, 0L), "씬이 안 떴으면 서 있는 단계가 없다");
            course.Fill(tooEarly, 0, 0L);
            Assert.IsEmpty(tooEarly, "두 번째로 물어도 마찬가지여야 한다");

            //  맵 씬이 떴다.
            current = ready;

            var afterLoad = new List<ArcheryTarget>();
            course.Fill(afterLoad, 0, 0L);
            Assert.AreEqual(2, afterLoad.Count,
                "빈 레이아웃을 굳혀 버렸다 — 씬이 떠도 이 인스턴스는 영영 과녁을 못 만든다");
            Assert.AreEqual(0, course.IndexAt(0L, 0L));
        }

        [Test]
        public void 웨이브_맵은_예전_생성기와_한_글자도_다르지_않다()
        {
            var kinds = new List<ArcheryTargetKind>
            {
                new ArcheryTargetKind(0.45f, 1, 30, false, ArcheryTargetShape.Sphere, null),
                new ArcheryTargetKind(0.30f, 2, 40, false, ArcheryTargetShape.Sphere, null),
                new ArcheryTargetKind(0.20f, 4, 30, false, ArcheryTargetShape.Sphere, null),
            };
            //  기본값 자리 — course_kind를 안 주면 웨이브다.
            var config = new ArcheryConfig(120, 3, 5, 3.5f, 0.3f, 0.6f, 1.2f, 0f, 1f,
                                           1.2f, 2.5f, 0f, 1.2f, 2.4f, 12, 20, kinds);
            var course = new ArcheryCourse(config, new FixedSeed(999UL), new[] { "user-a" }, 0.02f,
                                           () => ArcheryRangeLayout.From(new ArcheryLane[0]));

            var viaCourse = new List<ArcheryTarget>();
            var direct = new List<ArcheryTarget>();
            for (int wave = 0; wave < 4; wave++)
            {
                course.Fill(viaCourse, wave, 500L);
                ArcheryWaveGenerator.Fill(direct, 999UL, wave, config, 500L);

                //  비교할 것이 있어야 비교가 뜻이 있다 — 둘 다 비면 아래 루프가 한 번도 안 돈다.
                Assert.IsNotEmpty(direct, $"웨이브 {wave}에 과녁이 하나도 없다 — 비교가 공허하다");
                Assert.AreEqual(direct.Count, viaCourse.Count, $"웨이브 {wave}의 과녁 수가 다르다");
                for (int i = 0; i < direct.Count; i++)
                {
                    //  ArcheryTarget의 **모든** 칸을 잰다. 지금은 코스가 생성기에 그대로 넘기는
                    //  한 줄이라 구조적으로 안전하지만, 누가 그 경로를 코스 안으로 베껴 오는 날
                    //  이 시험이 잡는다 — 그날 빠지는 칸이 어느 것일지 모르니 전부 본다.
                    Assert.AreEqual(direct[i].WaveIndex, viaCourse[i].WaveIndex);
                    Assert.AreEqual(direct[i].SlotIndex, viaCourse[i].SlotIndex);
                    Assert.AreEqual(direct[i].Origin, viaCourse[i].Origin);
                    Assert.AreEqual(direct[i].RiseSpeed, viaCourse[i].RiseSpeed);
                    Assert.AreEqual(direct[i].SpawnTick, viaCourse[i].SpawnTick);
                    Assert.AreEqual(direct[i].Radius, viaCourse[i].Radius);
                    Assert.AreEqual(direct[i].Points, viaCourse[i].Points);
                    Assert.AreEqual(direct[i].IsTrap, viaCourse[i].IsTrap);
                    Assert.AreEqual(direct[i].Shape, viaCourse[i].Shape);
                    Assert.AreEqual(direct[i].Facing, viaCourse[i].Facing);
                    Assert.AreEqual(direct[i].LifetimeSeconds, viaCourse[i].LifetimeSeconds);
                    Assert.AreEqual(direct[i].OwnerUserId, viaCourse[i].OwnerUserId);
                    Assert.AreEqual(direct[i].LateralSpan, viaCourse[i].LateralSpan);
                    Assert.AreEqual(direct[i].LateralPeriod, viaCourse[i].LateralPeriod);

                    //  띠 목록은 참조라 값끼리 대 봐야 한다. 띠가 없는 종류(null)도 그대로여야
                    //  한다 — 빈 목록으로 바뀌면 점수 계산이 "띠 하나"로 안 떨어진다.
                    var directBands = direct[i].Bands;
                    var courseBands = viaCourse[i].Bands;
                    Assert.AreEqual(directBands == null, courseBands == null,
                        $"웨이브 {wave} 슬롯 {i}의 띠 목록이 한쪽만 null이다");
                    if (directBands != null)
                    {
                        Assert.AreEqual(directBands.Count, courseBands.Count);
                        for (int b = 0; b < directBands.Count; b++)
                        {
                            Assert.AreEqual(directBands[b].OuterRatio, courseBands[b].OuterRatio);
                            Assert.AreEqual(directBands[b].Points, courseBands[b].Points);
                        }
                    }
                }
                Assert.AreEqual(ArcheryWaveGenerator.WaveIndexAt(600L, 500L, config),
                                course.IndexAt(600L, 500L));
            }

            Assert.AreEqual(0, course.ArrowsPerStand, "웨이브 맵은 화살이 무제한이다");
        }

        [Test]
        public void 레인보다_사수가_많으면_있는_레인까지만_선다()
        {
            //  씬이 모자란 상태 — 서버 룰이 판 시작에 크게 실패시키지만(Task 6),
            //  코스 자체는 조용히 터지지 않아야 한다.
            var oneLane = Layout(laneCount: 1);
            var course = new ArcheryCourse(RangeConfig(), new FixedSeed(5UL),
                                           new[] { "user-a", "user-b" }, 0.02f, () => oneLane);
            var targets = new List<ArcheryTarget>();
            course.Fill(targets, 0, 0L);

            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual("user-a", targets[0].OwnerUserId);
        }
    
        // ── 거리마다 다른 과녁 크기 ─────────────────────────────────────────────
        //
        //  한 크기로 통일하면 가까운 자리는 거저 10점이고 먼 자리는 흔들림이 링을 통째로
        //  잡아먹는다 — 양쪽 다 실력을 못 가린다(화면에서 10점 링이 12m 57px vs 90m 7.6px).
        //  실제 양궁도 거리마다 과녁 크기를 바꿔 이걸 맞춘다.

        [Test]
        public void 자리에_정해진_크기가_있으면_그걸_쓴다()
        {
            var layout = Layout(laneCount: 1, standCount: 2);
            var stands = new List<ArcheryRangeStand>
            {
                new ArcheryRangeStand(0, 12f, 200, 0f, 0f, faceRadiusM: 0.20f),
                new ArcheryRangeStand(1, 90f, 200, 0f, 0f, faceRadiusM: 0.61f),
            };
            var config = RangeConfigWith(stands);
            var course = new ArcheryCourse(config, new FixedSeed(1UL), new[] { "user-a" }, 0.02f, () => layout);

            //  두 단계를 모두 채워, 각 자리가 제 크기로 섰는지 본다(순서는 씨앗이 정한다).
            var radiusByStand = new Dictionary<int, float>();
            for (int step = 0; step < 2; step++)
            {
                var targets = new List<ArcheryTarget>();
                course.Fill(targets, step, 1000L);
                Assert.AreEqual(1, targets.Count);
                //  어느 자리인지는 세운 위치(z)로 안다 — layout이 자리마다 다른 자리를 준다.
                radiusByStand[targets[0].Origin == layout.Lanes[0].Stands[0] ? 0 : 1] = targets[0].Radius;
            }

            Assert.AreEqual(0.20f, radiusByStand[0], 1e-4f, "가까운 자리가 제 크기로 안 섰다");
            Assert.AreEqual(0.61f, radiusByStand[1], 1e-4f, "먼 자리가 제 크기로 안 섰다");
        }

        [Test]
        public void 크기를_안_주면_과녁_종류에_적힌_값을_쓴다()
        {
            var layout = Layout(laneCount: 1, standCount: 1);
            var stands = new List<ArcheryRangeStand> { new ArcheryRangeStand(0, 30f, 200, 0f, 0f) };
            var course = new ArcheryCourse(RangeConfigWith(stands), new FixedSeed(1UL),
                                           new[] { "user-a" }, 0.02f, () => layout);

            var targets = new List<ArcheryTarget>();
            course.Fill(targets, 0, 1000L);

            Assert.AreEqual(FaceKind().Radius, targets[0].Radius, 1e-4f);
        }

        //  점수 띠는 **비율**이라 반지름을 바꿔도 따라와야 한다 — 안 따라오면 작은 과녁의
        //  10점 링이 옛 크기 그대로라 "가운데를 맞혔는데 5점"이 난다.
        [Test]
        public void 과녁이_작아져도_점수_띠는_비율_그대로_따라온다()
        {
            var layout = Layout(laneCount: 1, standCount: 1);
            var stands = new List<ArcheryRangeStand> { new ArcheryRangeStand(0, 12f, 200, 0f, 0f, faceRadiusM: 0.20f) };
            var course = new ArcheryCourse(RangeConfigWith(stands), new FixedSeed(1UL),
                                           new[] { "user-a" }, 0.02f, () => layout);

            var targets = new List<ArcheryTarget>();
            course.Fill(targets, 0, 1000L);
            var target = targets[0];

            Assert.AreEqual(0.20f, target.Radius, 1e-4f);
            //  가장 안쪽 띠는 0.2 비율 = 반지름 0.04m 안쪽이 10점. 비율이 그대로인지 본다.
            Assert.AreEqual(0.2f, target.Bands[0].OuterRatio, 1e-4f);
            Assert.AreEqual(10, target.Bands[0].Points);
        }

        private static ArcheryConfig RangeConfigWith(List<ArcheryRangeStand> stands)
        {
            var range = new ArcheryRangeSettings(FaceKind(), stands, stepGapTicks: 25);
            return new ArcheryConfig(120, 3, 5, 3.5f, 0.3f, 0.6f, 1.2f, 0f, 1f,
                                     1.2f, 2.5f, 0f, 1.2f, 2.4f, 12, 20,
                                     new List<ArcheryTargetKind> { FaceKind() },
                                     ArcheryCourseKind.Range, 0, range);
        }

        //  라운드 넷: 자리 0,2,0,1을 다시 쓴다. 마지막은 두 배, 셋째는 바람.
        private static ArcheryConfig ShootOffConfig()
        {
            var stands = new List<ArcheryRangeStand>
            {
                new ArcheryRangeStand(0, 10f, 250, 0f, 0f),
                new ArcheryRangeStand(2, 30f, 250, 0f, 0f),
                new ArcheryRangeStand(0, 10f, 250, 0f, 0f, 0f, windMps2: 12f),
                new ArcheryRangeStand(1, 20f, 300, 0f, 0f, 0f, 0f, pointsMultiplier: 2),
            };
            //  데이터에 화살·박스·속도를 적어도 ShootOff는 무시해야 한다 — 일부러 채운다.
            var range = new ArcheryRangeSettings(FaceKind(), stands, stepGapTicks: 200, arrowsPerStand: 5,
                                                 boxHalfWidthM: 3f, boxHalfDepthM: 1.5f, moveSpeedMps: 4f);
            return new ArcheryConfig(120, 3, 5, 3.5f, 0.3f, 0.6f, 1.2f, 0f, 1f,
                                     1.2f, 2.5f, 0f, 1.2f, 2.4f, 12, 20,
                                     new List<ArcheryTargetKind> { FaceKind() },
                                     ArcheryCourseKind.ShootOff, 0, range);
        }

        private ArcheryCourse ShootOffCourse(ArcheryRangeLayout layout = null)
        {
            var l = layout ?? Layout(laneCount: 2, standCount: 3);
            return new ArcheryCourse(ShootOffConfig(), new FixedSeed(777UL),
                                     new[] { "user-a", "user-b", "user-c", "user-d" }, 0.02f, () => l);
        }

        [Test]
        public void ShootOff는_데이터_순서_그대로_선다()
        {
            var course = ShootOffCourse();
            var targets = new List<ArcheryTarget>();
            //  라운드 1(두 번째)은 자리 2(30m) — 섞였다면 씨앗에 따라 다른 자리가 나온다.
            course.Fill(targets, 1, 0);
            Assert.AreEqual(1, targets.Count);
            Assert.AreEqual(30f, targets[0].Origin.z, 1e-4f);   // 자리 2 = z 30
            Assert.AreEqual(0f, targets[0].Origin.x, 1e-4f);    // 레인 0(x=0)
        }

        [Test]
        public void ShootOff는_레인_0에_공유_과녁_하나()
        {
            var course = ShootOffCourse();
            var targets = new List<ArcheryTarget>();
            course.Fill(targets, 0, 0);
            Assert.AreEqual(1, targets.Count);
            Assert.IsTrue(targets[0].IsShared);
            Assert.AreEqual(string.Empty, targets[0].OwnerUserId);
        }

        [Test]
        public void ShootOff는_씨앗이_달라도_같은_순서()
        {
            var layout = Layout(2, 3);
            var a = new ArcheryCourse(ShootOffConfig(), new FixedSeed(1UL), new[] { "u" }, 0.02f, () => layout);
            var b = new ArcheryCourse(ShootOffConfig(), new FixedSeed(999UL), new[] { "u" }, 0.02f, () => layout);
            var ta = new List<ArcheryTarget>();
            var tb = new List<ArcheryTarget>();
            for (int i = 0; i < 4; i++)
            {
                a.Fill(ta, i, 0);
                b.Fill(tb, i, 0);
                Assert.AreEqual(ta[0].Origin, tb[0].Origin);
            }
        }

        [Test]
        public void ShootOff는_한_발_박스_0_속도_0()
        {
            var course = ShootOffCourse();
            Assert.AreEqual(1, course.ArrowsPerStand);
            Assert.AreEqual(0f, course.ShootingBoxHalfWidth);
            Assert.AreEqual(0f, course.ShootingBoxHalfDepth);
            Assert.AreEqual(0f, course.MoveSpeed);
            Assert.AreEqual(4, course.StepCount);
            Assert.IsTrue(course.IsLaned);
            Assert.IsTrue(course.IsShootOff);
        }

        [Test]
        public void 라운드_마감_틱은_앞_라운드들의_노출과_간격의_합_더하기_이_노출()
        {
            var course = ShootOffCourse();
            Assert.AreEqual(1000 + 250, course.RoundCloseTick(0, 1000));
            Assert.AreEqual(1000 + 450 + 250, course.RoundCloseTick(1, 1000));
            Assert.AreEqual(1000 + 450 * 3 + 300, course.RoundCloseTick(3, 1000));
        }

        [Test]
        public void 배수는_데이터_값이고_범위_밖은_1()
        {
            var course = ShootOffCourse();
            Assert.AreEqual(1, course.MultiplierAt(0));
            Assert.AreEqual(2, course.MultiplierAt(3));
            Assert.AreEqual(1, course.MultiplierAt(9));
        }

        [Test]
        public void 바람은_사수_기준_오른쪽이_양수()
        {
            var course = ShootOffCourse();
            //  라운드 2가 시작하는 틱 = 450 × 2. 레인은 +z를 보므로 오른쪽은 +x.
            var wind = course.WindAt(900 + 10, 0);
            Assert.AreEqual(12f, wind.x, 1e-4f);
            Assert.AreEqual(0f, wind.y, 1e-4f);
            Assert.AreEqual(0f, wind.z, 1e-4f);
            Assert.AreEqual(Vector3.zero, course.WindAt(10, 0));     // 라운드 0은 바람 없음
        }

        [Test]
        public void 레이아웃이_없으면_바람은_0()
        {
            var course = new ArcheryCourse(ShootOffConfig(), new FixedSeed(1UL), new[] { "u" }, 0.02f,
                                           () => ArcheryRangeLayout.From(new ArcheryLane[0]));
            Assert.AreEqual(Vector3.zero, course.WindAt(910, 0));
        }

        [Test]
        public void 사거리는_공유_과녁이_아니고_예전처럼_레인마다_하나()
        {
            var course = RangeCourse();
            var targets = new List<ArcheryTarget>();
            course.Fill(targets, 0, 0);
            Assert.AreEqual(2, targets.Count);
            Assert.IsFalse(targets[0].IsShared);
            Assert.IsFalse(course.IsShootOff);
            Assert.AreEqual(Vector3.zero, course.WindAt(10, 0));
        }
}
}
