using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    public class PanchigiWorldTests
    {
        [Test]
        public void TickDoesNotMoveEntities()
        {
            // 판치기 동전은 PhysX가 굴린다 — 우리 시뮬은 아무것도 움직이지 않는다.
            var registry = new EntityRegistry();
            var entity = new Entity("coin1");
            entity.Add(new GameFramework.World.Transform
            {
                Position = new System.Numerics.Vector3(1f, 2f, 3f),
                Rotation = System.Numerics.Quaternion.Identity,
            });
            entity.Add(new Velocity { Linear = new System.Numerics.Vector3(5f, 0f, 0f) });
            entity.Add(new Simulated());
            registry.Add(entity);

            var world = new PanchigiWorld(registry, new WorldEventBuffer());
            world.Tick(1, 0.02f);

            Assert.AreEqual(1f, entity.Get<GameFramework.World.Transform>().Position.X, 1e-4f);
            Assert.AreEqual(2f, entity.Get<GameFramework.World.Transform>().Position.Y, 1e-4f);
            Assert.AreEqual(3f, entity.Get<GameFramework.World.Transform>().Position.Z, 1e-4f);
        }

        [Test]
        public void ExposesRegistryAndEventBuffer()
        {
            var registry = new EntityRegistry();
            var buffer = new WorldEventBuffer();

            var world = new PanchigiWorld(registry, buffer);

            Assert.AreSame(registry, world.EntityRegistry);
            Assert.AreSame(buffer, world.EventBuffer);
        }
    }
}
