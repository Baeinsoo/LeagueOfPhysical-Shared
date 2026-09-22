using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    //  사수가 사대 안에서 걷는다. 재는 것은 셋이다 —
    //   ① 입력대로 움직이나  ② 사대를 못 벗어나나  ③ 몸이 걷는 방향으로 돌아가지 않나.
    //
    //  ③이 중요한 이유: 공용 MovementSystem은 걷는 방향으로 몸을 돌린다(보통은 맞다).
    //  활쏘기에서는 몸이 과녁을 봐야 하는데, 옆으로 걸었다고 몸이 돌면 옆을 보고 쏘는 그림이 된다.
    public class ArcheryMovementTests
    {
        const float TickInterval = 0.02f;
        const float MoveSpeed = 3.5f;
        const float HalfWidth = 2f;
        const float HalfDepth = 1f;

        private sealed class ZeroSeed : IMatchSeed { public ulong Value => 1UL; }

        static ArcheryConfig RangeConfig(float moveSpeed = MoveSpeed)
        {
            var stands = new[] { new ArcheryRangeStand(0, 12f, 100, 0f, 0f, 0.2f) };
            var kind = new ArcheryTargetKind(0.2f, 5, 0, false, ArcheryTargetShape.Face,
                new[] { new ArcheryRingBand(1f, 5) });
            var range = new ArcheryRangeSettings(kind, stands, stepGapTicks: 10, arrowsPerStand: 3,
                boxHalfWidthM: HalfWidth, boxHalfDepthM: HalfDepth, moveSpeedMps: moveSpeed);

            return new ArcheryConfig(100, 1, 1, 1f, 0f, 1f, 1f, 0f, 0f,
                                     1f, 1f, 0f, 0.1f, 0.1f, 1, 1, kinds: null,
                                     courseKind: ArcheryCourseKind.Range,
                                     matchDurationTicks: 0, range: range);
        }

        //  사대는 원점, 과녁은 +z 쪽. 몸은 그쪽을 보고 선다.
        static readonly Vector3 Origin = Vector3.zero;
        static readonly Vector3 Facing = Vector3.forward;

        static (ArcheryWorld world, Entity archer) Make(float moveSpeed = MoveSpeed)
        {
            var config = RangeConfig(moveSpeed);
            var registry = new EntityRegistry();

            var archer = new Entity("archer-1");
            archer.Add(new GameFramework.World.Transform { Position = Origin.ToNumerics() });
            archer.Add(new Velocity());
            archer.Add(new ArcheryAim());
            archer.Add(new InputBuffer());
            archer.Add(new Simulated());
            archer.Add(new ArcheryStance(Origin, Facing));
            var stats = new Stats();
            stats.BaseStats[(int)EntityStatType.MoveSpeed] = moveSpeed;
            archer.Add(stats);
            archer.Add(new CapsuleShape(0.4f, 1.8f));
            registry.Add(archer);

            var course = new ArcheryCourse(config, new ZeroSeed(), new[] { "user-a" }, TickInterval,
                                           () => null);
            var world = new ArcheryWorld(registry, new WorldEventBuffer(),
                                         new ArcheryAimSystem(config), course, TickInterval,
                                         new MovementSystem(new StatsSystem(), new MotionContributionSystem()),
                                         new KinematicMoveSystem(new FlappyWorldFixture.NeverHit(), 0),
                                         new FlappyWorldFixture.NoopMotionBridge());
            return (world, archer);
        }

        static void Walk(Entity archer, float horizontal, float vertical)
        {
            archer.Get<InputBuffer>().Current = new InputCommand
            {
                Horizontal = horizontal, Vertical = vertical,
            };
        }

        static Vector3 Position(Entity archer) => archer.Get<GameFramework.World.Transform>().Position.ToUnity();

        static void Run(ArcheryWorld world, long ticks)
        {
            for (long t = 1; t <= ticks; t++)
            {
                world.Tick(t, TickInterval);
            }
        }

        [Test]
        public void 옆으로_걸으면_옆으로_간다()
        {
            var (world, archer) = Make();
            Walk(archer, horizontal: 1f, vertical: 0f);

            Run(world, 10);

            Assert.Greater(Position(archer).x, 0.05f, "옆 입력을 줬는데 안 움직였다");
        }

        //  ⭐ 이 슬라이스의 핵심 제약 — 앞으로 걸어 나가 거리를 줄일 수 없다.
        [Test]
        public void 앞으로_계속_걸어도_사대_깊이를_못_넘는다()
        {
            var (world, archer) = Make();
            Walk(archer, horizontal: 0f, vertical: 1f);

            Run(world, 300);   // 6초 — 막는 게 없으면 20m 넘게 간다

            Assert.LessOrEqual(Position(archer).z, HalfDepth + 1e-3f,
                "사대를 넘어 과녁 쪽으로 걸어 나갔다 — 거리가 무의미해진다");
        }

        [Test]
        public void 옆으로_계속_걸어도_사대_폭을_못_넘는다()
        {
            var (world, archer) = Make();
            Walk(archer, horizontal: 1f, vertical: 0f);

            Run(world, 300);

            Assert.LessOrEqual(Position(archer).x, HalfWidth + 1e-3f, "사대 옆면을 넘었다");
        }

        //  ③ 몸은 과녁을 계속 본다.
        [Test]
        public void 걸어도_몸이_돌아가지_않는다()
        {
            var (world, archer) = Make();
            var before = archer.Get<GameFramework.World.Transform>().Rotation;
            Walk(archer, horizontal: 1f, vertical: 0f);

            Run(world, 30);

            var after = archer.Get<GameFramework.World.Transform>().Rotation;
            Assert.AreEqual(before.X, after.X, 1e-4f, "걷는 방향으로 몸이 돌았다");
            Assert.AreEqual(before.Y, after.Y, 1e-4f, "걷는 방향으로 몸이 돌았다 — 옆을 보고 쏘게 된다");
            Assert.AreEqual(before.Z, after.Z, 1e-4f);
            Assert.AreEqual(before.W, after.W, 1e-4f);
        }

        //  속도 0 = 이동을 안 켠 맵(원형). 이때는 중력조차 돌면 안 된다 — 사수가 바닥으로 떨어진다.
        [Test]
        public void 속도가_0이면_아무것도_안_움직인다()
        {
            var (world, archer) = Make(moveSpeed: 0f);
            Walk(archer, horizontal: 1f, vertical: 1f);

            Run(world, 100);

            var p = Position(archer);
            Assert.AreEqual(0f, p.x, 1e-4f, "속도가 0인데 옆으로 갔다");
            Assert.AreEqual(0f, p.y, 1e-4f, "속도가 0인데 중력이 돌아 떨어졌다");
            Assert.AreEqual(0f, p.z, 1e-4f);
        }

        //  사대를 모르는 몸(스탠스 없음)은 안 자른다 — 자를 기준이 없는데 원점으로 끌면
        //  맵 한복판으로 순간이동한다.
        [Test]
        public void 사대를_모르면_안_자른다()
        {
            var (world, archer) = Make();
            archer.Remove<ArcheryStance>();
            Walk(archer, horizontal: 1f, vertical: 0f);

            Run(world, 300);

            Assert.Greater(Position(archer).x, HalfWidth, "기준이 없는데 사대 안으로 잘렸다");
        }
    }
}
