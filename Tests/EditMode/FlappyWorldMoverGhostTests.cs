using System.Collections.Generic;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 기절·무적인 새는 <b>치는</b> 장애물(진자·풍차·낙석)을 통과한다(10-24 사용자 결정 "기절·무적 동안 통과").
    /// 전엔 기절해 멈춘 새를 장애물이 밀어내기(Depenetrate)로 끌고 다니다가, 무적이 끝나는 틱에도
    /// 여전히 겹쳐 있으면 곧바로 다시 기절시켜 피할 수 없는 연속 기절이 났다(낙석·진자 구간 제보).
    /// 문(셔터·승강기)은 예전처럼 막는다 — 닫힌 문에 일부러 박고 통과하는 길을 막으려고(같은 날 결정).
    /// </summary>
    public class FlappyWorldMoverGhostTests
    {
        //  에디터에 열린 씬의 지오메트리와 안 겹치게 떨어진 곳에서 판을 꾸민다 — 실제 물리 질의를 쓴다.
        //  5000처럼 너무 멀면 float 간격(≈0.5 mm)이 아래 0.5 mm 틈 시험보다 커진다.
        private const float X0 = -500f;
        private const float Dt = 0.02f;
        //  맵·장애물은 Default 층(0)에, 새 자신의 콜라이더는 Ignore Raycast 층(2)에 둔다 — 실제 게임처럼
        //  새 콜라이더는 sweep·밀어내기 마스크 밖이다.
        private const int MapMask = 1 << 0;
        private const int OwnLayer = 2;

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

        //  진폭 0인 진자에 매단 상자 — 늘 그 자리에 서 있는 "치는 장애물" 콜라이더다.
        private FlappyPendulumField Mover(FlappyPendulumField field, float x, float y, float w, float h)
        {
            var root = Marked<FlappyPendulum>(x, y, w, h);
            var p = root.GetComponent<FlappyPendulum>();
            p.Amplitude = 0f; p.Period = 2.5f;
            field.Add(p);
            return field;
        }

        //  표식 컴포넌트 T를 단 뿌리 밑에 상자 하나. 셔터·승강기(문)는 필드에 안 넣는다 — 필드가 자세를
        //  대입하면 상자가 움직이므로, 여기선 표식만 보는 판정(막기·밀어내기)을 그 자리에서 본다.
        private GameObject Marked<T>(float x, float y, float w, float h) where T : MonoBehaviour
        {
            var root = new GameObject(typeof(T).Name);
            spawned.Add(root);
            root.transform.position = new Vector3(x, y, 0f);
            root.AddComponent<T>();
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.SetParent(root.transform, false);
            box.transform.localScale = new Vector3(w, h, 1f);
            Physics.SyncTransforms();
            return root;
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

        //  밀어내기를 부를 때마다 아래로 민다. 기절·무적이 아닌 새가 예전 경로(Depenetrate)를 그대로 타는지 센다.
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

        private FlappyWorld World(FlappyPendulumField field, GameFramework.Physics.ICollisionQuery query,
            IMotionBridge bridge, out Entity bird, bool realBody = false, float y = 0f)
        {
            var registry = new EntityRegistry();
            bird = FlappyWorldFixture.Bird("bird-1");
            bird.Get<GameFramework.World.Transform>().Position = new System.Numerics.Vector3(X0, y, 0f);
            if (realBody)
            {
                //  PhysicsBodyFactory와 같은 꼴의 캡슐 — 밀어내기(ComputePenetration)가 실제 콜라이더를 요구한다.
                var go = new GameObject("bird-body") { layer = OwnLayer };
                spawned.Add(go);
                var capsule = go.AddComponent<CapsuleCollider>();
                var shape = bird.Get<CapsuleShape>();
                capsule.radius = shape.Radius;
                capsule.direction = 1;
                capsule.height = shape.Length;
                capsule.center = new Vector3(0f, shape.Height * 0.5f, 0f);
                bird.Add<PhysicsBody>(new UnityPhysicsBody(null, capsule));
            }
            registry.Add(bird);
            var world = new FlappyWorld(registry, new WorldEventBuffer(),
                new FlappyMoveSystem(FlappyWorldFixture.Config()),
                new FlappyStunSystem(FlappyWorldFixture.Config()),
                new FlappyDashSystem(FlappyWorldFixture.Config()),
                new FinishSystem(new FinishLineBounds(FinishAxis.X), FinishAxis.X, increasing: true),
                new FlappyWindmillField(), new FlappyBoostPadField(),
                query, bridge, layerMask: MapMask, pendulumField: field);
            world.GameplayStartTick = 0;
            return world;
        }

        private FlappyWorld RealWorld(FlappyPendulumField field, out Entity bird, float y = 0f)
            => World(field, new GameFramework.Physics.UnityCollisionQuery(),
                     new MotionBridge(MapMask, 0, 1f), out bird, realBody: true, y: y);

        private static System.Numerics.Vector3 Pos(Entity bird) => bird.Get<GameFramework.World.Transform>().Position;

        //  문 판정을 켜 두려고 멀리 둔 진자 하나 — FlappyWorld는 장애물 필드가 비어 있으면 맞음 검사를 건너뛴다.
        private FlappyPendulumField FarPendulum() => Mover(new FlappyPendulumField(), X0 - 300f, 0f, 1f, 1f);

        // ── 밀어내기 ──────────────────────────────────────────────

        //  새 캡슐(반지름 0.45)의 앞면은 X0 + 0.45. 가운데 X0 + 0.6, 폭 0.5인 상자는 앞면을 0.1 m 파고든다.
        private const float EmbedX = X0 + 0.6f;

        [Test]
        public void 기절한_새는_치는_장애물에_밀리지_않는다()
        {
            var world = RealWorld(Mover(new FlappyPendulumField(), EmbedX, 0.45f, 0.5f, 6f), out var bird);
            bird.Get<FlappyStun>().StunRemaining = 0.8f;
            var start = Pos(bird);

            for (long t = 1; t <= 5; t++)
            {
                world.Tick(t, Dt);
            }

            Assert.AreEqual(start, Pos(bird), "기절해 멈춘 새가 진자에 밀렸다");
        }

        [Test]
        public void 기절한_새도_셔터엔_밀려난다()
        {
            Marked<FlappyShutter>(EmbedX, 0.45f, 0.5f, 6f);
            var world = RealWorld(new FlappyPendulumField(), out var bird);
            bird.Get<FlappyStun>().StunRemaining = 0.8f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.AreEqual(X0 - 0.1f, Pos(bird).X, 0.01f, "기절한 새가 닫힌 셔터 속에 남았다");
        }

        [Test]
        public void 기절한_새도_승강기엔_밀려난다()
        {
            Marked<FlappyLift>(EmbedX, 0.45f, 0.5f, 6f);
            var world = RealWorld(new FlappyPendulumField(), out var bird);
            bird.Get<FlappyStun>().StunRemaining = 0.8f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.AreEqual(X0 - 0.1f, Pos(bird).X, 0.01f, "기절한 새가 승강기 속에 남았다");
        }

        [Test]
        public void 무적인_새도_정적인_벽에_파묻혔으면_밀려난다()
        {
            //  0.1 밀려나 벽 면에 닿고, 벽 쪽으로 파고들던 전진 속도는 지워져 그 자리에 선다 → X0 − 0.1.
            //  안 밀어내면 시작부터 겹친 벽을 sweep이 못 봐 0.22를 다 가며 벽 속으로 더 들어간다.
            Wall(EmbedX, 0.45f, 0.5f, 6f);
            var world = RealWorld(new FlappyPendulumField(), out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.AreEqual(X0 - 0.1f, Pos(bird).X, 0.01f);
        }

        [Test]
        public void 기절도_무적도_아니면_전처럼_밀어낸다()
        {
            var bridge = new DragBridge();
            var world = World(new FlappyPendulumField(), new FlappyWorldFixture.NeverHit(), bridge, out var bird);

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.AreEqual(1, bridge.DepenetrateCalls);
        }

        // ── 무적은 치는 장애물 속에서 안 끝난다 ─────────────────────

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

        [Test]
        public void 셔터는_무적을_붙들지_않는다()
        {
            //  문 속에서 무적이 끝나면 예전처럼 기절한다 — 문은 통과 대상이 아니다.
            Marked<FlappyShutter>(X0, 0f, 400f, 400f);
            var world = World(FarPendulum(), new FlappyWorldFixture.NeverHit(), new FlappyWorldFixture.NoopMotionBridge(), out var bird);
            var stun = bird.Get<FlappyStun>();
            stun.InvulnRemaining = Dt;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.Greater(stun.StunRemaining, 0f);
        }

        // ── 이동 sweep ───────────────────────────────────────────

        //  새 캡슐(반지름 0.45)의 앞면은 X0 + 0.45. 얇은 판(폭 0.05)을 X0 + 0.55에 두면 앞면까지 0.075 —
        //  한 틱 전진 0.22 안이다.
        private const float PlateX = X0 + 0.55f;

        [Test]
        public void 무적이_아니면_장애물에_막히고_기절한다()
        {
            var world = RealWorld(Mover(new FlappyPendulumField(), PlateX, 0.45f, 0.05f, 6f), out var bird);

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.Less(Pos(bird).X, X0 + 0.1f, "판이 앞을 막지 않았다 — 판 배치가 틀렸다");
            Assert.Greater(bird.Get<FlappyStun>().StunRemaining, 0f);
        }

        [Test]
        public void 무적인_새는_치는_장애물을_뚫고_지나간다()
        {
            var world = RealWorld(Mover(new FlappyPendulumField(), PlateX, 0.45f, 0.05f, 6f), out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.Greater(Pos(bird).X, X0 + 0.2f, "무적인 새가 장애물에 막혔다");
            Assert.AreEqual(0f, bird.Get<FlappyStun>().StunRemaining);
        }

        [Test]
        public void 무적이어도_셔터엔_막힌다()
        {
            Marked<FlappyShutter>(PlateX, 0.45f, 0.05f, 6f);
            var world = RealWorld(new FlappyPendulumField(), out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.Less(Pos(bird).X, X0 + 0.1f, "무적인 새가 닫힌 셔터를 뚫었다");
        }

        [Test]
        public void 무적이어도_승강기엔_막힌다()
        {
            Marked<FlappyLift>(PlateX, 0.45f, 0.05f, 6f);
            var world = RealWorld(new FlappyPendulumField(), out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.Less(Pos(bird).X, X0 + 0.1f, "무적인 새가 승강기를 뚫었다");
        }

        [Test]
        public void 무적이어도_정적인_벽엔_막힌다()
        {
            Wall(PlateX, 0.45f, 0.05f, 6f);
            var world = RealWorld(new FlappyPendulumField(), out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.Less(Pos(bird).X, X0 + 0.1f, "무적인 새가 맵 벽을 뚫었다");
        }

        [Test]
        public void 무적이면_장애물_바로_뒤의_벽까지_가서_막힌다()
        {
            //  판(앞면 X0+0.525)을 지나 그 바로 뒤 벽(앞면 X0+0.6)에서 멈춘다 — 판 히트만 버리고 끝내면
            //  벽까지 못 보고, 판 앞에서 멈추면 장애물을 못 뚫은 것이다.
            Wall(X0 + 0.65f, 0.45f, 0.1f, 6f);
            var world = RealWorld(Mover(new FlappyPendulumField(), PlateX, 0.45f, 0.05f, 6f), out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            //  벽 앞면 0.6 − 반지름 0.45 − SkinWidth 0.02 = 0.13
            Assert.AreEqual(X0 + 0.13f, Pos(bird).X, 0.005f);
        }

        [Test]
        public void 무적이면_장애물_둘을_지나_벽까지_간_거리를_맞게_잰다()
        {
            //  판 둘(앞면 0.475, 0.525)을 지나 벽(앞면 0.6). 다시 쏠 때마다 지나온 거리를 더해 돌려줘야
            //  부르는 쪽이 처음 자리 기준으로 0.13에 세운다 — 덜 더하면 벽 속으로, 더 더하면 벽 앞에 선다.
            Wall(X0 + 0.65f, 0.45f, 0.1f, 6f);
            var field = Mover(new FlappyPendulumField(), X0 + 0.5f, 0.45f, 0.05f, 6f);
            Mover(field, PlateX, 0.45f, 0.05f, 6f);
            var world = RealWorld(field, out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.AreEqual(X0 + 0.13f, Pos(bird).X, 0.005f);
        }

        [Test]
        public void 치는_장애물이_한_번에_너무_많으면_막는다()
        {
            //  얇은 판(폭 0.005) 10장을 0.015 간격으로(앞면 0.46, 0.475, …). 다시 쏘기는 8번까지라 9번째
            //  판(앞면 0.58)에서 막힌다 → 0.58 − 0.45 − 0.02 = 0.11. 상한이 없으면 다 지나 0.22까지 간다.
            var field = new FlappyPendulumField();
            for (int k = 0; k < 10; k++)
            {
                Mover(field, X0 + 0.4625f + k * 0.015f, 0.45f, 0.005f, 6f);
            }
            var world = RealWorld(field, out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.AreEqual(X0 + 0.11f, Pos(bird).X, 0.005f);
        }

        [Test]
        public void 장애물이_벽보다_반_밀리_앞서_붙어_있어도_벽을_뚫지_않는다()
        {
            //  판 앞면 X0+0.5245, 벽 앞면 X0+0.525 — 0.5 mm 틈. 판에서 1 mm 들어가 다시 쏘면 벽과 시작부터
            //  겹쳐 sweep이 벽을 못 본다. 그때는 판 히트로 막아야 한다(판 앞 0.0745 − 0.02 ≈ 0.0545).
            Wall(X0 + 0.625f, 0.45f, 0.2f, 6f);
            var world = RealWorld(Mover(new FlappyPendulumField(), X0 + 0.5495f, 0.45f, 0.05f, 6f), out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.Less(Pos(bird).X, X0 + 0.06f, "장애물을 지나며 바로 뒤 벽 속으로 들어갔다");
        }

        [Test]
        public void 장애물과_벽의_면이_같아도_벽을_뚫지_않는다()
        {
            //  판과 벽의 앞면이 똑같이 X0+0.525. PhysX가 어느 쪽을 먼저 돌려주든 벽 앞에서 서야 한다.
            Wall(X0 + 0.625f, 0.45f, 0.2f, 6f);
            var world = RealWorld(Mover(new FlappyPendulumField(), PlateX, 0.45f, 0.05f, 6f), out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.Less(Pos(bird).X, X0 + 0.06f);
        }

        [Test]
        public void 천장에_붙은_새가_막_떨어지기_시작한_바위와_천장_면이_같아도_천장을_뚫지_않는다()
        {
            //  낙석 첫 틱(fallen 0)엔 바위 아랫면이 천장선과 같은 면이다. 무적인 새가 천장 바로 밑에서
            //  날아오르면 PhysX가 바위를 돌려줄 수 있고, 거기서 다시 쏘면 천장과 시작부터 겹친다.
            const float ceiling = 10f;
            Wall(X0, ceiling + 2.5f, 6f, 5f);
            var field = Mover(new FlappyPendulumField(), X0, ceiling + 0.6f, 1.2f, 1.2f);
            var world = RealWorld(field, out var bird, y: ceiling - 0.95f);   // 머리가 천장 0.05 아래
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;
            bird.Get<Velocity>().Linear = new System.Numerics.Vector3(0f, 10f, 0f);   // 한 틱 ≈ 0.17 m 위로

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.LessOrEqual(Pos(bird).Y + 0.9f, ceiling + 1e-3f, "새 머리가 천장 속으로 들어갔다");
        }

        // ── 대시 ────────────────────────────────────────────────

        [Test]
        public void 무적이면_진자_속을_지나도_대시가_이어진다()
        {
            var world = RealWorld(Mover(new FlappyPendulumField(), X0, 0f, 400f, 400f), out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;
            bird.Get<FlappyDash>().DashRemaining = 0.2f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.Greater(bird.Get<FlappyDash>().DashRemaining, 0f, "무적인 새의 대시가 진자 속에서 끊겼다");
        }

        [Test]
        public void 무적이면_진자를_뚫고_지나가도_대시가_이어진다()
        {
            var world = RealWorld(Mover(new FlappyPendulumField(), PlateX, 0.45f, 0.05f, 6f), out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;
            bird.Get<FlappyDash>().DashRemaining = 0.2f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.Greater(Pos(bird).X, X0 + 0.2f);
            Assert.Greater(bird.Get<FlappyDash>().DashRemaining, 0f, "무적인 새의 대시가 진자를 지나며 끊겼다");
        }

        [Test]
        public void 무적이_아니면_진자에_맞아_대시가_끊긴다()
        {
            var world = RealWorld(Mover(new FlappyPendulumField(), X0, 0f, 400f, 400f), out var bird);
            bird.Get<FlappyDash>().DashRemaining = 0.2f;

            world.Tick(FlappyWorldFixture.StartTick, Dt);

            Assert.AreEqual(0f, bird.Get<FlappyDash>().DashRemaining);
        }
    }
}
