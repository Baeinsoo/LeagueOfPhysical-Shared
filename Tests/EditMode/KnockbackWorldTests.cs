using System.Linq;
using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class KnockbackWorldTests
    {
        const float Dt = 0.02f;

        static readonly KnockbackConfig Config = new KnockbackConfig(120f, 6.2f, 14f, 17f, 1.5f, 0f, 5f, 11f, 30f, 18f, 0.3f, 0.55f, 3, 0.95f, 0.26f, 1.2f, 1, 6f, 0.5f, 0.5f, 0.3f, 0.1f, 0.2f, 0.8f, 5f, new System.Collections.Generic.List<MountedArcheryZoneStageConfig>());

        static Entity NewBody(string id, float x, float z)
        {
            var body = new Entity(id);
            body.Add(new GameFramework.World.Transform { Position = new System.Numerics.Vector3(x, 0f, z) });
            body.Add(new GameFramework.World.Velocity());
            body.Add(new CapsuleShape(BodySizes.CharacterRadius, BodySizes.CharacterHeight));
            body.Add(new GroundState { IsGrounded = true });
            body.Add(new InputBuffer());
            body.Add(new Simulated());
            body.Add(new KnockbackGun(Config.AmmoMax));
            body.Add(new KnockbackBody());
            body.Add(new EntityKind(EntityType.Character));
            return body;
        }

        static (KnockbackWorld world, Entity body) Make(float z)
        {
            var body = NewBody("body-1", 0f, z);
            return (MakeWorld(body), body);
        }

        static KnockbackWorld MakeWorld(params Entity[] bodies)
        {
            var registry = new EntityRegistry();
            foreach (var b in bodies) { registry.Add(b); }

            var floor = Config.CreateFloor();
            return new KnockbackWorld(registry, new WorldEventBuffer(),
                Config.CreateMoveSystem(floor, new FlappyWorldFixture.NeverHit(), ~0),
                new FlappyWorldFixture.NoopMotionBridge(), floor,
                new KnockbackFireSystem(Config),
                new BodyCollisionSystem(BodySizes.CharacterRadius, BodySizes.CharacterHeight, Config.BodyRestitution, new Vector3(1, 0, 1)),
                Config)
            { GameplayStartTick = 0 };
        }

        static void Step(KnockbackWorld world, Entity body, long t, float h, float v)
        {
            body.Get<InputBuffer>().Current = new InputCommand { Horizontal = h, Vertical = v };
            world.Tick(t, Dt);
        }

        [Test]
        public void 걸으면_움직인다()
        {
            var (world, body) = Make(0f);
            for (long t = 1; t <= 50; t++) { Step(world, body, t, 0f, 1f); }
            Assert.Greater(body.Get<GameFramework.World.Transform>().Position.Z, 0f);
        }

        [Test]
        public void 가장자리_밖에서_떨어진다()
        {
            //  16.5는 이미 칸 바깥(가장자리 끝)이라 15.9에서 시작해 걸어 나간다
            var (world, body) = Make(15.9f);
            for (long t = 1; t <= 60; t++) { Step(world, body, t, 0f, 1f); }
            Assert.Less(body.Get<GameFramework.World.Transform>().Position.Y, -1f);
        }

        [Test]
        public void 되감아_다시_돌리면_같은_자리()
        {
            var (world, body) = Make(15.9f);
            var ground = body.Get<GroundState>();
            long fallTick = 0;
            for (long t = 1; t <= 60 && fallTick == 0; t++)
            {
                Step(world, body, t, 0f, 1f);
                if (ground.IsGrounded == false) { fallTick = t; }
                else { world.SaveState(t); }
            }
            Assert.Greater(fallTick, 1, "떨어지기 시작해야 한다");
            long saveTick = fallTick - 1;   // 떨어지기 직전, 아직 접지 상태

            void Run()
            {
                for (long t = saveTick + 1; t <= saveTick + 40; t++) { Step(world, body, t, 0f, 1f); }
            }
            world.LoadState(saveTick);   // 떨어지기 시작한 틱까지 이미 진행했으니 저장 지점에서 다시 시작
            Run();
            var pos1 = body.Get<GameFramework.World.Transform>().Position;
            var vel1 = body.Get<GameFramework.World.Velocity>().Linear;
            bool grounded1 = ground.IsGrounded;

            world.LoadState(saveTick);
            Assert.IsTrue(ground.IsGrounded, "저장 시점은 접지 상태");
            Run();

            var pos2 = body.Get<GameFramework.World.Transform>().Position;
            var vel2 = body.Get<GameFramework.World.Velocity>().Linear;
            Assert.AreEqual(pos1.X, pos2.X, 1e-5f);
            Assert.AreEqual(pos1.Y, pos2.Y, 1e-5f);
            Assert.AreEqual(pos1.Z, pos2.Z, 1e-5f);
            Assert.AreEqual(vel1.Y, vel2.Y, 1e-5f);
            Assert.AreEqual(vel1.Z, vel2.Z, 1e-5f);
            Assert.AreEqual(grounded1, ground.IsGrounded);
            Assert.Less(pos2.Y, -1f, "재생에서도 떨어졌어야 한다");
        }

        [Test]
        public void 발사하면_목록과_사건에_생긴다()
        {
            var (world, body) = Make(0f);
            body.Get<InputBuffer>().Current = new InputCommand { Fire = true, AimYaw = 90f };
            world.Tick(1, Dt);

            Assert.AreEqual(1, world.Shots.Shots.Count);
            Assert.AreEqual("body-1", world.Shots.Shots[0].ShooterId);
            Assert.AreEqual(1, world.Shots.Shots[0].FireTick);
            Assert.AreEqual(2, body.Get<KnockbackGun>().Ammo);
            Assert.IsTrue(world.EventBuffer.Snapshot.OfType<KnockbackShotFiredEvent>().Any(), "발사 사건이 나와야 한다");
        }

        [Test]
        public void 날아간_몸이_선_몸을_친다()
        {
            var a = NewBody("a", -0.75f, 0f);
            var b = NewBody("b", 0f, 0f);
            var world = MakeWorld(a, b);
            a.Get<GameFramework.World.Velocity>().Linear = new System.Numerics.Vector3(10f, 0f, 0f);

            world.Tick(1, Dt);

            float bx = b.Get<GameFramework.World.Velocity>().Linear.X;
            float ax = a.Get<GameFramework.World.Velocity>().Linear.X;
            Assert.Greater(bx, 0.1f, "선 몸이 밀려야 한다");
            Assert.LessOrEqual(ax + bx, 10f + 1e-3f, "부딪혀서 운동량이 늘면 안 된다");
        }

        [Test]
        public void 총과_날림과_탄이_되감아_다시_돌려도_같다()
        {
            var (world, body) = Make(0f);
            var gun = body.Get<KnockbackGun>();
            var knock = body.Get<KnockbackBody>();
            const long SaveTick = 10;

            void Step(long t)
            {
                body.Get<InputBuffer>().Current = new InputCommand { Fire = true, AimYaw = 90f, Horizontal = 1f };
                //  15틱에 맞은 것으로 친다 — 날림 끝 틱이 되감기 상태에 끼게 한다.
                if (t == 15) { knock.KnockbackEndTick = 40; knock.DamagePercent = 12; }
                world.Tick(t, Dt);
                world.SaveState(t);
            }

            for (long t = 1; t <= 30; t++) { Step(t); }
            var shots1 = world.Shots.Capture();
            long end1 = knock.KnockbackEndTick;
            int dmg1 = knock.DamagePercent;
            int ammo1 = gun.Ammo;
            var pos1 = body.Get<GameFramework.World.Transform>().Position;
            Assert.Greater(shots1.Count, 1, "여러 발이 나가야 시험이 의미 있다");

            Assert.IsTrue(world.LoadState(SaveTick));
            Assert.AreEqual(long.MinValue, knock.KnockbackEndTick, "저장 때는 아직 안 맞았다");
            Assert.AreEqual(0, knock.DamagePercent);
            Assert.IsTrue(world.TryGetSaved(SaveTick, "body-1", out var savedBody));
            Assert.AreEqual(gun.Ammo, savedBody.Ammo);
            Assert.AreNotEqual(ammo1, gun.Ammo, "저장 때 탄 수는 끝 탄 수와 달라야 시험이 의미 있다");
            Assert.Less(world.Shots.Shots.Count, shots1.Count, "탄 목록도 저장 때로 돌아와야 한다");

            for (long t = SaveTick + 1; t <= 30; t++) { Step(t); }
            var shots2 = world.Shots.Capture();
            Assert.AreEqual(shots1.Count, shots2.Count);
            for (int i = 0; i < shots1.Count; i++)
            {
                Assert.AreEqual(shots1[i].FireTick, shots2[i].FireTick);
            }
            Assert.AreEqual(end1, knock.KnockbackEndTick);
            Assert.AreEqual(dmg1, knock.DamagePercent);
            Assert.AreEqual(ammo1, gun.Ammo);
            Assert.AreEqual(pos1.X, body.Get<GameFramework.World.Transform>().Position.X, 1e-5f);
        }

        [Test]
        public void 되감아도_남의_탄은_남는다()
        {
            var (world, body) = Make(0f);
            world.Tick(1, Dt);
            world.SaveState(1);

            var remote = new Entity("remote");
            remote.Add(new GameFramework.World.Transform());
            world.EntityRegistry.Add(remote);
            Assert.IsTrue(world.IngestRemoteShot(new KnockbackShot("remote", 2, Vector2.zero, Vector2.right)));

            Assert.IsTrue(world.LoadState(1));
            Assert.AreEqual(1, world.Shots.Shots.Count, "남의 탄은 되감아도 남아야 한다");
            Assert.AreEqual("remote", world.Shots.Shots[0].ShooterId);
        }

        [Test]
        public void 맞아서_지운_탄은_되감아도_안_살아난다()
        {
            var (world, body) = Make(0f);
            body.Get<InputBuffer>().Current = new InputCommand { Fire = true, AimYaw = 90f };
            world.Tick(1, Dt);
            world.SaveState(1);
            Assert.AreEqual(1, world.Shots.Shots.Count);

            Assert.IsTrue(world.RemoveShot("body-1", 1));
            Assert.IsTrue(world.LoadState(1));
            Assert.AreEqual(0, world.Shots.Shots.Count, "지운 탄이 되살아나면 안 된다");
        }

        [Test]
        public void 되감아_다시_돌려도_서버가_지운_내_탄은_다시_안_생긴다()
        {
            var (world, body) = Make(0f);
            world.SaveState(0);
            body.Get<InputBuffer>().Current = new InputCommand { Fire = true, AimYaw = 90f };
            world.Tick(1, Dt);
            Assert.AreEqual(1, world.Shots.Shots.Count, "쏘아야 시험이 의미 있다");

            //  서버가 이 탄이 맞았다고 지웠다 — 그 뒤 발사 틱보다 앞선 앵커로 되감아 같은 입력을 다시 돌린다.
            Assert.IsTrue(world.RemoveShot("body-1", 1));
            Assert.IsTrue(world.LoadState(0));
            body.Get<InputBuffer>().Current = new InputCommand { Fire = true, AimYaw = 90f };
            world.Tick(1, Dt);

            Assert.AreEqual(0, world.Shots.Shots.Count, "재생이 지운 탄을 다시 쏘면 안 된다");
            Assert.AreEqual(Config.AmmoMax - 1, body.Get<KnockbackGun>().Ammo, "탄 수는 쏜 것으로 줄어야 한다(서버도 쐈다)");
        }

        //  사거리 13 m, 탄 17 m/s — 틱당 0.34 m라 13 m를 넘는 틱의 선분(12.92 → 13.26)에 사거리 끝이 걸린다.
        static readonly KnockbackConfig RangeConfig = new KnockbackConfig(120f, 6.2f, 14f, 17f, 1.5f, 0f, 5f, 11f,
            17f, 13f, 0.28f, 0.6f, 3, 0.95f, 0.26f, 1.2f, 1, 6f, 0.5f, 0.5f, 0.3f, 0.1f, 0.2f, 0.8f, 5f,
            new System.Collections.Generic.List<MountedArcheryZoneStageConfig>());

        static bool HitsWithinRange(float touchDistance)
        {
            var registry = new EntityRegistry();
            var floor = RangeConfig.CreateFloor();
            var world = new KnockbackWorld(registry, new WorldEventBuffer(),
                RangeConfig.CreateMoveSystem(floor, new FlappyWorldFixture.NeverHit(), ~0),
                new FlappyWorldFixture.NoopMotionBridge(), floor, new KnockbackFireSystem(RangeConfig),
                new BodyCollisionSystem(BodySizes.CharacterRadius, BodySizes.CharacterHeight, RangeConfig.BodyRestitution, new Vector3(1, 0, 1)),
                RangeConfig) { GameplayStartTick = 0 };
            world.IngestRemoteShot(new KnockbackShot("remote", 0, Vector2.zero, Vector2.right));

            float reach = RangeConfig.HitRadius + RangeConfig.ShotRadius;
            var targets = new System.Collections.Generic.List<(string, Vector2)> { ("v", new Vector2(touchDistance + reach, 0f)) };
            //  서버 차례 그대로: 월드 틱(다 쓴 탄 정리 포함) 뒤에 End의 맞음 판정.
            for (long t = 1; t <= 60; t++)
            {
                world.Tick(t, Dt);
                if (KnockbackHits.Find(world.Shots.Shots, targets, t, RangeConfig, Dt).Count > 0) return true;
            }
            return false;
        }

        [Test]
        public void 사거리_끝_바로_앞에_선_몸도_맞는다()
        {
            //  마지막으로 판정되던 자리(38틱, 12.92 m)와 사거리 끝(13 m) 사이에 닿는 몸.
            Assert.IsTrue(HitsWithinRange(12.96f), "사거리 끝까지 판정해야 한다");
        }

        [Test]
        public void 사거리_밖에_선_몸은_안_맞는다()
        {
            Assert.IsFalse(HitsWithinRange(13.1f), "사거리를 넘은 몫은 판정하지 않는다");
            Assert.IsTrue(HitsWithinRange(12.5f), "양성 대조 — 사거리 안이면 맞는다");
        }

        [Test]
        public void 떨어지는_몸은_부딪히지_않는다()
        {
            var a = NewBody("a", -0.75f, 0f);
            var b = NewBody("b", 0f, 0f);
            a.Get<GameFramework.World.Transform>().Position = new System.Numerics.Vector3(-0.75f, -1f, 0f);
            a.Get<GroundState>().IsGrounded = false;
            var world = MakeWorld(a, b);
            a.Get<GameFramework.World.Velocity>().Linear = new System.Numerics.Vector3(10f, 0f, 0f);

            world.Tick(1, Dt);

            Assert.AreEqual(0f, b.Get<GameFramework.World.Velocity>().Linear.X, 1e-4f, "떨어지는 몸과는 부딪히지 않는다");
        }

        [Test]
        public void 출발_전에는_입력을_줘도_안_움직인다()
        {
            var (world, body) = Make(0f);
            world.GameplayStartTick = 100;
            for (long t = 1; t <= 50; t++) { Step(world, body, t, 1f, 0f); }
            var before = body.Get<GameFramework.World.Transform>().Position;
            Assert.AreEqual(0f, before.X, 1e-6f);
            Assert.AreEqual(0f, body.Get<GameFramework.World.Velocity>().Linear.X, 1e-6f);

            for (long t = 100; t <= 150; t++) { Step(world, body, t, 1f, 0f); }
            Assert.Greater(body.Get<GameFramework.World.Transform>().Position.X, 0f, "출발 뒤에는 움직여야 한다");
        }

        [Test]
        public void 설정이_바닥을_만든다()
        {
            Assert.IsTrue(Config.CreateFloor().IsSolid(0f, 0f, 0));
            Assert.AreEqual(6000, Config.MatchDurationTicks(0.02f));
        }
    }
}
