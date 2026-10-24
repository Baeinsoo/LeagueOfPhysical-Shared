using System.Collections.Generic;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 기절·무적인 새는 움직이는 장애물을 통과한다(10-24 사용자 결정 "기절·무적 동안 통과").
    /// 전엔 기절해 멈춘 새를 장애물이 밀어내기(Depenetrate)로 끌고 다니다가, 무적이 끝나는 틱에도
    /// 여전히 겹쳐 있으면 곧바로 다시 기절시켜 피할 수 없는 연속 기절이 났다(낙석·진자 구간 제보).
    /// </summary>
    public class FlappyWorldMoverGhostTests
    {
        //  에디터에 열린 씬의 지오메트리와 안 겹치게 멀리서 판을 꾸민다 — 실제 물리 질의를 쓴다.
        private const float X0 = 5000f;
        private const float Dt = 0.02f;

        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
            {
                if (go != null) { Object.DestroyImmediate(go); }
            }
            spawned.Clear();
        }

        //  진폭 0인 진자에 매단 상자 — 늘 그 자리에 서 있는 "움직이는 장애물" 콜라이더다.
        private FlappyPendulumField Mover(FlappyPendulumField field, float x, float y, float w, float h)
        {
            var root = new GameObject("mover");
            spawned.Add(root);
            root.transform.position = new Vector3(x, y, 0f);
            var p = root.AddComponent<FlappyPendulum>();
            p.Amplitude = 0f; p.Period = 2.5f;
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.SetParent(root.transform, false);
            box.transform.localScale = new Vector3(w, h, 1f);
            field.Add(p);
            Physics.SyncTransforms();
            return field;
        }

        //  장애물 표식이 없는 상자 — 정적인 맵 벽이다.
        private void Wall(float x, float y, float w, float h)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            spawned.Add(box);
            box.transform.position = new Vector3(x, y, 0f);
            box.transform.localScale = new Vector3(w, h, 1f);
            Physics.SyncTransforms();
        }

        //  밀어내기를 부를 때마다 장애물이 끌고 가듯 아래로 민다(낙석이 한 틱 0.18 m 내려가는 것과 같은 꼴).
        //  누구에게 불렸는지 센다 — 기절·무적인 새에겐 안 불려야 한다.
        private class DragBridge : IMotionBridge
        {
            public static readonly System.Numerics.Vector3 Drag = new System.Numerics.Vector3(0f, -0.18f, 0f);
            public int DepenetrateCalls;
            public void SyncTransforms() { }
            public void Separate(Entity entity) { }
            public void PushMotion(Entity entity) { }
            public System.Numerics.Vector3 Depenetrate(Entity entity)
            {
                DepenetrateCalls++;
                entity.Get<GameFramework.World.Transform>().Position += Drag;
                return Drag;
            }
        }

        private static FlappyWorld World(FlappyPendulumField field, GameFramework.Physics.ICollisionQuery query,
            IMotionBridge bridge, out Entity bird)
        {
            var registry = new EntityRegistry();
            bird = FlappyWorldFixture.Bird("bird-1");
            bird.Get<GameFramework.World.Transform>().Position = new System.Numerics.Vector3(X0, 0f, 0f);
            registry.Add(bird);
            var world = new FlappyWorld(registry, new WorldEventBuffer(),
                new FlappyMoveSystem(FlappyWorldFixture.Config()),
                new FlappyStunSystem(FlappyWorldFixture.Config()),
                new FlappyDashSystem(FlappyWorldFixture.Config()),
                new FinishSystem(new FinishLineBounds(FinishAxis.X), FinishAxis.X, increasing: true),
                new FlappyWindmillField(), new FlappyBoostPadField(),
                query, bridge, layerMask: ~0, pendulumField: field);
            world.GameplayStartTick = 0;
            return world;
        }

        private static System.Numerics.Vector3 Pos(Entity bird) => bird.Get<GameFramework.World.Transform>().Position;

        // ── 밀어내기 ──────────────────────────────────────────────

        [Test]
        public void 기절한_새는_장애물에_끌려가지_않는다()
        {
            //  새를 통째로 덮는 장애물 속에서 이미 기절해 있다.
            var field = Mover(new FlappyPendulumField(), X0, 0f, 20f, 20f);
            var bridge = new DragBridge();
            var world = World(field, new FlappyWorldFixture.NeverHit(), bridge, out var bird);
            bird.Get<FlappyStun>().StunRemaining = 0.8f;
            var start = Pos(bird);

            for (long t = 1; t <= 10; t++)
            {
                world.Tick(t, Dt);
            }

            Assert.AreEqual(start, Pos(bird), "기절해 멈춘 새가 밀어내기에 끌려갔다");
            Assert.AreEqual(0, bridge.DepenetrateCalls);
        }

        [Test]
        public void 무적인_새도_밀어내지_않는다()
        {
            var bridge = new DragBridge();
            var world = World(new FlappyPendulumField(), new FlappyWorldFixture.NeverHit(), bridge, out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.AreEqual(0, bridge.DepenetrateCalls);
        }

        [Test]
        public void 기절도_무적도_아니면_전처럼_밀어낸다()
        {
            var bridge = new DragBridge();
            var world = World(new FlappyPendulumField(), new FlappyWorldFixture.NeverHit(), bridge, out var bird);

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.AreEqual(1, bridge.DepenetrateCalls);
        }

        // ── 무적은 장애물 속에서 안 끝난다 ─────────────────────────

        [Test]
        public void 장애물_속에서_기절과_무적이_다_지나도_다시_기절하지_않는다()
        {
            //  기절 0.8 + 무적 0.6초 동안 떨어지고 나아가도 못 벗어날 만큼 큰 장애물.
            var field = Mover(new FlappyPendulumField(), X0, 0f, 400f, 400f);
            var world = World(field, new FlappyWorldFixture.NeverHit(), new FlappyWorldFixture.NoopMotionBridge(), out var bird);
            var stun = bird.Get<FlappyStun>();

            world.Tick(1, Dt);
            Assert.Greater(stun.StunRemaining, 0f, "처음 맞을 때는 기절해야 한다");

            bool released = false;
            for (long t = 2; t <= 150; t++)   // 3초 — 기절·무적을 합친 1.4초를 넉넉히 넘긴다
            {
                world.Tick(t, Dt);
                if (stun.StunRemaining <= 0f)
                {
                    released = true;
                }
                if (released)
                {
                    Assert.AreEqual(0f, stun.StunRemaining, $"tick {t}: 장애물 속에서 다시 기절했다");
                    Assert.Greater(stun.InvulnRemaining, 0f, $"tick {t}: 장애물 속인데 무적이 끝났다");
                }
            }
            Assert.IsTrue(released);
        }

        [Test]
        public void 장애물을_빠져나온_뒤에_다시_맞으면_기절한다()
        {
            var field = Mover(new FlappyPendulumField(), X0, 0f, 400f, 400f);
            var world = World(field, new FlappyWorldFixture.NeverHit(), new FlappyWorldFixture.NoopMotionBridge(), out var bird);
            var stun = bird.Get<FlappyStun>();
            var transform = bird.Get<GameFramework.World.Transform>();
            long t = 1;
            for (; t <= 100; t++)   // 2초 — 기절이 풀리고 무적이 붙들려 있다
            {
                world.Tick(t, Dt);
            }
            Assert.AreEqual(0f, stun.StunRemaining);

            //  장애물 밖(1000 m 앞)으로 옮기면 붙들린 무적이 곧 끝난다.
            transform.Position = new System.Numerics.Vector3(X0 + 1000f, 0f, 0f);
            for (int i = 0; i < 3; i++, t++)
            {
                world.Tick(t, Dt);
            }
            Assert.AreEqual(0f, stun.InvulnRemaining, "장애물 밖인데 무적이 안 끝났다");
            Assert.AreEqual(0f, stun.StunRemaining);

            //  다시 장애물 속으로 — 새로 맞았으니 기절한다.
            transform.Position = new System.Numerics.Vector3(X0, 0f, 0f);
            bird.Get<Velocity>().Linear = System.Numerics.Vector3.Zero;
            world.Tick(t, Dt);
            Assert.Greater(stun.StunRemaining, 0f);
        }

        [Test]
        public void 무적이_이번_틱에_끝나도_장애물_속이면_기절하지_않는다()
        {
            //  시간 감소(①)가 맞음 검사(①')보다 먼저라, 이 틱 처음엔 무적이었던 새가 검사 때는 0이 된다.
            var field = Mover(new FlappyPendulumField(), X0, 0f, 400f, 400f);
            var world = World(field, new FlappyWorldFixture.NeverHit(), new FlappyWorldFixture.NoopMotionBridge(), out var bird);
            var stun = bird.Get<FlappyStun>();
            stun.InvulnRemaining = Dt;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.AreEqual(0f, stun.StunRemaining, "무적이던 새가 장애물 속에서 기절했다");
            Assert.Greater(stun.InvulnRemaining, 0f, "장애물 속인데 무적이 끝났다");
        }

        // ── 이동 sweep ───────────────────────────────────────────

        //  새 캡슐(반지름 0.45)의 앞면은 X0 + 0.45. 얇은 판(폭 0.05)을 X0 + 0.55에 두면 앞면까지 0.075 —
        //  한 틱 전진 0.22 안이다.
        private const float PlateX = X0 + 0.55f;

        [Test]
        public void 무적이_아니면_장애물에_막히고_기절한다()
        {
            var field = Mover(new FlappyPendulumField(), PlateX, 0.45f, 0.05f, 6f);
            var world = World(field, new GameFramework.Physics.UnityCollisionQuery(), new FlappyWorldFixture.NoopMotionBridge(), out var bird);

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.Less(Pos(bird).X, X0 + 0.1f, "판이 앞을 막지 않았다 — 판 배치가 틀렸다");
            Assert.Greater(bird.Get<FlappyStun>().StunRemaining, 0f);
        }

        [Test]
        public void 무적인_새는_움직이는_장애물을_뚫고_지나간다()
        {
            var field = Mover(new FlappyPendulumField(), PlateX, 0.45f, 0.05f, 6f);
            var world = World(field, new GameFramework.Physics.UnityCollisionQuery(), new FlappyWorldFixture.NoopMotionBridge(), out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.Greater(Pos(bird).X, X0 + 0.2f, "무적인 새가 장애물에 막혔다");
            Assert.AreEqual(0f, bird.Get<FlappyStun>().StunRemaining);
        }

        [Test]
        public void 무적이어도_정적인_벽엔_막힌다()
        {
            Wall(PlateX, 0.45f, 0.05f, 6f);
            var world = World(new FlappyPendulumField(), new GameFramework.Physics.UnityCollisionQuery(),
                new FlappyWorldFixture.NoopMotionBridge(), out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.Less(Pos(bird).X, X0 + 0.1f, "무적인 새가 맵 벽을 뚫었다");
        }

        [Test]
        public void 무적이면_장애물_바로_뒤의_벽까지_가서_막힌다()
        {
            //  판(앞면 X0+0.525)을 지나 그 바로 뒤 벽(앞면 X0+0.6)에서 멈춘다 — 판 히트만 버리고 끝내면
            //  벽까지 못 보고, 판 앞에서 멈추면 장애물을 못 뚫은 것이다.
            var field = Mover(new FlappyPendulumField(), PlateX, 0.45f, 0.05f, 6f);
            Wall(X0 + 0.65f, 0.45f, 0.1f, 6f);
            var world = World(field, new GameFramework.Physics.UnityCollisionQuery(), new FlappyWorldFixture.NoopMotionBridge(), out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            //  벽 앞면 0.6 − 반지름 0.45 − SkinWidth 0.02 = 0.13
            Assert.AreEqual(X0 + 0.13f, Pos(bird).X, 0.01f);
        }
    }
}
