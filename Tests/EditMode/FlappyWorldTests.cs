using System.Collections.Generic;
using GameFramework;
using GameFramework.Physics;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class FlappyWorldTests
    {
        const float Tolerance = 1e-3f;

        // 아무데도 안 부딪히는 빈 하늘 — 맵 충돌 없이 이동 계산만 보고 싶을 때.
        private class EmptySkyQuery : ICollisionQuery
        {
            public CollisionHit CapsuleCast(Vector3 point1, Vector3 point2, float radius,
                Vector3 direction, float distance, int layerMask) => CollisionHit.None;

            public CollisionHit Raycast(Vector3 origin, Vector3 direction, float distance, int layerMask)
                => CollisionHit.None;

            public CollisionHit[] OverlapSphere(Vector3 center, float radius, int layerMask)
                => System.Array.Empty<CollisionHit>();
        }

        // 물리 바디가 아직 없는 단계라 브릿지는 아무 일도 하지 않는다(호출 여부·순서만 세어 둔다).
        // CallOrder는 지운 채로 둬도 두 스위트가 계속 초록이던 사각지대(밀어내기 순서)를 메우려고
        // 있다 — "누가 몇 번 불렸나"만이 아니라 "무엇보다 먼저 불렸나"까지 pin한다.
        private class NoopMotionBridge : GameFramework.World.IMotionBridge
        {
            public int SyncTransformsCalls;
            public int SeparateCalls;
            public int DepenetrateCalls;
            public readonly List<string> CallOrder = new List<string>();

            public void SyncTransforms() { SyncTransformsCalls++; CallOrder.Add("SyncTransforms"); }
            public System.Numerics.Vector3 Depenetrate(Entity entity)
            {
                DepenetrateCalls++;
                CallOrder.Add("Depenetrate");
                return PushToReturn;
            }

            /// <summary>밀어냈다고 답할 벡터. 기본값 0 = 안 밀었다.</summary>
            public System.Numerics.Vector3 PushToReturn;
            public void Separate(Entity entity) { SeparateCalls++; CallOrder.Add("Separate"); }
            public void PushMotion(Entity entity) => CallOrder.Add("PushMotion");
        }

        // 어느 방향으로 sweep하든 고정 거리로 맞는 벽. MoveBlockedByMap은 KinematicMover를 통해
        // 수평·수직을 나눠 따로 sweep하므로(각각 최대 한 번 이상) 호출 횟수는 방향 개수만큼 나온다.
        // 받은 인자를 기록해 phase ⑤가 실제로 엔티티가 들고 있는 몸 치수(반지름)·월드가 받은
        // 레이어마스크를 쓰는지 검증한다.
        private class WallAheadQuery : ICollisionQuery
        {
            private readonly float _hitDistance;
            private readonly Vector3 _normal;

            public float LastRadius;
            public int LastLayerMask;
            public int CastCount;

            public WallAheadQuery(float hitDistance, Vector3 normal)
            {
                _hitDistance = hitDistance;
                _normal = normal;
            }

            public CollisionHit CapsuleCast(Vector3 point1, Vector3 point2, float radius,
                Vector3 direction, float distance, int layerMask)
            {
                LastRadius = radius;
                LastLayerMask = layerMask;
                CastCount++;   // 수평·수직 sweep을 둘 다 센다
                return new CollisionHit(true, _hitDistance, _normal, point1 + direction * _hitDistance, null);
            }

            public CollisionHit Raycast(Vector3 origin, Vector3 direction, float distance, int layerMask)
                => CollisionHit.None;

            public CollisionHit[] OverlapSphere(Vector3 center, float radius, int layerMask)
                => System.Array.Empty<CollisionHit>();
        }

        static FlappyConfig Config()
            => new FlappyConfig(forwardSpeed: 11f, flapImpulse: 23f, gravity: 70f, maxFallSpeed: 30f,
                                bodyRadius: 0.45f, bodyHeight: 0.9f, restitution: 0.35f,
                                stunTime: 0.8f, invulnTime: 0.6f);

        static Entity Bird(string id, Vector3 position, bool simulated, float radius = 0.45f, float height = 0.9f)
        {
            var entity = new Entity(id);
            entity.Add(new GameFramework.World.Transform { Position = position.ToNumerics() });
            entity.Add(new Velocity());
            entity.Add(new CapsuleShape(radius, height));
            // 실제 크리에이터가 항상 붙이는 것과 같다 — EntityKind는 CollectBirds가 "새"를 가리는
            // 기준(정체성), FlappyStun은 스턴 진입 검증에 필요(상태).
            entity.Add(new EntityKind(EntityType.Character));
            entity.Add(new FlappyStun());
            if (simulated)
            {
                entity.Add(new Simulated());
            }
            return entity;
        }

        static FlappyWorld World(EntityRegistry registry, GameFramework.World.IMotionBridge bridge)
        {
            // 이 파일의 테스트는 출발 게이트가 아니라 이동/충돌을 다룬다 — 이미 출발한 것으로 둔다.
            var world = new FlappyWorld(registry, new WorldEventBuffer(),
                               new FlappyMoveSystem(Config()),
                               new FlappyBodyCollisionSystem(Config()),
                               new FlappyStunSystem(Config()),
                               new EmptySkyQuery(), bridge, layerMask: ~0);
            world.GameplayStartTick = 0;
            return world;
        }

        static Vector3 PositionOf(Entity e) => e.Get<GameFramework.World.Transform>().Position.ToUnity();
        static Vector3 VelocityOf(Entity e) => e.Get<Velocity>().Linear.ToUnity();

        [Test]
        public void 벽에서_밀려났으면_그_방향으로_파고들던_속도를_지운다()
        {
            var registry = new EntityRegistry();
            var bird = Bird("bird-1", Vector3.zero, simulated: true);
            registry.Add(bird);

            //  바닥에 파묻힌 상황: 밀어내기가 위로 밀어낸다.
            var bridge = new NoopMotionBridge { PushToReturn = new System.Numerics.Vector3(0f, 0.15f, 0f) };

            World(registry, bridge).Tick(1, 0.1f);

            //  안 지우면 중력이 그대로 남아(-7) 다음 틱에 또 파고들고, 밀어내기와 줄다리기가 된다.
            //  실제로 그렇게 낙하속도가 -14까지 쌓이는 동안 새는 0.11밖에 안 내려갔다.
            Assert.AreEqual(0f, VelocityOf(bird).y, Tolerance);
        }

        [Test]
        public void 밀려난_방향과_무관한_속도는_남긴다()
        {
            var registry = new EntityRegistry();
            var bird = Bird("bird-1", Vector3.zero, simulated: true);
            registry.Add(bird);

            var bridge = new NoopMotionBridge { PushToReturn = new System.Numerics.Vector3(0f, 0.15f, 0f) };

            World(registry, bridge).Tick(1, 0.1f);

            //  위로 밀렸다고 전진까지 멈추면 벽에 붙은 새가 미끄러져 빠져나올 길이 없어진다.
            Assert.AreEqual(11f, VelocityOf(bird).x, Tolerance);
        }

        [Test]
        public void 한_틱이면_전진하면서_중력만큼_떨어진다()
        {
            var registry = new EntityRegistry();
            var bird = Bird("bird-1", Vector3.zero, simulated: true);
            registry.Add(bird);

            World(registry, new NoopMotionBridge()).Tick(1, 0.1f);

            Assert.AreEqual(11f, VelocityOf(bird).x, Tolerance);    // 고정 전진
            Assert.AreEqual(-7f, VelocityOf(bird).y, Tolerance);    // 70 × 0.1
            Assert.AreEqual(1.1f, PositionOf(bird).x, Tolerance);   // 11 × 0.1
            Assert.AreEqual(-0.7f, PositionOf(bird).y, Tolerance);  // 7 × 0.1
        }

        [Test]
        public void 시뮬_대상이_아닌_엔티티는_건드리지_않는다()
        {
            var registry = new EntityRegistry();
            var remote = Bird("bird-2", Vector3.zero, simulated: false);
            registry.Add(remote);

            World(registry, new NoopMotionBridge()).Tick(1, 0.1f);

            // 남의 새는 예측하지 않고 서버 스냅샷 보간에 맡긴다
            Assert.AreEqual(Vector3.zero, PositionOf(remote));
            Assert.AreEqual(Vector3.zero, VelocityOf(remote));
        }

        [Test]
        public void 겹쳐_있던_두_새는_이동_전에_갈라진다()
        {
            var registry = new EntityRegistry();
            var lower = Bird("bird-1", Vector3.zero, simulated: true);
            var upper = Bird("bird-2", new Vector3(0f, 0.5f, 0f), simulated: true);
            registry.Add(lower);
            registry.Add(upper);

            World(registry, new NoopMotionBridge()).Tick(1, 0.1f);

            // 몸싸움으로 위아래 0.39만큼 갈라진 뒤 각자 이동한다 — 서로 파고든 채로 남지 않는다
            Assert.Greater(PositionOf(upper).y - PositionOf(lower).y, 0.5f);
        }

        [Test]
        public void 새끼리_밀어내기를_물리엔진에_맡기지_않는다()
        {
            var registry = new EntityRegistry();
            registry.Add(Bird("bird-1", Vector3.zero, simulated: true));
            var bridge = new NoopMotionBridge();

            World(registry, bridge).Tick(1, 0.1f);

            Assert.AreEqual(0, bridge.SeparateCalls);       // 겹침은 우리 계산이 이미 풀었다
            // 엔진에 자리를 알려 줄 일도 없다 — 밀어내기가 World.Transform을 직접 읽으므로
            // 물리 트랜스폼을 미리 맞춰 둘 이유가 사라졌다.
            Assert.AreEqual(0, bridge.SyncTransformsCalls);
        }

        [Test]
        public void 맵에서_밀어내기가_새마다_매_틱_불린다()
        {
            // 이 테스트를 지우고 Mutation의 Depenetrate 루프를 통째로 지워도 두 스위트가
            // 계속 초록이었다 — NoopMotionBridge.Depenetrate가 아무 일도 안 하는 빈 메서드라
            // 호출 자체를 아무도 세지 않았기 때문이다. 새 2마리 × 틱 2번 = 4번으로 그 사각지대를 막는다.
            var registry = new EntityRegistry();
            registry.Add(Bird("bird-1", Vector3.zero, simulated: true));
            registry.Add(Bird("bird-2", new Vector3(5f, 0f, 0f), simulated: true));
            var bridge = new NoopMotionBridge();

            var world = World(registry, bridge);
            world.Tick(1, 0.1f);
            world.Tick(2, 0.1f);

            Assert.AreEqual(4, bridge.DepenetrateCalls);
        }

        [Test]
        public void 맵에서_밀어내기가_이동보다_먼저_불린다()
        {
            // 밀어내기는 복구, sweep은 예방이다. 밀어내기가 이동 뒤에 오면 sweep이 아직 벽 안에
            // 있는 자리에서 출발해 거리 0을 받고 그대로 낀다 — 밀어낸 결과를 쓰지 못한다.
            // 그래서 "Depenetrate가 Move보다 앞"이라는 순서 자체를 pin한다.
            var registry = new EntityRegistry();
            registry.Add(Bird("bird-1", Vector3.zero, simulated: true));
            var bridge = new NoopMotionBridge();

            World(registry, bridge).Tick(1, 0.1f);

            // Depenetrate(밀어내기) → PushMotion(MoveBlockedByMap이 마지막에 최종 자리를 반영).
            CollectionAssert.AreEqual(new[] { "Depenetrate", "PushMotion" }, bridge.CallOrder);
        }

        [Test]
        public void 맵에_부딪히면_막히고_스턴에_걸리며_반지름과_레이어마스크가_넘어간다()
        {
            var registry = new EntityRegistry();
            var bird = Bird("bird-1", Vector3.zero, simulated: true);
            registry.Add(bird);

            var wallQuery = new WallAheadQuery(hitDistance: 0.5f, normal: Vector3.left);
            const int layerMask = 1 << 5;   // ~0처럼 아무 값이나 통과하는 마스크가 아니라 실제로 넘겨지는지 구분할 값
            var world = new FlappyWorld(registry, new WorldEventBuffer(),
                                         new FlappyMoveSystem(Config()),
                                         new FlappyBodyCollisionSystem(Config()),
                                         new FlappyStunSystem(Config()),
                                         wallQuery, new NoopMotionBridge(), layerMask);
            world.GameplayStartTick = 0;   // 이 테스트는 출발 게이트가 아니라 맵 충돌을 다룬다

            world.Tick(1, 0.1f);

            // 막힌다 — 델타(x=1.1)까지 못 가고 벽 앞(거리 0.5 − SkinWidth 0.02)에서 멈춘다.
            Assert.AreEqual(0.48f, PositionOf(bird).x, Tolerance);
            // 대신 스턴에 걸린다 — 페널티는 위치 차단과 별개로 여전히 멈춰 있는 시간이다.
            Assert.That(bird.Get<FlappyStun>().StunRemaining, Is.GreaterThan(0f));

            // 그리고 그 판정은 실제로 일어났고(수평·수직 각 sweep에서 한 번씩, 총 두 번),
            // 엔티티 자신의 몸 치수·월드가 받은 레이어마스크로 이뤄졌다.
            Assert.AreEqual(2, wallQuery.CastCount);
            Assert.AreEqual(Config().BodyRadius, wallQuery.LastRadius, Tolerance);
            Assert.AreEqual(layerMask, wallQuery.LastLayerMask);
        }

        [Test]
        public void 맵_sweep은_엔티티가_들고_있는_몸_치수를_쓴다()
        {
            var registry = new EntityRegistry();
            // 튜닝값(0.45)과 일부러 다른 몸을 준다 — 어느 쪽을 읽는지 구분되는 값이어야 한다.
            var bird = Bird("bird-1", Vector3.zero, simulated: true, radius: 0.2f, height: 0.4f);
            registry.Add(bird);

            var wallQuery = new WallAheadQuery(hitDistance: 0.5f, normal: Vector3.left);
            var world = new FlappyWorld(registry, new WorldEventBuffer(),
                                        new FlappyMoveSystem(Config()),
                                        new FlappyBodyCollisionSystem(Config()),
                                        new FlappyStunSystem(Config()),
                                        wallQuery, new NoopMotionBridge(), layerMask: ~0);
            world.GameplayStartTick = 0;   // 이 테스트는 출발 게이트가 아니라 맵 충돌을 다룬다

            world.Tick(1, 0.1f);

            Assert.AreEqual(0.2f, wallQuery.LastRadius, Tolerance);
        }

        [Test]
        public void 입력이_안_들어온_새는_안_누른_것으로_굴러간다()
        {
            //  이 Bird()가 InputBuffer를 안 붙인 채로 두는 건 임의 선택이 아니라, 클라의
            //  CharacterCreator/FlappyBirdCreator가 실제로 하는 일을 그대로 옮긴 것이다 —
            //  둘 다 worldEntity.Add(new InputBuffer())를 isUserEntity일 때만 부르므로, 남의
            //  새는 그 컴포넌트가 아예 없다. FlappyMoveSystem은 Get<InputBuffer>()가 null이면
            //  null-조건부로 그냥 넘어가므로, 컴포넌트가 없는 새는 몇 틱을 굴리든 한 번도
            //  날갯짓하지 않고 중력만 먹어야 한다 — 이게 실제 프로덕션에서 남의 새가 굴러가는
            //  방식이다.
            var registry = new EntityRegistry();
            var bird = Bird("bird-1", Vector3.zero, simulated: true);
            registry.Add(bird);
            var world = World(registry, new NoopMotionBridge());

            for (long t = 1; t <= 5; t++)
            {
                world.Tick(t, 0.02f);
            }

            //  순수 중력 누적(−70 × 0.02 × 5)과 정확히 같아야 한다 — 날갯짓이 단 한 번이라도
            //  끼어들었다면 이 값이 FlapImpulse(23)로 덮여 크게 벌어진다.
            Assert.AreEqual(-7f, VelocityOf(bird).y, Tolerance);
        }

        [Test]
        public void 몸이_없는_엔티티는_맵_이동을_하지_않는다()
        {
            var registry = new EntityRegistry();
            var noBody = new Entity("bird-1");
            noBody.Add(new GameFramework.World.Transform());
            noBody.Add(new Velocity());
            // CapsuleShape는 일부러 안 붙인다 — 이게 이 테스트의 요점이다. 대신 CollectBirds가
            // "새"로 집어 가려면 EntityKind(+FlappyStun, 스턴 시스템이 요구)는 있어야 한다.
            noBody.Add(new EntityKind(EntityType.Character));
            noBody.Add(new FlappyStun());
            noBody.Add(new Simulated());
            registry.Add(noBody);

            // 속도는 정해지지만(전진·중력) 위치를 옮기는 단계는 몸 없이는 돌 수 없다.
            World(registry, new NoopMotionBridge()).Tick(1, 0.1f);

            Assert.AreEqual(Vector3.zero, PositionOf(noBody));
            // 몸이 없어 "위치 이동"만 못 한다는 걸 못박는다 — 속도 계산(전진·중력)은 여전히 돌았다.
            Assert.That(VelocityOf(noBody).x, Is.GreaterThan(0f));
        }

        [Test]
        public void 조용한_구간은_9틱_전_상태에서_다시_굴려도_정확히_일치하고_날갯짓_구간은_그렇지_않다()
        {
            //  이 테스트가 이 슬라이스 전체의 값어치다(스펙 §8.2). 외삽(마지막 속도로 이어 그리기)은
            //  날갯짓이 하나도 없는 조용한 구간에서도 진실과 항상 어긋났다(실측 0.126m, 빠르게
            //  떨어질 땐 1.13m까지). 시뮬은 그 구간에서 "9틱 전의 진짜 상태에서 다시 굴리면 지금과
            //  정확히 같다"는, 외삽으로는 절대 못 만드는 성질을 가져야 한다.
            //
            //  대신 시뮬도 만능은 아니다 — 그 9틱 사이에 날갯짓이 실제로 있었다면, 입력이 안 온
            //  재현은 그 날갯짓을 모르니 갈라진다. 아래 두 번째 assert가 그 대조다: "조용하면
            //  정확히 맞고, 날갯짓이 끼면 안 맞는다"는 비대칭 자체가 이 설계가 사는 이유다.
            var registry = new EntityRegistry();
            var truth = Bird("truth", Vector3.zero, simulated: true);
            truth.Add(new InputBuffer());
            registry.Add(truth);
            var world = World(registry, new NoopMotionBridge());

            //  틱 5에서 딱 한 번만 날갯짓한다. 그 외 틱은 매번 새로 "안 눌렀다"를 세운다 —
            //  PlayerInputManager가 매 프레임 pendingJump를 소비한 뒤 리셋하는 것과 같은 모양이다
            //  (Current를 stale하게 남겨두는 시나리오가 아니다).
            const int FlapTick = 5;
            const int TotalTicks = 20;
            const int Window = 9;
            var positions = new Vector3[TotalTicks + 1];
            var velocities = new Vector3[TotalTicks + 1];
            positions[0] = Vector3.zero;
            velocities[0] = Vector3.zero;

            for (int t = 1; t <= TotalTicks; t++)
            {
                truth.Get<InputBuffer>().Current = new InputCommand { Jump = t == FlapTick };
                world.Tick(t, 0.02f);
                positions[t] = PositionOf(truth);
                velocities[t] = VelocityOf(truth);
            }

            // --- 조용한 구간: (T-9, T]에 날갯짓이 없다. T=20이면 창은 틱 12~20 — 날갯짓(틱 5)은 그 전에
            // 이미 끝나 창 밖이다. 9틱 전(틱 11) 상태만 들고 다시 굴리면 틱 20의 진실과 맞아야 한다.
            const int QuietTargetTick = 20;
            int quietSourceTick = QuietTargetTick - Window;   // 11

            var quietRegistry = new EntityRegistry();
            var quietBird = Bird("quiet-repro", positions[quietSourceTick], simulated: true);
            quietBird.Get<Velocity>().Linear = velocities[quietSourceTick].ToNumerics();
            //  InputBuffer를 아예 안 붙인다 — 남의 새가 실제로 클라에서 굴러가는 모양(입력 자체가 없음)이다.
            quietRegistry.Add(quietBird);
            var quietWorld = World(quietRegistry, new NoopMotionBridge());
            for (int t = 1; t <= Window; t++)
            {
                quietWorld.Tick(t, 0.02f);
            }

            //  느슨한 허용오차가 아니라 float 잡음 수준(1e-4)으로 본다 — 헐거우면 회귀도 통과시키고,
            //  옛 외삽 공식과도 구분이 안 된다(외삽은 조용한 구간에서도 0.126m씩 어긋났다).
            const float ExactTolerance = 1e-4f;
            Assert.AreEqual(positions[QuietTargetTick].x, PositionOf(quietBird).x, ExactTolerance);
            Assert.AreEqual(positions[QuietTargetTick].y, PositionOf(quietBird).y, ExactTolerance);
            Assert.AreEqual(velocities[QuietTargetTick].y, VelocityOf(quietBird).y, ExactTolerance);

            // --- 대조: (T'-9, T']에 날갯짓이 낀 구간. T'=10이면 창은 틱 2~10 — 날갯짓(틱 5)이 그 안에
            // 있다. 9틱 전(틱 1) 상태에서 입력 없이 다시 굴리면, 진짜로는 있었던 날갯짓을 재현이 모르니
            // 틱 10의 진실과 갈라져야 한다 — 안 갈라지면 위 "정확히 일치"가 우연이라는 뜻이다.
            const int FlapTargetTick = 10;
            int flapSourceTick = FlapTargetTick - Window;   // 1

            var flapRegistry = new EntityRegistry();
            var flapBird = Bird("flap-repro", positions[flapSourceTick], simulated: true);
            flapBird.Get<Velocity>().Linear = velocities[flapSourceTick].ToNumerics();
            flapRegistry.Add(flapBird);
            var flapWorld = World(flapRegistry, new NoopMotionBridge());
            for (int t = 1; t <= Window; t++)
            {
                flapWorld.Tick(t, 0.02f);
            }

            //  1cm는 float 잡음(1e-4)보다 훨씬 크면서, 날갯짓 하나가 만드는 실제 벌어짐보다는
            //  훨씬 작다 — "그냥 안 같다" 정도가 아니라 "확실히, 의미 있게 안 같다"를 본다.
            const float DivergenceFloor = 0.01f;
            float divergence = Mathf.Abs(positions[FlapTargetTick].y - PositionOf(flapBird).y);
            Assert.That(divergence, Is.GreaterThan(DivergenceFloor));
        }
    }
}
