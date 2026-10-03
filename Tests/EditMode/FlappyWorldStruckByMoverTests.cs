using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>움직이는 장애물이 가만히 있는 새를 쳐서 들어오면 기절한다(spec 2026-10-03 §1.3).</summary>
    public class FlappyWorldStruckByMoverTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown() { if (root != null) { Object.DestroyImmediate(root); } }

        //  축 (x, 3)에 매달린 3m 막대. 진폭 0이라 늘 수직 — x 근처의 y 0~3을 막는다.
        private FlappyPendulumField Pendulum(float x, float amplitude = 0f)
        {
            root = new GameObject("pendulum");
            root.transform.position = new Vector3(x, 3f, 0f);
            var p = root.AddComponent<FlappyPendulum>();
            p.Amplitude = amplitude; p.Period = 2.5f;
            var rod = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rod.transform.SetParent(root.transform, false);
            rod.transform.localPosition = new Vector3(0f, -1.5f, 0f);
            rod.transform.localScale = new Vector3(0.3f, 3f, 1f);
            var field = new FlappyPendulumField();
            field.Add(p);
            return field;
        }

        private static FlappyWorld World(FlappyPendulumField field, out Entity bird)
        {
            var registry = new EntityRegistry();
            bird = FlappyWorldFixture.Bird("bird-1");
            registry.Add(bird);
            var world = new FlappyWorld(registry, new WorldEventBuffer(),
                new FlappyMoveSystem(FlappyWorldFixture.Config()),
                new FlappyStunSystem(FlappyWorldFixture.Config()),
                new FlappyDashSystem(FlappyWorldFixture.Config()),
                new FinishSystem(new FinishLineBounds(FinishAxis.X), FinishAxis.X, increasing: true),
                new FlappyWindmillField(), new FlappyBoostPadField(),
                new FlappyWorldFixture.NeverHit(), new FlappyWorldFixture.NoopMotionBridge(),
                layerMask: ~0, pendulumField: field);
            world.GameplayStartTick = 0;
            return world;
        }

        [Test]
        public void 막대가_새_자리에_오면_기절한다()
        {
            var world = World(Pendulum(0f), out var bird);
            world.Tick(FlappyWorldFixture.StartTick, 0.02f);
            Assert.Greater(bird.Get<FlappyStun>().StunRemaining, 0f);
        }

        [Test]
        public void 멀리_있으면_기절하지_않는다()
        {
            var world = World(Pendulum(10f), out var bird);
            world.Tick(FlappyWorldFixture.StartTick, 0.02f);
            Assert.AreEqual(0f, bird.Get<FlappyStun>().StunRemaining);
        }

        [Test]
        public void 무적이면_기절하지_않는다()
        {
            var world = World(Pendulum(0f), out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;
            world.Tick(FlappyWorldFixture.StartTick, 0.02f);
            Assert.AreEqual(0f, bird.Get<FlappyStun>().StunRemaining);
        }

        [Test]
        public void 진자가_없으면_예전과_같다()
        {
            //  pendulumField 없이 만든 세계(기존 호출부)는 이 규칙을 아예 안 탄다.
            var registry = new EntityRegistry();
            var bird = FlappyWorldFixture.Bird("bird-1");
            registry.Add(bird);
            var world = new FlappyWorld(registry, new WorldEventBuffer(),
                new FlappyMoveSystem(FlappyWorldFixture.Config()), new FlappyStunSystem(FlappyWorldFixture.Config()),
                new FlappyDashSystem(FlappyWorldFixture.Config()),
                new FinishSystem(new FinishLineBounds(FinishAxis.X), FinishAxis.X, increasing: true),
                new FlappyWindmillField(), new FlappyBoostPadField(),
                new FlappyWorldFixture.NeverHit(), new FlappyWorldFixture.NoopMotionBridge(), layerMask: ~0);
            world.GameplayStartTick = 0;
            world.Tick(FlappyWorldFixture.StartTick, 0.02f);
            Assert.AreEqual(0f, bird.Get<FlappyStun>().StunRemaining);
        }
    }
}
