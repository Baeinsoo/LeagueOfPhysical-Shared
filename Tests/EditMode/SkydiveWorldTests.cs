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

        static SkydiveWorld World(EntityRegistry registry)
            => new SkydiveWorld(registry, new WorldEventBuffer(),
                                new SkydiveMoveSystem(), new StaminaSystem(), Config());

        static Entity Diver(string id, bool simulated = true, EntityType kind = EntityType.Character)
        {
            var e = new Entity(id);
            e.Add(new GameFramework.World.Transform { Position = new Vector3(0f, 1000f, 0f).ToNumerics() });
            e.Add(new Velocity());
            e.Add(new EntityKind(kind));
            e.Add(new Posture());
            e.Add(new Stamina { Current = 100f });
            e.Add(new InputBuffer());
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
            float staminaAt0 = diver.Get<Stamina>().Current;

            for (int i = 1; i <= 20; i++) { world.Tick(i, 0.02f); world.SaveState(i); }
            Assert.AreNotEqual(axisAt0, diver.Get<Posture>().Axis, "20틱 뒤엔 달라져 있어야 한다");

            Assert.IsTrue(world.LoadState(0));
            Assert.AreEqual(axisAt0, diver.Get<Posture>().Axis, Tolerance);
            Assert.AreEqual(staminaAt0, diver.Get<Stamina>().Current, Tolerance);
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
