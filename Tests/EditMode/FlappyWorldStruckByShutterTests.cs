using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>셔터 문이 내려와 가만히 있는 새를 치면 기절한다 — 진자와 같은 규칙(FlappyWorldStruckByMoverTests).</summary>
    public class FlappyWorldStruckByShutterTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown() { if (root != null) { Object.DestroyImmediate(root); } }

        //  x에 선 문. 닫히면 y 0~3을 막고, 다 열리면 Travel 3만큼 올라가 y 3~6에 있다.
        //  주기 2.5초 = 125틱, 열림 0.4·움직임 0.15 → 닫힘 [0,0.3) 올라감 [0.3,0.45) 열림 [0.45,0.85) 내려옴 [0.85,1).
        private FlappyShutterField Shutter(float x, float phase)
        {
            root = new GameObject("shutter");
            root.transform.position = new Vector3(x, 1.5f, 0f);
            var s = root.AddComponent<FlappyShutter>();
            s.Travel = 3f; s.Period = 2.5f; s.OpenShare = 0.4f; s.MoveShare = 0.15f; s.Phase = phase;
            var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Door";
            door.transform.SetParent(root.transform, false);
            door.transform.localScale = new Vector3(0.8f, 3f, 1f);
            s.Door = door.transform;
            var field = new FlappyShutterField();
            field.Add(s);
            return field;
        }

        private static FlappyWorld World(FlappyShutterField field, out Entity bird)
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
                layerMask: ~0, shutterField: field);
            world.GameplayStartTick = 0;
            return world;
        }

        //  1틱(0.02초 = 주기의 0.008)에 내려옴 끝자락 u≈0.997 — 문 바닥이 y≈0.06, 새(y 0~0.9)를 덮는다.
        private const float Descending = 356f;
        //  1틱에 u≈0.608 — 다 열려 문은 y 3~6.
        private const float Open = 216f;

        [Test]
        public void 내려오는_문이_새를_치면_기절한다()
        {
            var world = World(Shutter(0f, Descending), out var bird);
            world.Tick(FlappyWorldFixture.StartTick, 0.02f);
            Assert.Greater(bird.Get<FlappyStun>().StunRemaining, 0f);
        }

        [Test]
        public void 열려_있으면_밑을_지나도_기절하지_않는다()
        {
            //  자세를 이 틱으로 세운 뒤에 겹침을 본다 — 순서가 뒤집히면 0틱 자리(닫힘)로 판정해 기절한다.
            var world = World(Shutter(0f, Open), out var bird);
            world.Tick(FlappyWorldFixture.StartTick, 0.02f);
            Assert.AreEqual(0f, bird.Get<FlappyStun>().StunRemaining);
        }

        [Test]
        public void 멀리_있으면_기절하지_않는다()
        {
            var world = World(Shutter(10f, Descending), out var bird);
            world.Tick(FlappyWorldFixture.StartTick, 0.02f);
            Assert.AreEqual(0f, bird.Get<FlappyStun>().StunRemaining);
        }

        [Test]
        public void 무적이면_기절하지_않는다()
        {
            var world = World(Shutter(0f, Descending), out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;
            world.Tick(FlappyWorldFixture.StartTick, 0.02f);
            Assert.AreEqual(0f, bird.Get<FlappyStun>().StunRemaining);
        }
    }
}
