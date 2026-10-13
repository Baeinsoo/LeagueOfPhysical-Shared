using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>승강기 파이프가 내려와 가만히 있는 새를 치면 기절한다 — 셔터와 같은 규칙(FlappyWorldStruckByShutterTests).</summary>
    public class FlappyWorldStruckByLiftTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown() { if (root != null) { Object.DestroyImmediate(root); } }

        //  x에 선 승강기. 파이프(0.8 × 1)는 쉬는 자리 y 2.5~3.5, 진폭 3 · 주기 2초(100틱).
        private FlappyLiftField Lift(float x, float phase)
        {
            root = new GameObject("lift");
            root.transform.position = new Vector3(x, 0f, 0f);
            var lift = root.AddComponent<FlappyLift>();
            lift.Amplitude = 3f; lift.Period = 2f; lift.Phase = phase;
            var cage = new GameObject("Cage");
            cage.transform.SetParent(root.transform, false);
            var pipe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pipe.transform.SetParent(cage.transform, false);
            pipe.transform.localPosition = new Vector3(0f, 3f, 0f);
            pipe.transform.localScale = new Vector3(0.8f, 1f, 1f);
            lift.Cage = cage.transform;
            var field = new FlappyLiftField();
            field.Add(lift);
            return field;
        }

        private static FlappyWorld World(FlappyLiftField field, out Entity bird)
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
                layerMask: ~0, liftField: field);
            world.GameplayStartTick = 0;
            return world;
        }

        //  1틱(주기의 0.01)에 위상 270°면 sin ≈ −1 → 파이프가 3 m 내려와 y −0.5~0.5, 새(y 0~0.9)를 덮는다.
        private const float Down = 270f;
        //  위상 90°면 3 m 올라가 y 5.5~6.5.
        private const float Up = 90f;

        [Test]
        public void 내려온_파이프가_새를_치면_기절한다()
        {
            var world = World(Lift(0f, Down), out var bird);
            world.Tick(FlappyWorldFixture.StartTick, 0.02f);
            Assert.Greater(bird.Get<FlappyStun>().StunRemaining, 0f);
        }

        [Test]
        public void 열려_있으면_지나도_기절하지_않는다()
        {
            //  쉬는 자리(y 2.5~3.5)는 새와 안 겹친다 — 세우는 순서는 위 Down 시험이 지킨다(틱 자세를 안 세우면 Down도 기절하지 않는다).
            var world = World(Lift(0f, Up), out var bird);
            world.Tick(FlappyWorldFixture.StartTick, 0.02f);
            Assert.AreEqual(0f, bird.Get<FlappyStun>().StunRemaining);
        }

        [Test]
        public void 멀리_있으면_기절하지_않는다()
        {
            var world = World(Lift(10f, Down), out var bird);
            world.Tick(FlappyWorldFixture.StartTick, 0.02f);
            Assert.AreEqual(0f, bird.Get<FlappyStun>().StunRemaining);
        }

        [Test]
        public void 무적이면_기절하지_않는다()
        {
            var world = World(Lift(0f, Down), out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;
            world.Tick(FlappyWorldFixture.StartTick, 0.02f);
            Assert.AreEqual(0f, bird.Get<FlappyStun>().StunRemaining);
        }
    }
}
