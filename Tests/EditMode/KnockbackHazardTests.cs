using System.Collections.Generic;
using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class KnockbackHazardTests
    {
        const float Dt = 0.02f;

        static readonly KnockbackConfig Hazard = new KnockbackConfig(120f, 6.2f, 14f, 17f, 1.5f, 0f, 5f, 11f, 30f, 18f, 0.3f, 0.55f, 3, 0.95f, 0.26f, 1.2f, 1, 6f, 0.5f, 0.5f, 0.3f, 0.1f, 0.2f, 0.8f, 5f,
            new List<MountedArcheryZoneStageConfig>(), null, 1, 10f, 0.7f);
        static readonly KnockbackConfig Cliff = new KnockbackConfig(120f, 6.2f, 14f, 17f, 1.5f, 0f, 5f, 11f, 30f, 18f, 0.3f, 0.55f, 3, 0.95f, 0.26f, 1.2f, 1, 6f, 0.5f, 0.5f, 0.3f, 0.1f, 0.2f, 0.8f, 5f,
            new List<MountedArcheryZoneStageConfig>());

        //  100~200틱에 걸쳐 반지름 toRadius까지 줄어드는 원 하나.
        static ZoneStage Stage(KnockbackFloor floor, float toRadius)
            => new ZoneStage(0, 0, 100, 200, Vector2.zero, floor.ZoneStartRadius, Vector2.zero, toRadius, 4f);

        static KnockbackFloor ShrunkFloor(KnockbackConfig config, float toRadius)
        {
            var floor = config.CreateFloor();
            floor.SetZoneStages(new[] { Stage(floor, toRadius) });
            return floor;
        }

        [Test]
        public void 위험_지대_방식이면_원이_줄어도_칸이_남는다()
        {
            var floor = ShrunkFloor(Hazard, 3f);
            Assert.IsTrue(floor.IsSolid(0.1f, 0.1f, 200));
            Assert.IsTrue(floor.IsSolid(12f, 5f, 200));
            Assert.IsTrue(floor.IsTileSolid(8, 3, 200));
        }

        [Test]
        public void 원_밖_칸은_위험_지대()
        {
            var floor = ShrunkFloor(Hazard, 3f);
            Assert.IsTrue(floor.InHazard(12f, 5f, 200));
        }

        [Test]
        public void 원_안_칸은_위험_지대가_아니다()
        {
            var floor = ShrunkFloor(Hazard, 11f);
            Assert.IsFalse(floor.InHazard(0.1f, 0.1f, 200));
            Assert.IsFalse(floor.InHazard(0.1f, 0.1f, 0));
        }

        [Test]
        public void 반지름_0이면_원판_전체가_위험_지대()
        {
            var floor = ShrunkFloor(Hazard, 0f);
            foreach (var t in floor.Tiles)
            {
                Assert.IsTrue(floor.IsTileHazard(t.x, t.z, 200));
                Assert.IsTrue(floor.IsTileSolid(t.x, t.z, 200));
            }
        }

        [Test]
        public void 원판_밖은_바닥이_아니다()
        {
            var floor = ShrunkFloor(Hazard, 0f);
            Assert.IsFalse(floor.IsSolid(18f, 0f, 300));
            Assert.IsFalse(floor.InHazard(18f, 0f, 300));
        }

        [Test]
        public void 낭떠러지_방식은_지금과_같다()
        {
            var floor = ShrunkFloor(Cliff, 3f);
            Assert.IsFalse(floor.IsTileSolid(8, 3, 300));
            Assert.IsTrue(floor.IsTileSolid(0, 0, 300));
            foreach (var t in floor.Tiles)
            {
                Assert.IsFalse(floor.IsTileHazard(t.x, t.z, 300));
            }
        }

        //  칸 (10, 3)의 가장 먼 꼭짓점은 원판 중심에서 약 17.56m. 원 반지름이 그 아래로 내려오는 첫 틱이 104다(100틱에 축소 시작, 가운데로 줄어듦).
        [Test]
        public void 위험_지대_깜박임은_위험_지대가_되기_전에_켜진다()
        {
            var floor = ShrunkFloor(Hazard, 3f);
            Assert.IsFalse(floor.IsTileHazard(10, 3, 103));
            Assert.IsTrue(floor.IsTileHazard(10, 3, 104));
            Assert.IsFalse(floor.IsWarning(10, 3, 53, 50));
            Assert.IsTrue(floor.IsWarning(10, 3, 54, 50));
            Assert.IsTrue(floor.IsWarning(10, 3, 103, 50));
            Assert.IsFalse(floor.IsWarning(10, 3, 104, 50));
            Assert.IsFalse(floor.IsWarning(0, 0, 54, 50));
        }

        //  마지막 원이 200틱에 끝난다. 서든데스는 그 뒤 750틱(15초)에 걸쳐 원판 반지름 17m를 0까지 줄인다.
        [Test]
        public void 마지막_원_뒤_바깥부터_무너진다()
        {
            var floor = ShrunkFloor(Hazard, 3f);
            Assert.IsTrue(floor.IsTileSolid(10, 3, 200));
            Assert.IsFalse(floor.IsTileSolid(10, 3, 201));
            Assert.IsTrue(floor.IsTileSolid(0, 0, 201));
            Assert.IsTrue(floor.IsTileSolid(0, 0, 500));
            Assert.IsFalse(floor.IsTileSolid(10, 3, 400));
        }

        [Test]
        public void 서든데스_D초_뒤_전부_무너진다()
        {
            var floor = ShrunkFloor(Hazard, 3f);
            foreach (var t in floor.Tiles)
            {
                Assert.IsFalse(floor.IsTileSolid(t.x, t.z, 200 + 750));
            }
            Assert.IsTrue(floor.IsTileSolid(0, 0, 200 + 300));
        }

        [Test]
        public void 서든데스로_무너질_칸은_미리_깜박인다()
        {
            var floor = ShrunkFloor(Hazard, 3f);
            Assert.IsTrue(floor.IsWarning(10, 3, 200, 50));
        }

        [Test]
        public void 단계가_없으면_서든데스도_없다()
        {
            var floor = Hazard.CreateFloor();
            foreach (var t in floor.Tiles)
            {
                Assert.IsTrue(floor.IsTileSolid(t.x, t.z, 1000000));
            }
        }

        [Test]
        public void 낭떠러지_방식엔_서든데스가_영향_없다()
        {
            var floor = ShrunkFloor(Cliff, 11f);
            Assert.IsTrue(floor.IsTileSolid(0, 0, 200 + 700));
        }

        [Test]
        public void 서든데스로_칸이_무너지면_기둥도_가라앉는다()
        {
            var floor = ShrunkFloor(Hazard, 3f);
            var pillars = new KnockbackPillars(new KnockbackPillarLayout(4, 7f, 0.9f, 3f), floor);
            Assert.IsTrue(pillars.IsStanding(0, 200));
            Assert.IsFalse(pillars.IsStanding(0, 200 + 750));
            long collapse = pillars.CollapseTick(0);
            Assert.Greater(collapse, 200);
            Assert.LessOrEqual(collapse, 200 + 750);
        }

        static KnockbackConfig WithRate(float perSecond)
            => new KnockbackConfig(120f, 6.2f, 14f, 17f, 1.5f, 0f, 5f, 11f, 30f, 18f, 0.3f, 0.55f, 3, 0.95f, 0.26f, 1.2f, 1, 6f, 0.5f, 0.5f, 0.3f, 0.1f, 0.2f, 0.8f, 5f,
                new List<MountedArcheryZoneStageConfig>(), null, 1, perSecond, 0.7f);

        [Test]
        public void 위험도_간격은_가장_가까운_틱으로_반올림한다()
        {
            Assert.AreEqual(5, WithRate(10f).HazardDamageIntervalTicks);
            Assert.AreEqual(2, WithRate(30f).HazardDamageIntervalTicks);
            Assert.AreEqual(1, WithRate(45f).HazardDamageIntervalTicks);
            Assert.AreEqual(1, WithRate(100f).HazardDamageIntervalTicks);
            Assert.AreEqual(0, WithRate(0f).HazardDamageIntervalTicks);
            Assert.AreEqual(0, WithRate(-3f).HazardDamageIntervalTicks);
        }

        [Test]
        public void 카운트다운_중엔_위험도가_오르지_않는다()
        {
            var body = NewBody("b", 0f, 0f);
            var world = MakeWorld(Hazard, ShrunkFloor(Hazard, 0f), body);
            world.GameplayStartTick = 1000;
            for (long t = 200; t < 250; t++) world.Tick(t, Dt);

            Assert.AreEqual(0, body.Get<KnockbackBody>().DamagePercent);
        }

        static Entity NewBody(string id, float x, float z)
        {
            var body = new Entity(id);
            body.Add(new GameFramework.World.Transform { Position = new System.Numerics.Vector3(x, 0f, z) });
            body.Add(new GameFramework.World.Velocity());
            body.Add(new CapsuleShape(BodySizes.CharacterRadius, BodySizes.CharacterHeight));
            body.Add(new GroundState { IsGrounded = true });
            body.Add(new InputBuffer());
            body.Add(new Simulated());
            body.Add(new KnockbackGun(Hazard.AmmoMax));
            body.Add(new KnockbackBody());
            body.Add(new EntityKind(EntityType.Character));
            return body;
        }

        static KnockbackWorld MakeWorld(KnockbackConfig config, KnockbackFloor floor, Entity body)
        {
            var registry = new EntityRegistry();
            registry.Add(body);
            return new KnockbackWorld(registry, new WorldEventBuffer(),
                config.CreateMoveSystem(floor, new FlappyWorldFixture.NeverHit(), ~0),
                new FlappyWorldFixture.NoopMotionBridge(), floor,
                new KnockbackFireSystem(config),
                new BodyCollisionSystem(BodySizes.CharacterRadius, BodySizes.CharacterHeight, config.BodyRestitution, new Vector3(1, 0, 1)),
                config)
            { GameplayStartTick = 0 };
        }

        [Test]
        public void 위험_지대에서_0점1초마다_1씩_오른다()
        {
            var body = NewBody("b", 0f, 0f);
            var world = MakeWorld(Hazard, ShrunkFloor(Hazard, 0f), body);
            for (long t = 200; t < 250; t++) world.Tick(t, Dt);

            Assert.AreEqual(10, body.Get<KnockbackBody>().DamagePercent);
        }

        [Test]
        public void 원_안에서는_오르지_않는다()
        {
            var body = NewBody("b", 0f, 0f);
            var world = MakeWorld(Hazard, ShrunkFloor(Hazard, 11f), body);
            for (long t = 200; t < 250; t++) world.Tick(t, Dt);

            Assert.AreEqual(0, body.Get<KnockbackBody>().DamagePercent);
        }

        [Test]
        public void 낭떠러지_방식에서는_오르지_않는다()
        {
            var body = NewBody("b", 0f, 0f);
            var world = MakeWorld(Cliff, ShrunkFloor(Cliff, 0f), body);
            for (long t = 200; t < 250; t++) world.Tick(t, Dt);

            Assert.AreEqual(0, body.Get<KnockbackBody>().DamagePercent);
        }

        [Test]
        public void 떨어지는_중에는_오르지_않는다()
        {
            var body = NewBody("b", 0f, 0f);
            body.Get<GameFramework.World.Transform>().Position = new System.Numerics.Vector3(0f, -1f, 0f);
            body.Get<GroundState>().IsGrounded = false;
            var world = MakeWorld(Hazard, ShrunkFloor(Hazard, 0f), body);
            for (long t = 200; t < 210; t++) world.Tick(t, Dt);

            Assert.AreEqual(0, body.Get<KnockbackBody>().DamagePercent);
        }

        [Test]
        public void 되감아_다시_계산해도_같은_위험도()
        {
            var body = NewBody("b", 0f, 0f);
            var world = MakeWorld(Hazard, ShrunkFloor(Hazard, 0f), body);
            for (long t = 200; t <= 210; t++) { world.Tick(t, Dt); world.SaveState(t); }
            for (long t = 211; t <= 237; t++) world.Tick(t, Dt);
            int live = body.Get<KnockbackBody>().DamagePercent;
            Assert.Greater(live, 2);
            //  되감기 전에 몇 틱 더 달려 두어, 틱 번호가 아닌 호출 횟수로 세는 구현이면 위상이 어긋나게 한다.
            for (long t = 238; t <= 243; t++) world.Tick(t, Dt);

            Assert.IsTrue(world.LoadState(210));
            for (long t = 211; t <= 237; t++) world.Tick(t, Dt);

            Assert.AreEqual(live, body.Get<KnockbackBody>().DamagePercent);
        }

        static float TopSpeed(KnockbackConfig config, KnockbackFloor floor)
        {
            var body = NewBody("b", 0f, 0f);
            var world = MakeWorld(config, floor, body);
            for (long t = 300; t < 340; t++)
            {
                body.Get<InputBuffer>().Current = new InputCommand { Horizontal = 0f, Vertical = 1f };
                world.Tick(t, Dt);
            }
            return body.Get<GameFramework.World.Velocity>().Linear.Z;
        }

        [Test]
        public void 위험_지대에선_느리다()
        {
            float normal = TopSpeed(Hazard, ShrunkFloor(Hazard, 11f));
            float slow = TopSpeed(Hazard, ShrunkFloor(Hazard, 0f));
            Assert.AreEqual(6.2f, normal, 0.05f);
            Assert.AreEqual(6.2f * 0.7f, slow, 0.05f);
        }
    }
}
