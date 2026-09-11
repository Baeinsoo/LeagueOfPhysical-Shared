using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    public class ArcheryWorldTests
    {
        const float TickInterval = 0.02f;

        static (ArcheryWorld world, EntityRegistry registry, Entity archer) Make()
        {
            var registry = new EntityRegistry();
            var archer = new Entity("archer-1");
            archer.Add(new GameFramework.World.Transform());
            // Velocity가 없으면 WorldBase.SaveState가 이 몸의 프레임을 기록하지 않고,
            // 그러면 LoadState가 프레임을 못 찾아 LoadGameState까지 가지도 못한다.
            archer.Add(new Velocity());
            archer.Add(new ArcheryAim());
            archer.Add(new InputBuffer());
            archer.Add(new Simulated());
            registry.Add(archer);

            var world = new ArcheryWorld(registry, new WorldEventBuffer(), new ArcheryAimSystem(), TickInterval);
            return (world, registry, archer);
        }

        static void Feed(Entity entity, bool drawing, bool release)
        {
            entity.Get<InputBuffer>().Current = new InputCommand { Drawing = drawing, Release = release };
        }

        [Test]
        public void 쏘면_화살_목록에_들어간다()
        {
            var (world, _, archer) = Make();

            Feed(archer, drawing: true, release: false);
            world.Tick(1, TickInterval);
            Feed(archer, drawing: false, release: true);
            world.Tick(2, TickInterval);

            Assert.AreEqual(1, world.Shots.Count);
            Assert.AreEqual("archer-1", world.Shots[0].ShooterId);
        }

        [Test]
        public void 수명이_지난_화살은_사라진다()
        {
            var (world, _, archer) = Make();

            Feed(archer, drawing: true, release: false);
            world.Tick(1, TickInterval);
            Feed(archer, drawing: false, release: true);
            world.Tick(2, TickInterval);
            Assert.AreEqual(1, world.Shots.Count);

            Feed(archer, drawing: false, release: false);
            long expiryTick = 2 + (long)(ArcheryTrajectory.LifetimeSeconds / TickInterval) + 1;
            world.Tick(expiryTick, TickInterval);

            Assert.AreEqual(0, world.Shots.Count);
        }

        [Test]
        public void 되감으면_당김이_되살아난다()
        {
            var (world, _, archer) = Make();

            Feed(archer, drawing: true, release: false);
            world.Tick(10, TickInterval);
            world.SaveState(10);
            Assert.IsTrue(archer.Get<ArcheryAim>().Drawing);

            Feed(archer, drawing: false, release: true);
            world.Tick(11, TickInterval);
            Assert.IsFalse(archer.Get<ArcheryAim>().Drawing);

            world.LoadState(10);

            Assert.IsTrue(archer.Get<ArcheryAim>().Drawing);
            Assert.AreEqual(10, archer.Get<ArcheryAim>().DrawStartTick);
        }

        [Test]
        public void 되감으면_없던_화살도_사라진다()
        {
            var (world, _, archer) = Make();

            Feed(archer, drawing: true, release: false);
            world.Tick(10, TickInterval);
            world.SaveState(10);

            Feed(archer, drawing: false, release: true);
            world.Tick(11, TickInterval);
            Assert.AreEqual(1, world.Shots.Count);

            world.LoadState(10);

            Assert.AreEqual(0, world.Shots.Count);
        }

        [Test]
        public void 되감아도_원격_엔티티_조준은_안_건드린다()
        {
            var (world, registry, archer) = Make();

            // 남의 몸 — 예측 대상이 아니라 스냅샷으로 따라온다(Simulated 없음).
            var remote = new Entity("archer-2");
            remote.Add(new GameFramework.World.Transform());
            remote.Add(new Velocity());
            remote.Add(new ArcheryAim { Yaw = 10f });
            remote.Add(new InputBuffer());
            registry.Add(remote);

            world.SaveState(10);

            // 되감는 사이 네트워크로 남의 새 조준이 도착한 상황.
            remote.Get<ArcheryAim>().Yaw = 77f;
            world.LoadState(10);

            Assert.AreEqual(77f, remote.Get<ArcheryAim>().Yaw, 1e-3f);   // 옛 값으로 덮이면 안 된다
        }
    }
}
