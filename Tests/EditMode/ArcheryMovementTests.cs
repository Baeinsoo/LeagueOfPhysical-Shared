using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    //  사수가 사대 안에서 걷는다. 재는 것은 셋이다 —
    //   ① 입력대로 움직이나  ② 사대를 못 벗어나나  ③ 몸이 겨누는 쪽을 보나.
    //
    //  ②가 이 기능의 존재 이유다 — 앞으로 걸어 나갈 수 있으면 90m가 60m가 되어 거리 여섯이
    //  무의미해진다. ③은 몸과 화살이 따로 놀지 않는지를 잰다.
    public class ArcheryMovementTests
    {
        const float TickInterval = 0.02f;
        const float MoveSpeed = 4f;
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

        static void Walk(Entity archer, float horizontal, float vertical, float aimYaw = 0f)
        {
            archer.Get<InputBuffer>().Current = new InputCommand
            {
                Horizontal = horizontal, Vertical = vertical, AimYaw = aimYaw,
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

        //  ③ 몸은 **겨누는 쪽**을 본다 — 걷는 쪽이 아니다.
        //
        //  공용 이동 시스템은 걷는 쪽으로 몸을 돌린다(보통은 맞다). 활쏘기에서 그러면
        //  옆으로 걸으며 쏠 때 몸과 화살이 따로 놀아 **옆을 보고 쏘는 그림**이 된다.
        //  회전의 주인은 ArcheryAimSystem 하나이고, 이동이 쓴 회전은 되돌려진다.
        [Test]
        public void 몸은_걷는_쪽이_아니라_겨누는_쪽을_본다()
        {
            var (world, archer) = Make();
            //  오른쪽(+x)으로 걸으면서 앞(yaw 0)을 겨눈다.
            Walk(archer, horizontal: 1f, vertical: 0f, aimYaw: 0f);

            Run(world, 30);

            Assert.AreEqual(0f, Yaw(archer), 0.5f, "걷는 쪽으로 몸이 돌았다 — 옆을 보고 쏘게 된다");
        }

        [Test]
        public void 겨누는_쪽을_바꾸면_몸도_따라_돈다()
        {
            var (world, archer) = Make();
            Walk(archer, horizontal: 0f, vertical: 0f, aimYaw: 90f);

            Run(world, 5);

            Assert.AreEqual(90f, Yaw(archer), 0.5f, "겨누는 쪽을 돌렸는데 몸이 그대로다");
        }

        //  걷는 중에 겨누는 쪽을 돌려도 몸은 겨누는 쪽을 따라간다 — 두 곳이 회전을 쓰면
        //  여기서 실행 순서에 따라 값이 갈린다.
        [Test]
        public void 걸으면서_겨눠도_겨누는_쪽이_이긴다()
        {
            var (world, archer) = Make();
            Walk(archer, horizontal: -1f, vertical: 0f, aimYaw: 45f);

            Run(world, 30);

            Assert.AreEqual(45f, Yaw(archer), 0.5f, "이동이 회전을 가로챘다");
        }

        static float Yaw(Entity archer)
        {
            var r = archer.Get<GameFramework.World.Transform>().Rotation;
            return new Quaternion(r.X, r.Y, r.Z, r.W).eulerAngles.y;
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
