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
        private static ArcheryConfig RangeConfig(int standCount = 3)
        {
            var stands = new List<ArcheryRangeStand>();
            for (int s = 0; s < standCount; s++)
            {
                stands.Add(new ArcheryRangeStand(s, 10f * (s + 1), 100 + s * 50));
            }
            var range = new ArcheryRangeSettings(FaceKind(), stands, stepGapTicks: 25);

            return new ArcheryConfig(120, 3, 5, 3.5f, 0.3f, 0.6f, 1.2f, 0f, 1f,
                                     1.2f, 2.5f, 0f, 1.2f, 2.4f, 12, 20,
                                     new List<ArcheryTargetKind> { FaceKind() },
                                     ArcheryCourseKind.Range, 0, range);
        }

        private ArcheryCourse RangeCourse(ulong seed = 777UL, int laneCount = 2, int standCount = 3)
        {
            var layout = Layout(laneCount, standCount);
            return new ArcheryCourse(RangeConfig(standCount), new FixedSeed(seed),
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

        [Test]
        public void 화살은_과녁_수만큼_주어진다()
        {
            Assert.AreEqual(3, RangeCourse(standCount: 3).ArrowsPerArcher);
            Assert.AreEqual(5, RangeCourse(standCount: 5).ArrowsPerArcher);
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

                Assert.AreEqual(direct.Count, viaCourse.Count, $"웨이브 {wave}의 과녁 수가 다르다");
                for (int i = 0; i < direct.Count; i++)
                {
                    Assert.AreEqual(direct[i].Origin, viaCourse[i].Origin);
                    Assert.AreEqual(direct[i].SpawnTick, viaCourse[i].SpawnTick);
                    Assert.AreEqual(direct[i].Points, viaCourse[i].Points);
                    Assert.AreEqual(direct[i].Radius, viaCourse[i].Radius);
                    Assert.AreEqual(direct[i].IsTrap, viaCourse[i].IsTrap);
                }
                Assert.AreEqual(ArcheryWaveGenerator.WaveIndexAt(600L, 500L, config),
                                course.IndexAt(600L, 500L));
            }

            Assert.AreEqual(0, course.ArrowsPerArcher, "웨이브 맵은 화살이 무제한이다");
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
    }
}
