using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    internal static class MountedArcheryWorldFixture
    {
        public const float Dt = 0.02f;
        public const float EyeHeight = 2.5f;

        public static readonly MountSettings Settings = new MountSettings(
            maxSpeed: 13f, acceleration: 9f, deceleration: 16f,
            turnRateStoppedDeg: 172f, turnRateAtMaxDeg: 92f,
            sharpTurnDeg: 100f, sharpTurnSpeedFactor: 0.35f, drawSpeedFactor: 0.5f);

        public static readonly MountedArcheryConfig Config
            = new MountedArcheryConfig(150f, 40f, 80f, EyeHeight, Settings);

        public static MountedArcheryWorld Make(EntityRegistry registry, bool turnBodyToAim = false,
                                               WorldEventBuffer eventBuffer = null)
        {
            return new MountedArcheryWorld(registry, eventBuffer ?? new WorldEventBuffer(),
                new ArcheryAimSystem(Config.ToBowConfig(), Config.RiderEyeHeight, turnBodyToAim),
                new MountSystem(Config.Mount),
                new KinematicMoveSystem(new FlappyWorldFixture.NeverHit(), 0),
                new FlappyWorldFixture.NoopMotionBridge(),
                Dt);
        }

        public static Entity Rider(EntityRegistry registry, float yawDeg = 0f)
        {
            var rider = new Entity("rider-1");
            rider.Add(new GameFramework.World.Transform
            {
                Rotation = Quaternion.Euler(0f, yawDeg, 0f).ToNumerics(),
            });
            rider.Add(new Velocity());
            rider.Add(new ArcheryAim());
            rider.Add(new InputBuffer());
            rider.Add(new Simulated());
            rider.Add(new Mount());
            rider.Add(new CapsuleShape(0.4f, 1.8f));
            registry.Add(rider);
            return rider;
        }
    }

    public class MountedArcheryWorldTests
    {
        const float Dt = MountedArcheryWorldFixture.Dt;
        const long HoldTicks = 10;

        static (MountedArcheryWorld world, Entity rider) Make(bool turnBodyToAim = false, float yawDeg = 0f)
        {
            var registry = new EntityRegistry();
            var rider = MountedArcheryWorldFixture.Rider(registry, yawDeg);
            return (MountedArcheryWorldFixture.Make(registry, turnBodyToAim), rider);
        }

        static void Feed(Entity entity, float h, float v, bool drawing, bool release, float aimYaw = 0f)
        {
            entity.Get<InputBuffer>().Current = new InputCommand
            {
                Horizontal = h, Vertical = v, Drawing = drawing, Release = release,
                DrawRatio = 1f, AimYaw = aimYaw,
            };
        }

        static void DrawThenRelease(Entity rider, MountedArcheryWorld world)
        {
            for (long t = 1; t <= HoldTicks; t++)
            {
                Feed(rider, 0f, 0f, drawing: true, release: false);
                world.Tick(t, Dt);
            }
            Feed(rider, 0f, 0f, drawing: false, release: true);
            world.Tick(HoldTicks + 1, Dt);
        }

        static float Yaw(Entity e)
            => Mathf.DeltaAngle(0f, e.Get<GameFramework.World.Transform>().Rotation.ToUnity().eulerAngles.y);

        [Test]
        public void 앞으로_몰면_앞으로_간다()
        {
            var (world, rider) = Make();

            for (long t = 1; t <= 50; t++)
            {
                Feed(rider, 0f, 1f, false, false);
                world.Tick(t, Dt);
            }

            Assert.Greater(rider.Get<GameFramework.World.Transform>().Position.Z, 0f);
        }

        [Test]
        public void 조준해도_말_머리는_탈것이_정한다()
        {
            var (world, rider) = Make();

            for (long t = 1; t <= 10; t++)
            {
                Feed(rider, 0f, 1f, drawing: true, release: false, aimYaw: 90f);
                world.Tick(t, Dt);
            }

            Assert.AreEqual(0f, Yaw(rider), 0.01f);
        }

        [Test]
        public void 머리가_돌아가_있어도_그_방향으로_간다()
        {
            var (world, rider) = Make(yawDeg: 40f);
            var transform = rider.Get<GameFramework.World.Transform>();
            var start = transform.Position;
            float rad = 40f * Mathf.Deg2Rad;

            for (long t = 1; t <= 50; t++)
            {
                Feed(rider, Mathf.Sin(rad), Mathf.Cos(rad), false, false);
                world.Tick(t, Dt);
            }

            float dx = transform.Position.X - start.X;
            float dz = transform.Position.Z - start.Z;
            Assert.Greater(Mathf.Sqrt(dx * dx + dz * dz), 0.5f);
            Assert.AreEqual(40f, Mathf.Atan2(dx, dz) * Mathf.Rad2Deg, 1f);
            Assert.AreEqual(40f, Yaw(rider), 0.5f);
        }

        [Test]
        public void 쏘면_기수_눈높이에서_나간다()
        {
            var (world, rider) = Make();

            DrawThenRelease(rider, world);

            Assert.AreEqual(1, world.Shots.Count);
            float y = rider.Get<GameFramework.World.Transform>().Position.Y;
            Assert.AreEqual(y + MountedArcheryWorldFixture.EyeHeight, world.Shots[0].Origin.y, 1e-3f);
        }

        [Test]
        public void 쏘면_사건이_쌓인다()
        {
            var (world, rider) = Make();

            DrawThenRelease(rider, world);

            int fired = 0;
            foreach (var e in world.EventBuffer.Snapshot)
            {
                if (e is ArcheryShotFiredEvent f)
                {
                    fired++;
                    Assert.AreEqual("rider-1", f.shooterId);
                    Assert.AreEqual(world.Shots[0].FireTick, f.fireTick);
                }
            }
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void 되감아_다시_돌리면_같은_자리()
        {
            var (world, rider) = Make();
            var transform = rider.Get<GameFramework.World.Transform>();

            //  이동·방향 전환·당김·뗌이 모두 20틱 구간 안에 들어가는 입력열
            void Step(long t)
            {
                bool draw = t >= 8 && t <= 17;
                bool release = t == 18;
                Feed(rider, t % 7 < 3 ? 0.6f : -0.2f, 1f, draw, release);
                world.Tick(t, Dt);
            }

            for (long t = 1; t <= 5; t++) { Step(t); }
            world.SaveState(5);

            for (long t = 6; t <= 25; t++) { Step(t); }
            var pos1 = transform.Position;
            float yaw1 = Yaw(rider);
            int shots1 = world.Shots.Count;
            Assert.AreEqual(1, shots1);
            Assert.Greater(pos1.Z, 0f);

            world.LoadState(5);
            for (long t = 6; t <= 25; t++) { Step(t); }

            Assert.AreEqual(pos1.X, transform.Position.X, 1e-4f);
            Assert.AreEqual(pos1.Z, transform.Position.Z, 1e-4f);
            Assert.AreEqual(yaw1, Yaw(rider), 1e-3f);
            Assert.AreEqual(shots1, world.Shots.Count);
        }

        [Test]
        public void 남의_화살을_받고_지울_수_있다()
        {
            var (world, _) = Make();

            world.IngestRemoteShot(new ArcheryShot("rider-9", 5, Vector3.zero, new Vector3(0f, 0f, 30f)));
            Assert.AreEqual(1, world.Shots.Count);

            world.CancelShots("rider-9", 4, 5);

            Assert.AreEqual(0, world.Shots.Count);
        }

        static ArcheryShot RemoteShot(long fireTick)
            => new ArcheryShot("rider-9", fireTick, Vector3.zero, new Vector3(0f, 0f, 30f));

        static int CountShots(MountedArcheryWorld world, string shooterId, long fireTick)
        {
            int n = 0;
            foreach (var s in world.Shots)
            {
                if (s.ShooterId == shooterId && s.FireTick == fireTick) { n++; }
            }
            return n;
        }

        // 남의 화살은 저장본보다 늦게 도착한다 — 그 저장본으로 되감아도 서버가 확정한 화살은 남아야 한다.
        [Test]
        public void 되감아도_서버가_확정한_남의_화살은_남는다()
        {
            var (world, rider) = Make();
            for (long t = 1; t <= 10; t++)
            {
                Feed(rider, 0f, 0f, false, false);
                world.Tick(t, Dt);
            }
            world.SaveState(10);

            world.IngestRemoteShot(RemoteShot(8));
            world.LoadState(10);

            Assert.AreEqual(1, CountShots(world, "rider-9", 8));
        }

        [Test]
        public void 확정된_남의_화살도_수명이_지나면_사라진다()
        {
            var (world, rider) = Make();
            world.SaveState(0);
            world.IngestRemoteShot(RemoteShot(1));

            long lifetimeTicks = (long)(ArcheryTrajectory.LifetimeSeconds / Dt) + 2;
            for (long t = 1; t <= lifetimeTicks; t++)
            {
                Feed(rider, 0f, 0f, false, false);
                world.Tick(t, Dt);
            }
            Assert.AreEqual(0, CountShots(world, "rider-9", 1), "수명이 지나면 목록에서 빠진다");

            world.SaveState(lifetimeTicks);
            world.LoadState(lifetimeTicks);
            Assert.AreEqual(0, CountShots(world, "rider-9", 1), "수명이 지난 화살을 되감기가 되살리면 안 된다");
        }

        [Test]
        public void 같은_남의_화살을_두_번_받아도_하나다()
        {
            var (world, rider) = Make();
            world.SaveState(0);

            world.IngestRemoteShot(RemoteShot(1));
            world.IngestRemoteShot(RemoteShot(1));
            Assert.AreEqual(1, CountShots(world, "rider-9", 1));

            // 되감기가 다시 넣은 뒤 또 받아도 늘지 않는다.
            world.LoadState(0);
            world.IngestRemoteShot(RemoteShot(1));
            Assert.AreEqual(1, CountShots(world, "rider-9", 1));
        }

        [Test]
        public void 취소한_남의_화살은_되감아도_안_돌아온다()
        {
            var (world, _) = Make();
            world.SaveState(0);

            world.IngestRemoteShot(RemoteShot(5));
            world.CancelShots("rider-9", 4, 5);
            world.LoadState(0);

            Assert.AreEqual(0, CountShots(world, "rider-9", 5));
        }
    }
}
