using System.Collections.Generic;
using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class SkydiveWorldTests
    {
        const float Tolerance = 1e-3f;

        static SkydiveConfig Config()
            => new SkydiveConfig(
                spreadFallSpeed: 60f, diveFallSpeed: 90f, glideFallSpeed: 6f,
                spreadMoveSpeed: 12f, diveMoveSpeed: 9f, glideMoveSpeed: 14f,
                spreadTurnAccel: 22f, diveTurnAccel: 6f, glideTurnAccel: 18f,
                fallApproach: 29f, postureRate: 4f,
                bodyRadius: 0.4f, bodyHeight: 1.8f, groundY: 0f,
                staminaMax: 100f, glideDrain: 20f, groundRecover: 40f, emergencyGlideTime: 1f,
                groundMoveSpeed: 4f, groundAccel: 100f, jumpPower: 11f, poseClearance: 5f, fallBrake: 150f,
                glideWindLag: 0.2f, spreadWindLag: 2.06f, diveWindLag: 3.1f,
                landingLethalSpeed: 15f,
                restitution: 0.35f);

        // 기본 맵은 면이 하나도 없는 하늘이다(HalfSpaceQuery에 면을 안 넣으면 늘 CollisionHit.None).
        static SkydiveWorld World(EntityRegistry registry,
                                  GameFramework.Physics.ICollisionQuery query = null,
                                  WindField wind = null,
                                  DoorField doors = null,
                                  FinishLineBounds finish = null,
                                  BodyCollisionSystem bodyCollisionSystem = null)
            => new SkydiveWorld(registry, new WorldEventBuffer(),
                                new SkydiveMoveSystem(), new StaminaSystem(),
                                new WindDriftSystem(),
                                //  결승선을 안 주면 아무도 통과하지 않는다 — 대부분의 테스트가 그걸 원한다.
                                new FinishSystem(finish ?? new FinishLineBounds(FinishAxis.Y),
                                                 FinishAxis.Y, increasing: false),
                                wind ?? new WindField(), doors ?? new DoorField(),
                                bodyCollisionSystem ?? new BodyCollisionSystem(
                                    Config().BodyRadius, Config().BodyHeight, Config().Restitution, Vector3.one),
                                Config(),
                                query ?? new HalfSpaceQuery(),
                                new FlappyWorldFixture.NoopMotionBridge(), layerMask: ~0);

        //  결승선은 "서서 접지한 다이버의 몸이 실제로 닿는 높이"에 둬야 한다 — 그래야 아래 세
        //  테스트가 서로 다른 결론(접지 전 통과 안 됨 / 치명 착지는 통과 안 됨 / 안전 착지는
        //  통과됨)을 실제로 가른다. 선을 몸이 안 닿는 높이에 두면 뒤의 두 "완주 아님" 단언은
        //  그냥 몸이 못 닿아서 통과하는 가짜 초록이 되어 아무것도 못 잰다 — 그 방지턱이
        //  안전_속도로_접지하면_완주다(양성 대조군)다. 여기 쓰는 좌표는 이 파일 안에서만
        //  의미가 있고, 실제 맵 씬의 결승선 마커 좌표를 따르지 않는다(따로 맞출 필요 없음).
        static FinishLineBounds GroundFinishLine()
        {
            var line = new FinishLineBounds(FinishAxis.Y);
            line.Register(new Bounds(new Vector3(0f, 1f, 0f), new Vector3(200f, 1f, 200f)));
            return line;
        }

        static bool Finished(EntityRegistry r, string id)
            => r.Get(id).Get<FinishState>()?.Finished ?? false;

        static Entity Diver(string id, bool simulated = true, EntityType kind = EntityType.Character)
        {
            var e = new Entity(id);
            e.Add(new GameFramework.World.Transform { Position = new Vector3(0f, 1000f, 0f).ToNumerics() });
            e.Add(new Velocity());
            e.Add(new EntityKind(kind));
            e.Add(new Posture());
            e.Add(new Stamina { Current = 100f });
            e.Add(new InputBuffer());
            e.Add(new GroundState());   // 이동 커널이 매 틱 접지 여부를 여기 적는다
            e.Add(new LandingImpact());   // 이동이 매 틱 착지 충격을 여기 적는다
            e.Add(new MotionState());
            e.Add(new WindDrift());
            if (simulated) { e.Add(new Simulated()); }
            return e;
        }

        //  FinishState/CapsuleShape을 기본 Diver()에는 안 넣는다 — 넣으면 결승선을 등록하지 않는
        //  다른 모든 테스트에서도 FinishSystem.Tick이 "결승선을 모른다" 에러 로그를 쏘게 되어
        //  (지금은 state==null에서 조용히 빠져나간다) 관계없는 테스트들이 LogAssert로 깨진다.
        //  결승선을 실제로 재는 이 세 테스트에만 두 컴포넌트를 얹는다.
        static Entity FinishingDiver(string id)
        {
            var e = Diver(id);
            e.Add(new GameFramework.World.CapsuleShape(Config().BodyRadius, Config().BodyHeight));
            e.Add(new FinishState());
            return e;
        }

        static float HeightOf(EntityRegistry r, string id)
            => r.Get(id).Get<GameFramework.World.Transform>().Position.Y;

        static float ImpactOf(EntityRegistry r, string id)
            => r.Get(id).Get<LandingImpact>().DownwardSpeed;

        [Test]
        public void 출발_전에는_아무도_움직이지_않는다()
        {
            var registry = new EntityRegistry();
            registry.Add(Diver("a"));
            var world = World(registry);
            world.GameplayStartTick = 100;

            world.Tick(10, 0.02f);

            Assert.AreEqual(1000f, HeightOf(registry, "a"), Tolerance);
        }

        [Test]
        public void 바닥에_닿으면_멈추고_접지로_기록된다()
        {
            var registry = new EntityRegistry();
            var diver = Diver("a");
            diver.Get<GameFramework.World.Transform>().Position = new Vector3(0f, 0.3f, 0f).ToNumerics();
            registry.Add(diver);

            var map = new HalfSpaceQuery();
            map.AddGround(0f);
            var world = World(registry, map);
            world.GameplayStartTick = 0;

            for (int t = 0; t < 20; t++) { world.Tick(t, 0.02f); }

            Assert.GreaterOrEqual(HeightOf(registry, "a"), -0.01f, "바닥을 뚫고 내려가면 안 된다");
            Assert.IsTrue(diver.Get<GroundState>().IsGrounded, "바닥에 서 있으면 접지여야 한다");
        }

        [Test]
        public void 발판_위에_서면_스태미나가_찬다()
        {
            var registry = new EntityRegistry();
            var diver = Diver("a");
            diver.Get<GameFramework.World.Transform>().Position = new Vector3(0f, 0.3f, 0f).ToNumerics();
            diver.Get<Stamina>().Current = 0f;
            registry.Add(diver);

            var map = new HalfSpaceQuery();
            map.AddGround(0f);
            var world = World(registry, map);
            world.GameplayStartTick = 0;

            // y=0.3에서 떨어져 바닥에 앉기까지 일곱 틱쯤 걸린다(수렴 가속이라 처음엔 느리다).
            // 1초를 굴리면 그중 40틱 이상이 접지이고, 회복 40/s이므로 30 넘게 차 있어야 한다.
            for (int t = 0; t < 50; t++) { world.Tick(t, 0.02f); }

            Assert.Greater(diver.Get<Stamina>().Current, 20f, "발판 위에서는 스태미나가 차야 한다");
        }

        [Test]
        public void 허공에서는_스태미나가_차지_않는다()
        {
            var registry = new EntityRegistry();
            var diver = Diver("a");
            diver.Get<Stamina>().Current = 0f;
            registry.Add(diver);

            var world = World(registry);   // 면이 없는 하늘
            world.GameplayStartTick = 0;

            for (int t = 0; t < 50; t++) { world.Tick(t, 0.02f); }

            Assert.AreEqual(0f, diver.Get<Stamina>().Current, Tolerance, "공중에서는 안 찬다(젤다 규칙)");
        }

        [Test]
        public void 허공에서는_접지가_아니다()
        {
            var registry = new EntityRegistry();
            var diver = Diver("a");
            registry.Add(diver);

            var map = new HalfSpaceQuery();
            map.AddGround(0f);   // 1000m 아래 — 이번 틱엔 닿지 않는다
            var world = World(registry, map);
            world.GameplayStartTick = 0;

            world.Tick(0, 0.02f);

            Assert.IsFalse(diver.Get<GroundState>().IsGrounded);
            Assert.Less(HeightOf(registry, "a"), 1000f, "허공에서는 내려가야 한다");
        }

        [Test]
        public void Simulated가_없으면_굴리지_않는다()
        {
            var registry = new EntityRegistry();
            registry.Add(Diver("a", simulated: false));
            var world = World(registry);
            world.GameplayStartTick = 0;

            world.Tick(1, 0.02f);

            Assert.AreEqual(1000f, HeightOf(registry, "a"), Tolerance);
        }

        [Test]
        public void 캐릭터가_아니면_굴리지_않는다()
        {
            var registry = new EntityRegistry();
            registry.Add(Diver("a", kind: EntityType.Item));
            var world = World(registry);
            world.GameplayStartTick = 0;

            world.Tick(1, 0.02f);

            Assert.AreEqual(1000f, HeightOf(registry, "a"), Tolerance);
        }

        [Test]
        public void 등록_순서를_뒤집어도_결과가_같다()
        {
            // 이 월드가 존재하는 이유가 결정론이다 — 레지스트리 순회 순서는 정해져 있지 않으므로
            // 처리 순서를 id로 고정한다.
            // ⚠️ 지금 이 테스트는 공허하다: 슬라이스 2에는 엔티티 사이의 상호작용이 없어서
            // 정렬을 지워도 통과한다. 슬라이스 3이 몸싸움을 넣는 순간 load-bearing이 된다 —
            // 그때 이 자리가 "정렬이 조용히 사라진 것"을 잡는다.
            float RunWith(string[] order)
            {
                var registry = new EntityRegistry();
                foreach (var id in order) { registry.Add(Diver(id)); }
                var world = World(registry);
                world.GameplayStartTick = 0;
                for (int i = 0; i < 10; i++) { world.Tick(i, 0.02f); }
                return HeightOf(registry, "b");
            }

            Assert.AreEqual(RunWith(new[] { "a", "b", "c" }), RunWith(new[] { "c", "b", "a" }), Tolerance);
        }

        // 자세 문(발밑 여유) 관련 — 슬라이더를 끝까지 민 다이버를 만든다.
        static Entity PosingDiver(string id, float height)
        {
            var e = Diver(id);
            e.Get<GameFramework.World.Transform>().Position = new Vector3(0f, height, 0f).ToNumerics();
            e.Get<InputBuffer>().Current = new InputCommand { Posture = 1f, Glide = false, Posing = true };
            return e;
        }

        [Test]
        public void 착지하면_걷기로_돌아온다()
        {
            var registry = new EntityRegistry();
            var diver = PosingDiver("a", 0.3f);
            diver.Get<MotionState>().Value = SkydiveMotionState.Skydiving;
            registry.Add(diver);
            var map = new HalfSpaceQuery();
            map.AddGround(0f);
            var world = World(registry, map);
            world.GameplayStartTick = 0;

            // 접지는 이동 커널이 틱 끝에 적으므로 한 틱으로는 아직 false다 — 내려앉을 시간을 준다.
            for (int t = 0; t < 20; t++) { world.Tick(t, 0.02f); }

            Assert.AreEqual(SkydiveMotionState.Walking, diver.Get<MotionState>().Value);
        }

        [Test]
        public void 발밑이_비면_낙하에서_활공으로_들어간다()
        {
            var registry = new EntityRegistry();
            var diver = PosingDiver("a", 500f);   // 발밑이 뻥 뚫려 있다
            registry.Add(diver);
            var world = World(registry);
            world.GameplayStartTick = 0;

            world.Tick(0, 0.02f);

            Assert.AreEqual(SkydiveMotionState.Skydiving, diver.Get<MotionState>().Value);
        }

        [Test]
        public void 한_번_활공에_들면_지면이_가까워도_패러세일이_유지된다()
        {
            // 이게 이 설계의 핵심이다. 발밑 여유를 매 틱 보면 착지 직전에 낙하산이 접혀
            // 그대로 처박힌다 — 젤다는 땅에 닿기 직전까지 펼 수 있다.
            var registry = new EntityRegistry();
            var diver = Diver("a");
            diver.Get<GameFramework.World.Transform>().Position = new Vector3(0f, 2f, 0f).ToNumerics();
            diver.Get<MotionState>().Value = SkydiveMotionState.Skydiving;   // 이미 들어와 있다
            diver.Get<Posture>().Gliding = true;
            diver.Get<InputBuffer>().Current = new InputCommand { Glide = true, Posing = true };
            registry.Add(diver);
            var map = new HalfSpaceQuery();
            map.AddGround(0f);   // 발밑 2m — 여유(5m)보다 가깝다
            var world = World(registry, map);
            world.GameplayStartTick = 0;

            world.Tick(0, 0.02f);

            Assert.IsTrue(diver.Get<Posture>().Gliding, "지면이 가깝다고 공중에서 접히면 안 된다");
        }

        [Test]
        public void 발판_위에서_뛰면_자세를_못_잡는다()
        {
            // 선반 위에서 2m 뛰어봐야 발밑이 막혀 있으니 활공에 못 들어간다.
            var registry = new EntityRegistry();
            var diver = PosingDiver("a", 2f);
            registry.Add(diver);
            var map = new HalfSpaceQuery();
            map.AddGround(0f);
            var world = World(registry, map);
            world.GameplayStartTick = 0;

            world.Tick(0, 0.02f);

            Assert.AreEqual(0f, diver.Get<Posture>().Axis, Tolerance, "발밑이 막혀 있으면 자세가 안 잡힌다");
        }

        [Test]
        public void 발_딛고_있으면_자세를_못_잡는다()
        {
            var registry = new EntityRegistry();
            var diver = PosingDiver("a", 0.3f);
            registry.Add(diver);
            var map = new HalfSpaceQuery();
            map.AddGround(0f);
            var world = World(registry, map);
            world.GameplayStartTick = 0;

            for (int t = 0; t < 40; t++) { world.Tick(t, 0.02f); }

            Assert.AreEqual(0f, diver.Get<Posture>().Axis, Tolerance,
                "서 있는데 슬라이더로 다이브가 되면 안 된다");
        }

        [Test]
        public void 착지하면_패러세일이_저절로_접힌다()
        {
            var registry = new EntityRegistry();
            var diver = Diver("a");
            diver.Get<GameFramework.World.Transform>().Position = new Vector3(0f, 0.3f, 0f).ToNumerics();
            diver.Get<MotionState>().Value = SkydiveMotionState.Skydiving;
            diver.Get<Posture>().Gliding = true;
            diver.Get<InputBuffer>().Current = new InputCommand { Glide = true, Posing = true };
            registry.Add(diver);
            var map = new HalfSpaceQuery();
            map.AddGround(0f);
            var world = World(registry, map);
            world.GameplayStartTick = 0;

            for (int t = 0; t < 40; t++) { world.Tick(t, 0.02f); }

            Assert.IsFalse(diver.Get<Posture>().Gliding, "닿으면 접혀야 한다");
        }

        [Test]
        public void 입력이_자세로_반영된다()
        {
            var registry = new EntityRegistry();
            var diver = Diver("a");
            diver.Get<InputBuffer>().Current = new InputCommand { Posture = 1f, Posing = true };
            registry.Add(diver);
            var world = World(registry);
            world.GameplayStartTick = 0;

            for (int i = 0; i < 30; i++) { world.Tick(i, 0.02f); }   // 0.6초 > 전환 0.25초

            Assert.AreEqual(1f, diver.Get<Posture>().Axis, 1e-2f);
        }

        [Test]
        public void 자세_축은_한_틱에_끝까지_가지_않는다()
        {
            var registry = new EntityRegistry();
            var diver = Diver("a");
            diver.Get<InputBuffer>().Current = new InputCommand { Posture = 1f, Posing = true };
            registry.Add(diver);
            var world = World(registry);
            world.GameplayStartTick = 0;

            world.Tick(0, 0.02f);

            Assert.Less(diver.Get<Posture>().Axis, 0.5f, "0.02초에 4×0.02=0.08만 움직여야 한다");
            Assert.Greater(diver.Get<Posture>().Axis, 0f);
        }

        [Test]
        public void 되감으면_자세와_스태미나가_그때로_돌아간다()
        {
            var registry = new EntityRegistry();
            var diver = Diver("a");
            diver.Get<InputBuffer>().Current = new InputCommand { Posture = 1f, Glide = true, Posing = true };
            registry.Add(diver);
            var world = World(registry);
            world.GameplayStartTick = 0;

            world.Tick(0, 0.02f);
            world.SaveState(0);
            float axisAt0 = diver.Get<Posture>().Axis;
            bool glidingAt0 = diver.Get<Posture>().Gliding;
            float staminaAt0 = diver.Get<Stamina>().Current;
            bool emergencyUsedAt0 = diver.Get<Stamina>().EmergencyUsed;
            float emergencyRemainingAt0 = diver.Get<Stamina>().EmergencyRemaining;

            for (int i = 1; i <= 20; i++) { world.Tick(i, 0.02f); world.SaveState(i); }
            Assert.AreNotEqual(axisAt0, diver.Get<Posture>().Axis, "20틱 뒤엔 달라져 있어야 한다");

            Assert.IsTrue(world.LoadState(0));
            // SkydiveSavedState가 담는 다섯 필드(Axis/Gliding/Stamina/EmergencyUsed/EmergencyRemaining)
            // 전부를 확인한다 — 두 개만 재면 되감기가 "완전하다"는 주장을 실제로 재는 게 아니다.
            Assert.AreEqual(axisAt0, diver.Get<Posture>().Axis, Tolerance);
            Assert.AreEqual(glidingAt0, diver.Get<Posture>().Gliding);
            Assert.AreEqual(staminaAt0, diver.Get<Stamina>().Current, Tolerance);
            Assert.AreEqual(emergencyUsedAt0, diver.Get<Stamina>().EmergencyUsed);
            Assert.AreEqual(emergencyRemainingAt0, diver.Get<Stamina>().EmergencyRemaining, Tolerance);
        }

        [Test]
        public void 비상_펼침_중에는_손을_떼도_접히지_않는다()
        {
            // 스펙 §2.2 — 잔고 0에서의 "마지막 한 번" 펼침은 *보장된* 구제 시간이다. 손을 떼는
            // 순간 접혀 버리면 우리 조작(떼면 대자로 돌아온다)에서 그 보장이 흔한 경로로 사라진다.
            var registry = new EntityRegistry();
            var diver = Diver("a");
            diver.Get<Stamina>().Current = 0f;   // 잔고 0 — "마지막 한 번" 구간
            diver.Get<InputBuffer>().Current = new InputCommand { Glide = true, Posing = true };
            registry.Add(diver);
            var world = World(registry);
            world.GameplayStartTick = 0;

            world.Tick(0, 0.02f);   // 비상 펼침 시작
            Assert.IsTrue(diver.Get<Posture>().Gliding, "비상 펼침이 걸렸어야 한다");
            Assert.Greater(diver.Get<Stamina>().EmergencyRemaining, 0f);

            diver.Get<InputBuffer>().Current = new InputCommand { Glide = false };   // 손을 뗀다
            world.Tick(1, 0.02f);

            Assert.IsTrue(diver.Get<Posture>().Gliding,
                "비상 창이 도는 동안은 입력과 무관하게 활공이 유지돼야 한다");

            // 창(emergencyGlideTime=1초)이 다 돌 때까지 손을 뗀 채로 튕긴다.
            for (int i = 2; i <= 60; i++) { world.Tick(i, 0.02f); }

            Assert.IsFalse(diver.Get<Posture>().Gliding, "창이 끝나면 접혀야 한다");
            Assert.AreEqual(0f, diver.Get<Stamina>().EmergencyRemaining, Tolerance);
        }

        [Test]
        public void 저장된_틱의_자세를_되돌려_읽을_수_있다()
        {
            //  보정 핸들러(SkydiveServerCorrectionHandler)가 "그 틱에 내가 뭘 예측했나"를
            //  서버 스냅과 비교하려면 이 조회가 필요하다.
            var registry = new EntityRegistry();
            var diver = Diver("a");
            diver.Get<InputBuffer>().Current = new InputCommand { Posture = 1f, Posing = true };
            registry.Add(diver);
            var world = World(registry);
            world.GameplayStartTick = 0;

            world.Tick(0, 0.02f);
            world.SaveState(0);
            float axisAt0 = diver.Get<Posture>().Axis;

            for (int i = 1; i <= 20; i++) { world.Tick(i, 0.02f); world.SaveState(i); }
            float axisAfterMore = diver.Get<Posture>().Axis;
            Assert.AreNotEqual(axisAt0, axisAfterMore, "20틱 뒤엔 달라져 있어야 한다");

            Assert.IsTrue(world.TryGetSavedPosture(0, diver.Id, out var saved));
            Assert.AreEqual(axisAt0, saved.Axis, Tolerance);
            Assert.AreNotEqual(axisAfterMore, saved.Axis, "돌려준 값이 현재와 달라야 저장된 프레임을 읽은 것");
        }

        [Test]
        public void 저장_안_한_틱을_조회하면_false다()
        {
            var registry = new EntityRegistry();
            var diver = Diver("a");
            registry.Add(diver);
            var world = World(registry);
            world.GameplayStartTick = 0;

            world.Tick(0, 0.02f);
            world.SaveState(0);

            Assert.IsFalse(world.TryGetSavedPosture(999, diver.Id, out _));
        }

        [Test]
        public void 없는_엔티티_id를_조회하면_false다()
        {
            var registry = new EntityRegistry();
            var diver = Diver("a");
            registry.Add(diver);
            var world = World(registry);
            world.GameplayStartTick = 0;

            world.Tick(0, 0.02f);
            world.SaveState(0);

            Assert.IsFalse(world.TryGetSavedPosture(0, "no-such-entity", out _));
        }

        [Test]
        public void 컴포넌트가_없어도_예외가_나지_않는다()
        {
            var registry = new EntityRegistry();
            var broken = new Entity("broken");
            broken.Add(new EntityKind(EntityType.Character));
            broken.Add(new Simulated());   // Transform/Velocity/Posture 없음
            registry.Add(broken);
            registry.Add(Diver("ok"));
            var world = World(registry);
            world.GameplayStartTick = 0;

            Assert.DoesNotThrow(() => world.Tick(1, 0.02f));
        }

        [Test]
        public void 상승풍_속에서는_천천히_떨어진다()
        {
            var registry = new EntityRegistry();
            registry.Add(Diver("a"));

            var wind = new WindField();
            wind.Add(new WindCylinder(
                new System.Numerics.Vector3(0f, 1000f, 0f), 1000f, 2000f,
                new System.Numerics.Vector3(0f, 14f, 0f)));
            var world = World(registry, wind: wind);
            world.GameplayStartTick = 0;   // 안 정하면 기본값(long.MaxValue)이라 아예 안 떨어진다

            var noWindRegistry = new EntityRegistry();
            noWindRegistry.Add(Diver("a"));
            var noWindWorld = World(noWindRegistry);
            noWindWorld.GameplayStartTick = 0;

            for (long tick = 1; tick <= 100; tick++)
            {
                world.Tick(tick, 0.02f);
                noWindWorld.Tick(tick, 0.02f);
            }

            Assert.That(HeightOf(registry, "a"), Is.GreaterThan(HeightOf(noWindRegistry, "a")),
                        "상승풍을 받은 쪽이 덜 내려가야 한다");
        }

        [Test]
        public void 되감으면_실린_바람도_돌아온다()
        {
            var registry = new EntityRegistry();
            registry.Add(Diver("a"));

            var wind = new WindField();
            wind.Add(new WindCylinder(
                new System.Numerics.Vector3(0f, 1000f, 0f), 1000f, 2000f,
                new System.Numerics.Vector3(9f, 0f, 0f)));
            var world = World(registry, wind: wind);
            world.GameplayStartTick = 0;   // 안 정하면 기본값(long.MaxValue)이라 아예 안 떨어진다

            for (long tick = 1; tick <= 40; tick++)
            {
                world.Tick(tick, 0.02f);
                world.SaveState(tick);
            }
            float atTwenty = registry.Get("a").Get<WindDrift>().Value.X;

            for (long tick = 41; tick <= 80; tick++)
            {
                world.Tick(tick, 0.02f);
                world.SaveState(tick);
            }
            Assert.That(registry.Get("a").Get<WindDrift>().Value.X, Is.Not.EqualTo(atTwenty));

            Assert.IsTrue(world.LoadState(40));

            Assert.AreEqual(atTwenty, registry.Get("a").Get<WindDrift>().Value.X, Tolerance);
        }

        // 되감기가 Value만 담고 Anchor를 빠뜨리면, 볼륨 밖 틱으로 되감았을 때 살아 있는 Anchor가
        // 그 틱의 것과 달라 바람이 빠지는 속도가 어긋난다.
        [Test]
        public void 저장한_바람은_실린_값과_기준을_모두_되돌린다()
        {
            var entity = Diver("a");
            var drift = entity.Get<WindDrift>();
            drift.Value = new Vector3(1f, 2f, 3f).ToNumerics();
            drift.Anchor = new Vector3(0f, 14f, 0f).ToNumerics();

            var snapshot = SkydiveSavedState.Capture(entity);

            drift.Value = new Vector3(9f, 9f, 9f).ToNumerics();
            drift.Anchor = new Vector3(0f, 5f, 0f).ToNumerics();
            snapshot.RestoreTo(entity);

            Assert.AreEqual(1f, drift.Value.X, Tolerance);
            Assert.AreEqual(2f, drift.Value.Y, Tolerance);
            Assert.AreEqual(3f, drift.Value.Z, Tolerance);
            Assert.AreEqual(14f, drift.Anchor.Y, Tolerance);
        }

        /// <summary>
        /// 대자로 떨어지다 바닥에 닿는 틱에만 충격이 남는다. 다음 틱에도 남아 있으면 서 있는 동안
        /// 매 틱 죽으므로, 둘째 단언이 이 테스트의 핵심이다.
        /// </summary>
        [Test]
        public void 접지로_뒤집힌_틱에만_충격_속도가_남는다()
        {
            var registry = new EntityRegistry();
            var diver = Diver("a");
            //  한 틱(0.02초)에 대자 속도로 1.2m를 가므로, 2m 위에서 시작하면 두 틱째에 닿는다.
            diver.Get<GameFramework.World.Transform>().Position = new Vector3(0f, 2f, 0f).ToNumerics();
            diver.Get<Velocity>().Linear = new Vector3(0f, -Config().SpreadFallSpeed, 0f).ToNumerics();
            registry.Add(diver);

            var map = new HalfSpaceQuery();
            map.AddGround(0f);
            var world = World(registry, map);
            world.GameplayStartTick = 0;

            //  닿을 때까지 돌린다. 닿은 그 틱의 값을 잡아 둔다.
            float atLanding = 0f;
            int landedAt = -1;
            for (int t = 0; t < 10 && landedAt < 0; t++)
            {
                world.Tick(t, 0.02f);
                if (diver.Get<GroundState>().IsGrounded)
                {
                    landedAt = t;
                    atLanding = ImpactOf(registry, "a");
                }
            }

            Assert.GreaterOrEqual(landedAt, 0, "열 틱 안에 닿지 않았다 — 이 테스트가 아무것도 못 쟀다");
            Assert.That(atLanding, Is.EqualTo(Config().SpreadFallSpeed).Within(1f),
                        "닿은 틱의 충격이 대자 낙하 속도와 달랐다");

            world.Tick(landedAt + 1, 0.02f);
            Assert.IsTrue(diver.Get<GroundState>().IsGrounded, "여전히 서 있어야 한다");
            Assert.That(ImpactOf(registry, "a"), Is.EqualTo(0f).Within(Tolerance),
                        "서 있는 동안에도 값이 남으면 매 틱 죽는다");
        }

        /// <summary>공중을 떨어지는 동안에는 0이다 — "빠르다"가 아니라 "부딪혔다"를 재기 때문.</summary>
        [Test]
        public void 공중에서는_아무리_빨라도_0이다()
        {
            var registry = new EntityRegistry();
            var diver = Diver("a");
            diver.Get<Velocity>().Linear = new Vector3(0f, -Config().DiveFallSpeed, 0f).ToNumerics();
            registry.Add(diver);

            var world = World(registry);   // 면이 없는 하늘
            world.GameplayStartTick = 0;

            for (int t = 0; t < 5; t++)
            {
                world.Tick(t, 0.02f);
                Assert.IsFalse(diver.Get<GroundState>().IsGrounded);
                Assert.That(ImpactOf(registry, "a"), Is.EqualTo(0f).Within(Tolerance), $"t={t}");
            }
        }

        /// <summary>
        /// 되감기 재생이 라이브와 같은 값을 낸다 — LandingImpact를 저장 상태에 안 넣기로 한 선택의 근거다.
        /// (스펙 §7.1) 넣지 않아도 매 틱 이동이 다시 계산하므로 같은 답이 나와야 한다.
        /// </summary>
        [Test]
        public void 되감아_다시_돌려도_같은_충격이_나온다()
        {
            var registry = new EntityRegistry();
            var diver = Diver("a");
            diver.Get<GameFramework.World.Transform>().Position = new Vector3(0f, 2f, 0f).ToNumerics();
            diver.Get<Velocity>().Linear = new Vector3(0f, -Config().SpreadFallSpeed, 0f).ToNumerics();
            registry.Add(diver);

            var map = new HalfSpaceQuery();
            map.AddGround(0f);
            var world = World(registry, map);
            world.GameplayStartTick = 0;

            world.Tick(0, 0.02f);
            world.SaveState(0);
            world.Tick(1, 0.02f);
            float live = ImpactOf(registry, "a");

            world.LoadState(0);
            world.Tick(1, 0.02f);
            Assert.That(ImpactOf(registry, "a"), Is.EqualTo(live).Within(Tolerance),
                        "재생이 라이브와 다른 충격을 냈다 — 저장 상태에서 빠뜨린 것이 있다");
        }

        /// <summary>
        /// 스펙 §2.0. 결승선은 몸 바운드가 선을 지나면 성립하고 착지는 발이 멈춰야 성립하는데,
        /// 대자 60m/s면 한 틱에 1.2m를 가고 몸은 1.8m라 <b>선을 먼저 넘고 한두 틱 뒤에 부딪히는</b>
        /// 순간이 실재한다. 순서만으로는 같은 틱의 죽음밖에 못 막으므로 완주 조건에 접지를 넣는다.
        /// </summary>
        [Test]
        public void 선을_넘어도_접지_전에는_완주가_아니다()
        {
            var registry = new EntityRegistry();
            var diver = FinishingDiver("a");
            //  선(윗면 y=1.5) 아래로 이미 들어와 있지만 아직 바닥(y=0)에 닿지는 않은 자리.
            diver.Get<GameFramework.World.Transform>().Position = new Vector3(0f, 1.2f, 0f).ToNumerics();
            diver.Get<Velocity>().Linear = new Vector3(0f, -Config().SpreadFallSpeed, 0f).ToNumerics();
            registry.Add(diver);

            //  바닥을 훨씬 아래에 둬서 이 틱엔 접지가 안 되게 한다.
            var map = new HalfSpaceQuery();
            map.AddGround(-100f);
            var world = World(registry, map, finish: GroundFinishLine());
            world.GameplayStartTick = 0;

            world.Tick(0, 0.02f);

            Assert.IsFalse(diver.Get<GroundState>().IsGrounded, "이 테스트는 공중 상태를 재야 한다");
            Assert.IsFalse(Finished(registry, "a"), "접지 전인데 완주로 잡혔다");
        }

        [Test]
        public void 치명_속도로_접지하면_완주가_아니다()
        {
            var registry = new EntityRegistry();
            var diver = FinishingDiver("a");
            diver.Get<GameFramework.World.Transform>().Position = new Vector3(0f, 2f, 0f).ToNumerics();
            diver.Get<Velocity>().Linear = new Vector3(0f, -Config().SpreadFallSpeed, 0f).ToNumerics();
            registry.Add(diver);

            var map = new HalfSpaceQuery();
            map.AddGround(0f);
            var world = World(registry, map, finish: GroundFinishLine());
            world.GameplayStartTick = 0;

            //  착지 틱에서 멈춘다(접지로_뒤집힌_틱에만_충격_속도가_남는다와 같은 이유) — 충격은
            //  접지가 뒤집히는 그 한 틱만 남고 다음 틱엔 0으로 리셋된다(Task 3에서 검증됨). 죽음을
            //  되돌리는 건 서버 전용(스펙 §7)이라 이 공유 시뮬 테스트엔 그 뒤처리가 없다 — 착지 틱을
            //  지나 계속 돌리면 다음 틱엔 치명이 아니게 되어 완주가 잡혀 버린다(실측: 착지+1틱에
            //  Finished=True). 이 테스트가 재려는 것은 "착지 그 순간의 게이트"이므로 거기서 멈춘다.
            int landedAt = -1;
            for (int t = 0; t < 10 && landedAt < 0; t++)
            {
                world.Tick(t, 0.02f);
                if (diver.Get<GroundState>().IsGrounded) { landedAt = t; }
            }

            Assert.GreaterOrEqual(landedAt, 0, "열 틱 안에 닿지 않았다 — 이 테스트가 아무것도 못 쟀다");
            Assert.IsTrue(diver.Get<GroundState>().IsGrounded, "이 테스트는 접지한 상태를 재야 한다");
            Assert.IsFalse(Finished(registry, "a"), "치명 착지인데 완주로 잡혔다");
        }

        /// <summary>
        /// 방지턱이다 — 결승선을 잘못 놓으면 위 두 테스트가 "완주 아님"으로 둘 다 초록이 되어
        /// 아무것도 못 잰다.
        /// </summary>
        [Test]
        public void 안전_속도로_접지하면_완주다()
        {
            var registry = new EntityRegistry();
            var diver = FinishingDiver("a");
            //  바닥 바로 위에서 활공 속도로 내려온다 — 충격이 문턱 아래다.
            diver.Get<GameFramework.World.Transform>().Position = new Vector3(0f, 0.3f, 0f).ToNumerics();
            diver.Get<Velocity>().Linear = new Vector3(0f, -Config().GlideFallSpeed, 0f).ToNumerics();
            diver.Get<Posture>().Gliding = true;
            registry.Add(diver);

            var map = new HalfSpaceQuery();
            map.AddGround(0f);
            var world = World(registry, map, finish: GroundFinishLine());
            world.GameplayStartTick = 0;

            for (int t = 0; t < 10; t++) { world.Tick(t, 0.02f); }

            Assert.IsTrue(diver.Get<GroundState>().IsGrounded);
            Assert.IsTrue(Finished(registry, "a"), "안전하게 내려섰는데 완주가 안 됐다");
        }

        //  문 자세는 틱의 순수 함수라(스펙 §2.3), 그 판이 틱의 어느 지점에서 서느냐가 곧
        //  "누가 그 자세를 보느냐"다. 클라 뷰가 프레임 사이에 패널을 소수 틱 자세로 옮겨 두므로,
        //  틱이 그것을 덮기 전에 도는 질의는 클라에만 있는 자세를 보게 된다.
        [Test]
        public void 발밑_여유를_재기_전에_문이_이_틱_자세로_선다()
        {
            var door = MakeDoor();
            door.Pose(999);   // 뷰가 프레임 사이에 남겨 둔 자세

            var doors = new DoorField();
            doors.Add(door);
            var probe = new PanelPoseAtRaycast(door);

            var registry = new EntityRegistry();
            registry.Add(Diver("a"));
            var world = World(registry, probe, doors: doors);
            world.GameplayStartTick = 0;

            world.Tick(12, 0.02f);

            Assert.IsTrue(probe.Sampled, "발밑 여유 레이가 안 불렸다 — 이 테스트가 아무것도 못 잰다");
            door.Pose(12);
            Assert.AreEqual(door.PanelA.localPosition.x, probe.PanelX, Tolerance);
        }

        //  출발 전에도 문은 돈다 — 여기서 안 세우면 뷰가 옮겨 둔 자세가 출발 순간까지 남는다.
        [Test]
        public void 출발_전에도_문은_이_틱_자세로_선다()
        {
            var door = MakeDoor();
            door.Pose(999);

            var doors = new DoorField();
            doors.Add(door);

            var registry = new EntityRegistry();
            registry.Add(Diver("a"));
            var world = World(registry, doors: doors);
            world.GameplayStartTick = 100;

            world.Tick(12, 0.02f);
            float posed = door.PanelA.localPosition.x;

            door.Pose(12);
            Assert.AreEqual(door.PanelA.localPosition.x, posed, Tolerance);
        }

        readonly List<GameObject> doorRoots = new List<GameObject>();

        [TearDown]
        public void DestroyDoors()
        {
            foreach (var root in doorRoots)
            {
                if (root != null) { UnityEngine.Object.DestroyImmediate(root); }
            }
            doorRoots.Clear();
        }

        //  네 구간이 다 나오는 문: 0~9 열림, 10~14 닫히는 중, 15~34 닫힘, 35~39 열리는 중.
        DoorVolume MakeDoor()
        {
            var root = new GameObject("Door");
            doorRoots.Add(root);

            var volume = root.AddComponent<DoorVolume>();
            volume.HalfWidth = 8f;
            volume.HalfDepth = 4f;
            volume.Thickness = 0.5f;
            volume.AxisAngleDegrees = 0f;
            volume.Period = 40;
            volume.OpenTicks = 10;
            volume.MoveTicks = 5;
            volume.Phase = 0;

            volume.PanelA = new GameObject("PanelA").transform;
            volume.PanelA.SetParent(root.transform, worldPositionStays: false);
            volume.PanelB = new GameObject("PanelB").transform;
            volume.PanelB.SetParent(root.transform, worldPositionStays: false);
            return volume;
        }

        //  "틱이 끝난 뒤" 세로 간격이 (1.0, 1.8)에 오게 하는 시작 간격. 떨어지는 속도가 다르면
        //  한 틱에 좁혀지는 양도 달라서 값이 둘이다 — 초속 60이면 1.2m, 6이면 0.12m를 간다.
        const float HardSpawnGap = 2.6f;   // 1.2m 좁혀져 ≈1.41
        const float SoftSpawnGap = 1.6f;   // 0.12m 좁혀져 ≈1.49

        static Entity DiverAt(string id, float x, float y)
        {
            var e = Diver(id);
            e.Get<GameFramework.World.Transform>().Position = new Vector3(x, y, 0f).ToNumerics();
            return e;
        }

        //  예외 분기(간격 ≤ 1.0에서 거리 0 → 규칙으로 정한 법선)로 통과하지 않았음을 못 박는다.
        static void AssertVerticalContact(EntityRegistry r, string lowerId, string upperId)
        {
            float gap = HeightOf(r, upperId) - HeightOf(r, lowerId);
            Assert.Greater(gap, 1.0f,
                $"세로 간격 {gap:F3}은 심 선분이 겹치는 구간이라 접촉 법선이 기하가 아니라 " +
                "규칙으로 정해진다 — 이 테스트는 판별을 시험하지 못한다");
        }

        [Test]
        public void 남의_머리에_세게_떨어지면_죽을_속도가_기록된다()
        {
            var registry = new EntityRegistry();
            registry.Add(DiverAt("a", 0f, 1000f));
            var upper = DiverAt("b", 0f, 1000f + HardSpawnGap);
            upper.Get<Velocity>().Linear = new Vector3(0f, -60f, 0f).ToNumerics();
            registry.Add(upper);

            var world = World(registry);   // 면이 없는 하늘 — 맵 접지가 섞이지 않는다
            world.GameplayStartTick = 0;

            world.Tick(0, 0.02f);

            AssertVerticalContact(registry, "a", "b");
            Assert.Greater(ImpactOf(registry, "b"), 15f);
        }

        [Test]
        public void 남의_머리에_살살_내려오면_서고_죽지_않는다()
        {
            var registry = new EntityRegistry();
            registry.Add(DiverAt("a", 0f, 1000f));
            var upper = DiverAt("b", 0f, 1000f + SoftSpawnGap);
            upper.Get<Velocity>().Linear = new Vector3(0f, -6f, 0f).ToNumerics();
            registry.Add(upper);

            var world = World(registry);
            world.GameplayStartTick = 0;

            world.Tick(0, 0.02f);

            AssertVerticalContact(registry, "a", "b");
            Assert.IsTrue(registry.Get("b").Get<GroundState>().IsGrounded);
            Assert.LessOrEqual(ImpactOf(registry, "b"), 15f);
        }

        [Test]
        public void 옆으로_부딪히면_접지도_충격도_없다()
        {
            var registry = new EntityRegistry();
            registry.Add(DiverAt("a", 0f, 1000f));
            registry.Add(DiverAt("b", 0.5f, 1000f));

            var world = World(registry);
            world.GameplayStartTick = 0;

            world.Tick(0, 0.02f);

            Assert.IsFalse(registry.Get("a").Get<GroundState>().IsGrounded);
            Assert.IsFalse(registry.Get("b").Get<GroundState>().IsGrounded);
            Assert.AreEqual(0f, ImpactOf(registry, "a"), 1e-4f);
            Assert.AreEqual(0f, ImpactOf(registry, "b"), 1e-4f);
        }

        //  "틱이 끝난 뒤" 세로 간격이 (0.12, 1.0)에 오게 하는 시작 간격. 이 구간이 조용히
        //  실패하던 자리다 — 심 선분(길이 1.0)이 겹쳐 <b>틱 끝 모습의 접촉 방향에 세로 성분이
        //  아예 없다</b>. 초속 90(다이브)이면 한 틱에 ≈1.79 m를 가므로 2.4 − 1.79 ≈ 0.61.
        const float StompSpawnGap = 2.4f;
        const float StompFallSpeed = 90f;

        //  일부러 가로로 조금 어긋나게 둔다. 정확히 포개면 심 거리가 0이 되어 BodyOverlap의 예외
        //  분기(규칙으로 정한 아래 방향)로 빠져 <b>옛 코드에서도</b> 접지가 나온다 — 실제 플레이에서
        //  x·z가 딱 맞아떨어지는 일은 없으니 그 우연에 기대면 회귀를 못 잡는다.
        const float StompSideOffset = 0.3f;

        //  몸싸움이 풀기 <b>직전</b>의 세로 간격. 해소가 끝난 자리로는 잴 수 없다 — 밀어내기가
        //  이미 간격을 벌려 놓았기 때문이다. 가로로 멀찍이 떼어 놓아도 세로 운동은 똑같다:
        //  맵도 없고(하늘) 가로로 미는 힘도 없다.
        static float GapBeforeContact(float spawnGap, float fallSpeed)
        {
            var registry = new EntityRegistry();
            registry.Add(DiverAt("a", 0f, 1000f));
            var upper = DiverAt("b", 50f, 1000f + spawnGap);   // 서로 닿을 수 없는 거리
            upper.Get<Velocity>().Linear = new Vector3(0f, -fallSpeed, 0f).ToNumerics();
            registry.Add(upper);

            var world = World(registry);
            world.GameplayStartTick = 0;
            world.Tick(0, 0.02f);

            return HeightOf(registry, "b") - HeightOf(registry, "a");
        }

        [Test]
        public void 깊이_파고든_밟기도_접지와_치명_충격을_남긴다()
        {
            var registry = new EntityRegistry();
            registry.Add(DiverAt("a", 0f, 1000f));
            var upper = DiverAt("b", StompSideOffset, 1000f + StompSpawnGap);
            upper.Get<Velocity>().Linear = new Vector3(0f, -StompFallSpeed, 0f).ToNumerics();
            registry.Add(upper);

            var world = World(registry);   // 면이 없는 하늘 — 맵 접지가 섞이지 않는다
            world.GameplayStartTick = 0;

            world.Tick(0, 0.02f);

            float freeGap = GapBeforeContact(StompSpawnGap, StompFallSpeed);
            AssertDeepOverlapContact(freeGap);

            float resolvedGap = HeightOf(registry, "b") - HeightOf(registry, "a");
            TestContext.WriteLine(
                $"밟기: 해소 전 세로 간격 {freeGap:F4} m → 해소 뒤 {resolvedGap:F4} m " +
                $"(벌어진 양 {resolvedGap - freeGap:F4} m, 한 몸당 {(resolvedGap - freeGap) * 0.5f:F4} m)");

            Assert.IsTrue(registry.Get("b").Get<GroundState>().IsGrounded, "밟은 쪽이 접지로 안 잡혔다");
            Assert.Greater(ImpactOf(registry, "b"), 15f, "죽을 속도가 안 기록됐다");
        }

        //  이 테스트가 <b>정말</b> 깨지던 구간을 재고 있는지 못 박는다. 값이 흘러 세로 간격이
        //  (1.0, 1.8)로 돌아가면 이 테스트는 "이미 되던 것"을 재며 엉뚱한 이유로 초록이 된다.
        static void AssertDeepOverlapContact(float gap)
        {
            Assert.Greater(gap, 0.12f, $"세로 간격 {gap:F3}이 너무 좁다");
            Assert.Less(gap, 1.0f,
                $"세로 간격 {gap:F3}은 심 선분이 안 겹치는 구간이라 예전 코드도 세로 법선을 냈다 " +
                "— 이 테스트가 회귀를 못 잡는다");

            //  같은 말을 기하로 한 번 더. 틱 끝 모습의 방향에 세로 성분이 0이어야 "예전엔 접지가
            //  아예 안 나오던 자리"다.
            Assert.IsTrue(BodyOverlap.TryCompute(
                Vector3.zero, new Vector3(StompSideOffset, gap, 0f),
                0.4f, 1.8f, out Vector3 endPushDir, out _), "겹치지도 않았다");
            Assert.AreEqual(0f, endPushDir.y, 1e-6f,
                "틱 끝 모습의 접촉 방향에 세로 성분이 남아 있다 — 예전 코드로도 접지가 나오는 자리다");
        }

        [Test]
        public void 빠르게_옆에서_부딪혀도_접지가_안_생긴다()
        {
            //  옆으로_부딪히면_접지도_충격도_없다는 둘이 나란히 떨어져 <b>상대 이동이 0</b>이라
            //  훑기가 아예 돌지 않는다(틱 끝 모습으로 물러선다). 서로 다가오는 경우도 따로 본다 —
            //  C1의 실패 모습이 바로 "옆으로 밀리고 접지가 없다"라, 훑기가 옆 접촉까지 세로로
            //  오인하기 시작해도 그 테스트로는 못 잡는다.
            var registry = new EntityRegistry();
            var left = DiverAt("a", -0.6f, 1000f);
            left.Get<Velocity>().Linear = new Vector3(12f, 0f, 0f).ToNumerics();
            registry.Add(left);
            var right = DiverAt("b", 0.6f, 1000f);
            right.Get<Velocity>().Linear = new Vector3(-12f, 0f, 0f).ToNumerics();
            registry.Add(right);

            var world = World(registry);
            world.GameplayStartTick = 0;

            world.Tick(0, 0.02f);

            //  실제로 부딪혔는지부터. 자리만 보면 안 된다 — 안 닿아도 가로 거리는 0.8보다 작다.
            //  가로 속도가 뒤집혀야 접촉이 있었다는 증거가 된다(들어올 때 +12였다).
            Assert.Less(registry.Get("a").Get<Velocity>().Linear.ToUnity().x, 0f,
                "가로 속도가 안 튕겼다 — 이 테스트가 옆 충돌을 재고 있지 않다");

            Assert.IsFalse(registry.Get("a").Get<GroundState>().IsGrounded);
            Assert.IsFalse(registry.Get("b").Get<GroundState>().IsGrounded);
            Assert.AreEqual(0f, ImpactOf(registry, "a"), 1e-4f);
            Assert.AreEqual(0f, ImpactOf(registry, "b"), 1e-4f);
        }

        [Test]
        public void 몸이_서로_통과하지_않는다()
        {
            var registry = new EntityRegistry();
            registry.Add(DiverAt("a", 0f, 1000f));
            registry.Add(DiverAt("b", 0.3f, 1000f));

            var world = World(registry);
            world.GameplayStartTick = 0;

            world.Tick(0, 0.02f);

            Vector3 pa = registry.Get("a").Get<GameFramework.World.Transform>().Position.ToUnity();
            Vector3 pb = registry.Get("b").Get<GameFramework.World.Transform>().Position.ToUnity();
            float horizontal = new Vector2(pb.x - pa.x, pb.z - pa.z).magnitude;
            Assert.GreaterOrEqual(horizontal, 0.79f);   // 지름 0.8 − 허용 겹침 0.01
        }

        //  결과가 아니라 질의 시점의 상태를 적어 둔다 — 재려는 것이 "언제"이기 때문이다.
        class PanelPoseAtRaycast : GameFramework.Physics.ICollisionQuery
        {
            readonly DoorVolume door;

            public bool Sampled { get; private set; }
            public float PanelX { get; private set; }

            public PanelPoseAtRaycast(DoorVolume door) => this.door = door;

            public GameFramework.Physics.CollisionHit Raycast(
                Vector3 origin, Vector3 direction, float distance, int layerMask)
            {
                if (Sampled == false)
                {
                    Sampled = true;
                    PanelX = door.PanelA.localPosition.x;
                }
                return GameFramework.Physics.CollisionHit.None;
            }

            public GameFramework.Physics.CollisionHit CapsuleCast(
                Vector3 point1, Vector3 point2, float radius,
                Vector3 direction, float distance, int layerMask)
                => GameFramework.Physics.CollisionHit.None;

            public GameFramework.Physics.CollisionHit[] OverlapSphere(
                Vector3 center, float radius, int layerMask)
                => System.Array.Empty<GameFramework.Physics.CollisionHit>();
        }
    }
}
