using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>떨어지는 낙석이 가만히 있는 새를 치면 기절한다 — 셔터와 같은 규칙(FlappyWorldStruckByShutterTests).</summary>
    public class FlappyWorldStruckByRockfallTests
    {
        private GameObject root;

        [TearDown]
        public void TearDown() { if (root != null) { Object.DestroyImmediate(root); } }

        //  천장 y 6, 바위 1.2, 9 m/s, 낙하 거리 7.2(통로 6 + 바위 1.2), 주기 2초.
        private FlappyRockfallField Rockfall(float x, float phase)
        {
            root = new GameObject("rockfall");
            root.transform.position = new Vector3(x, 6f, 0f);
            var r = root.AddComponent<FlappyRockfall>();
            r.Period = 2f; r.Speed = 9f; r.Drop = 7.2f; r.Size = 1.2f; r.HiddenRise = 5.6f; r.Phase = phase;
            var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rock.name = "Rock";
            rock.transform.SetParent(root.transform, false);
            rock.transform.localPosition = new Vector3(0f, 5.6f, 0f);
            rock.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
            r.Rock = rock.transform;
            var field = new FlappyRockfallField();
            field.Add(r);
            return field;
        }

        private static FlappyWorld World(FlappyRockfallField field, out Entity bird)
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
                layerMask: ~0, rockfallField: field);
            world.GameplayStartTick = 0;
            return world;
        }

        //  1틱에 주기 안 0.5556초 → 5.0 m 내려와 바위 y −0.2~1.0, 새(y 0~0.9)를 덮는다. (0.01 + 96.4/360)·2 = 0.5556
        private const float Falling = 96.4f;
        //  1틱에 1.52초 → 13.7 m > 7.2 — 천장 속에 숨어 있다.
        private const float Hidden = 270f;

        [Test]
        public void 떨어지는_바위가_새를_치면_기절한다()
        {
            var world = World(Rockfall(0f, Falling), out var bird);
            world.Tick(FlappyWorldFixture.StartTick, 0.02f);
            Assert.Greater(bird.Get<FlappyStun>().StunRemaining, 0f);
        }

        [Test]
        public void 숨어_있으면_밑을_지나도_기절하지_않는다()
        {
            var world = World(Rockfall(0f, Hidden), out var bird);
            world.Tick(FlappyWorldFixture.StartTick, 0.02f);
            Assert.AreEqual(0f, bird.Get<FlappyStun>().StunRemaining);
        }

        [Test]
        public void 멀리_있으면_기절하지_않는다()
        {
            var world = World(Rockfall(10f, Falling), out var bird);
            world.Tick(FlappyWorldFixture.StartTick, 0.02f);
            Assert.AreEqual(0f, bird.Get<FlappyStun>().StunRemaining);
        }

        [Test]
        public void 무적이면_기절하지_않는다()
        {
            var world = World(Rockfall(0f, Falling), out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;
            world.Tick(FlappyWorldFixture.StartTick, 0.02f);
            Assert.AreEqual(0f, bird.Get<FlappyStun>().StunRemaining);
        }
    }
}
