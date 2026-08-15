using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    public class FlappyWorldTests
    {
        [Test]
        public void Tick_LeavesEntitiesUntouched_WhileMutationIsEmpty()
        {
            var registry = new EntityRegistry();
            var entity = new Entity("bird-1");
            entity.Add(new GameFramework.World.Transform());
            entity.Add(new Velocity());
            entity.Add(new Simulated());
            registry.Add(entity);

            var world = new FlappyWorld(registry, new WorldEventBuffer());
            world.Tick(1, 0.05f);

            Assert.AreEqual(System.Numerics.Vector3.Zero, entity.Get<GameFramework.World.Transform>().Position);
            Assert.AreEqual(System.Numerics.Vector3.Zero, entity.Get<Velocity>().Linear);
        }
    }
}
