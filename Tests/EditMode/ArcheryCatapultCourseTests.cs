using System.Collections.Generic;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;
using static LOP.Tests.ArcheryCatapultFixture;

namespace LOP.Tests
{
    public class ArcheryCatapultCourseTests
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

        //  씬과 같은 모양: 레인 0이 원점(+z를 본다), 옆 레인은 8 m 떨어져 있다 — 투석기는 레인 0만 써야 한다.
        private ArcheryRangeLayout SceneLikeLayout()
        {
            var lanes = new List<ArcheryLane>();
            for (int i = 0; i < 4; i++)
            {
                var root = new GameObject("lane" + i);
                spawned.Add(root);
                root.transform.position = new Vector3(i * 8f, 0f, 0f);
                var lane = root.AddComponent<ArcheryLane>();
                lane.Order = i;
                var stand = new GameObject("stand0");
                stand.transform.SetParent(root.transform);
                stand.transform.localPosition = new Vector3(0f, 1.3f, 12f);
                lane.Stands = new[] { stand.transform };
                lanes.Add(lane);
            }
            return ArcheryRangeLayout.From(lanes);
        }

        private ArcheryCourse Course(ulong seed = 42UL, string[] roster = null)
        {
            var layout = SceneLikeLayout();
            return new ArcheryCourse(Config(), new FixedSeed(seed), roster ?? new[] { "u1", "u2" }, Dt, () => layout);
        }

        [Test]
        public void 투석기는_사대_한_줄을_쓰고_레인형이_아니다()
        {
            var course = Course();
            Assert.IsTrue(course.IsCatapult);
            Assert.IsTrue(course.UsesSharedLane);
            Assert.IsFalse(course.IsLaned);
            Assert.IsFalse(course.IsShootOff);
            Assert.AreEqual(Vector3.zero, course.SharedLane.Value.ShooterPosition);
        }

        [Test]
        public void 단계_수는_무리_수고_판_길이는_라운드_합이다()
        {
            var course = Course();
            Assert.AreEqual(21, course.StepCount);
            Assert.AreEqual(4320L, course.MatchDurationTicks);
        }

        [Test]
        public void 투석기는_화살통이_없고_걷지_않는다()
        {
            var course = Course();
            Assert.AreEqual(0, course.ArrowsPerStand);
            Assert.AreEqual(0f, course.MoveSpeed);
            Assert.AreEqual(0f, course.ShootingBoxHalfWidth);
        }

        [Test]
        public void 단계_번호는_시계를_따른다()
        {
            //  단계는 "준비"가 아니라 발사 틱(1000 + 소개 80 + 준비 35 = 1115)에서 넘어간다.
            var course = Course();
            Assert.AreEqual(-1, course.IndexAt(1114, StartTick));
            Assert.AreEqual(0, course.IndexAt(1115, StartTick));
            Assert.AreEqual(21, course.IndexAt(5320, StartTick));
        }

        [Test]
        public void 무리의_마지막_살아_있는_틱에도_단계는_그_무리다()
        {
            //  다음 무리의 "준비"가 먼저 와도 지난 무리는 수명을 다 채워야 한다 — 판정·화면이 이 번호로 목록을 갈아 끼운다.
            var course = Course();
            var s = Config().Catapult;
            for (int v = 0; v < s.VolleyCount; v++)
            {
                long lastLive = ArcheryCatapultSchedule.LaunchTick(s, v, StartTick) + s.TargetLifeTicks - 1;
                Assert.AreEqual(v, course.IndexAt(lastLive, StartTick), $"무리 {v}");
            }
        }

        [Test]
        public void 채우기는_레인_0_기준으로_생성기와_같은_목록을_낸다()
        {
            var course = Course(seed: 42UL);
            var fromCourse = new List<ArcheryTarget>();
            var direct = new List<ArcheryTarget>();
            course.Fill(fromCourse, 4, StartTick);
            ArcheryCatapultGenerator.Fill(direct, 42UL, 4, Config(), 2, Vector3.zero, Vector3.forward, StartTick, Dt);
            Assert.AreEqual(direct.Count, fromCourse.Count);
            for (int i = 0; i < direct.Count; i++)
            {
                Assert.AreEqual(direct[i].Origin, fromCourse[i].Origin);
            }
        }

        [Test]
        public void 채우기는_명단_수만큼_일반_표적을_낸다()
        {
            var roster = new[] { "u1", "u2", "u3", "u4", "u5", "u6", "u7", "u8" };
            var course = Course(seed: 42UL, roster: roster);
            var list = new List<ArcheryTarget>();
            for (int v = 0; v < 21; v++)
            {
                course.Fill(list, v, StartTick);
                int normals = 0;
                foreach (var t in list) { if (t.IsTrap == false && t.IsJackpot == false) { normals++; } }
                Assert.AreEqual(4, normals, $"무리 {v}");
            }
        }

        [Test]
        public void 자리는_라운드마다_씨앗으로_섞인다()
        {
            var course = Course(seed: 7UL);
            //  1라운드(시작 전·소개 포함) = 입장 순서, 2라운드 소개부터 섞인 자리.
            Assert.AreEqual(1, course.SeatSlotAt(1, 3, StartTick - 10, StartTick));
            Assert.AreEqual(1, course.SeatSlotAt(1, 3, 2439, StartTick));
            Assert.AreEqual(ArcheryCatapultSeats.SlotOf(1, 1, 3, 7UL), course.SeatSlotAt(1, 3, 2440, StartTick));
            Assert.AreEqual(ArcheryCatapultSeats.SlotOf(1, 2, 3, 7UL), course.SeatSlotAt(1, 3, 9999, StartTick));
        }

        [Test]
        public void 월드는_투석기_사수를_사대_한_줄에_세운다()
        {
            var course = Course(seed: 7UL, roster: new[] { "ua", "ub", "uc" });
            var config = Config();
            var registry = new EntityRegistry();
            var archers = new Dictionary<string, Entity>();
            foreach (var id in new[] { "c", "a", "b" })
            {
                var e = new Entity(id);
                e.Add(new GameFramework.World.Transform());
                e.Add(new Velocity());
                e.Add(new ArcheryAim());
                e.Add(new InputBuffer());
                e.Add(new ArcheryStance(Vector3.zero, Vector3.forward));
                e.Add(new Simulated());
                registry.Add(e);
                archers[id] = e;
            }
            var world = ArcheryWorldFixture.Still(registry, new ArcheryAimSystem(config), course, Dt);
            world.GameplayStartTick = StartTick;

            world.Tick(2440, Dt);   // 2라운드 소개 — 섞인 자리
            string[] byId = { "a", "b", "c" };
            for (int i = 0; i < 3; i++)
            {
                int slot = ArcheryCatapultSeats.SlotOf(i, 1, 3, 7UL);
                float x = ArcheryShootOffSeats.LateralOffset(slot, 3);
                Assert.AreEqual(x, archers[byId[i]].Get<GameFramework.World.Transform>().Position.X, 1e-4f, byId[i]);
            }
        }
    }
}
