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
                spreadFallSpeed: 25f, diveFallSpeed: 45f, glideFallSpeed: 6f,
                spreadMoveSpeed: 12f, diveMoveSpeed: 18f, glideMoveSpeed: 14f,
                spreadTurnAccel: 22f, diveTurnAccel: 6f, glideTurnAccel: 18f,
                fallApproach: 30f, postureRate: 4f,
                bodyRadius: 0.4f, bodyHeight: 1.8f, groundY: 0f,
                staminaMax: 100f, glideDrain: 20f, groundRecover: 40f, emergencyGlideTime: 1f);

        // 기본 맵은 면이 하나도 없는 하늘이다(HalfSpaceQuery에 면을 안 넣으면 늘 CollisionHit.None).
        static SkydiveWorld World(EntityRegistry registry, GameFramework.Physics.ICollisionQuery query = null)
            => new SkydiveWorld(registry, new WorldEventBuffer(),
                                new SkydiveMoveSystem(), new StaminaSystem(), Config(),
                                query ?? new HalfSpaceQuery(), layerMask: ~0);

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
            if (simulated) { e.Add(new Simulated()); }
            return e;
        }

        static float HeightOf(EntityRegistry r, string id)
            => r.Get(id).Get<GameFramework.World.Transform>().Position.Y;

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

        [Test]
        public void 입력이_자세로_반영된다()
        {
            var registry = new EntityRegistry();
            var diver = Diver("a");
            diver.Get<InputBuffer>().Current = new InputCommand { Posture = 1f };
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
            diver.Get<InputBuffer>().Current = new InputCommand { Posture = 1f };
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
            diver.Get<InputBuffer>().Current = new InputCommand { Posture = 1f, Glide = true };
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
            diver.Get<InputBuffer>().Current = new InputCommand { Glide = true };
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
            diver.Get<InputBuffer>().Current = new InputCommand { Posture = 1f };
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
    }
}
