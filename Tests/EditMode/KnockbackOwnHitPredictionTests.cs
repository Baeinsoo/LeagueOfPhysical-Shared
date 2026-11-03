using System.Collections.Generic;
using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 클라 월드가 내 몸(예측하는 몸)이 남의 탄에 맞는 것을 서버와 같은 식으로 미리 넣는지(§3.3.2).
    /// 서버 KnockbackHitSystem과의 나란한 비교는 서버 레포 시험(KnockbackOwnHitParityTests)이 한다.
    /// </summary>
    public class KnockbackOwnHitPredictionTests
    {
        const float Dt = 0.02f;
        const long FireTick = 10;

        //  시작값(스펙 §3): 탄 17 m/s·사거리 13 m·반지름 0.28, 몸 0.6, 맞을 때 +12, 날림 7 + 피해×0.12, 날림 0.18 + 피해×0.006초.
        static readonly KnockbackConfig C = new KnockbackConfig(120f, 6.2f, 14f, 17f, 1.5f, 0f, 5f, 11f,
            17f, 13f, 0.28f, 0.6f, 3, 0.95f, 0.26f, 1.2f, 12, 7f, 0.12f, 0.2f, 0.18f, 0.006f, 1.6f, 0.8f, 5f,
            new List<MountedArcheryZoneStageConfig>());

        readonly Dictionary<string, int> teams = new Dictionary<string, int>();
        KnockbackPillarColliders pillarColliders;

        [TearDown]
        public void DisposePillarColliders()
        {
            pillarColliders?.Dispose();
            pillarColliders = null;
        }

        static Entity Body(string id, float x, float z, bool simulated)
        {
            var body = new Entity(id);
            body.Add(new GameFramework.World.Transform { Position = new System.Numerics.Vector3(x, 0f, z) });
            body.Add(new GameFramework.World.Velocity());
            body.Add(new CapsuleShape(BodySizes.CharacterRadius, BodySizes.CharacterHeight));
            body.Add(new GroundState { IsGrounded = true });
            body.Add(new InputBuffer());
            if (simulated)
            {
                body.Add(new Simulated());
            }
            body.Add(new KnockbackGun(C.AmmoMax));
            body.Add(new KnockbackBody());
            body.Add(new EntityKind(EntityType.Character));
            return body;
        }

        KnockbackWorld World(bool predict, KnockbackPillars pillars, params Entity[] bodies)
        {
            var registry = new EntityRegistry();
            foreach (var b in bodies) { registry.Add(b); }
            var floor = C.CreateFloor();
            if (pillars != null)
            {
                pillarColliders = new KnockbackPillarColliders(pillars);
            }
            return new KnockbackWorld(registry, new WorldEventBuffer(),
                C.CreateMoveSystem(floor, new FlappyWorldFixture.NeverHit(), ~0),
                new FlappyWorldFixture.NoopMotionBridge(), floor,
                new KnockbackFireSystem(C),
                new BodyCollisionSystem(BodySizes.CharacterRadius, BodySizes.CharacterHeight, C.BodyRestitution, new Vector3(1, 0, 1)),
                C, pillars, pillars != null ? pillarColliders : null,
                predict ? new KnockbackOwnHitPrediction(id => teams.TryGetValue(id, out var t) ? t : 0) : null)
            { GameplayStartTick = 0 };
        }

        //  내 몸 "me"(5, 0)과 남 "foe"(원점, 시뮬 안 함). foe가 +x로 쏜다.
        (KnockbackWorld world, Entity me, Entity foe) ClientWorld(bool predict = true, float meX = 5f, float meZ = 0f)
        {
            var me = Body("me", meX, meZ, simulated: true);
            var foe = Body("foe", 0f, 0f, simulated: false);
            return (World(predict, null, me, foe), me, foe);
        }

        static KnockbackShot Shot(bool bomb = false) => new KnockbackShot("foe", FireTick, Vector2.zero, Vector2.right, bomb);

        static void Run(KnockbackWorld world, long from, long to)
        {
            for (long t = from; t <= to; t++)
            {
                world.Tick(t, Dt);
                world.SaveState(t);
            }
        }

        //  (5, 0)에 가만히 선 몸을 원점 탄이 처음 맞히는 틱 — 판정 식을 그대로 대어 따로 계산한다.
        static long ExpectedHitTick(Vector2 at)
        {
            var targets = new List<(string, Vector2)> { ("me", at) };
            var shots = new List<KnockbackShot> { Shot() };
            for (long t = FireTick; t < FireTick + 60; t++)
            {
                if (KnockbackHits.Find(shots, targets, new List<Vector2>(), 0f, t, C, Dt).Count > 0) return t;
            }
            return -1;
        }

        static long FirstFlyingTick(KnockbackWorld world, Entity me, long from, long to)
        {
            for (long t = from; t <= to; t++)
            {
                world.Tick(t, Dt);
                world.SaveState(t);
                if (me.Get<KnockbackBody>().KnockbackEndTick != long.MinValue) return t;
            }
            return -1;
        }

        [Test]
        public void 남의_탄이_내_몸에_오면_서버와_같은_틱에_맞음을_넣는다()
        {
            var (world, me, _) = ClientWorld();
            world.IngestRemoteShot(Shot());
            long expected = ExpectedHitTick(new Vector2(5f, 0f));
            Assert.Greater(expected, FireTick);

            long hit = FirstFlyingTick(world, me, 1, 60);

            Assert.AreEqual(expected, hit, "서버 판정과 같은 틱에 맞아야 한다");
            var body = me.Get<KnockbackBody>();
            Assert.AreEqual(12, body.DamagePercent);
            Assert.AreEqual(hit + C.Ticks(C.StunBase + 12 * C.StunScale), body.KnockbackEndTick);
            Assert.AreEqual(7f + 12 * 0.12f, me.Get<GameFramework.World.Velocity>().Linear.X, 1e-4f, "맞은 틱에 탄 방향으로 날림");
            Assert.AreEqual(0, world.Shots.Shots.Count, "맞힌 탄은 장부에서 사라진다");
        }

        [Test]
        public void 같은_팀_탄은_지나간다()
        {
            teams["me"] = 1;
            teams["foe"] = 1;
            var (world, me, _) = ClientWorld();
            world.IngestRemoteShot(Shot());
            Run(world, 1, 60);
            Assert.AreEqual(0, me.Get<KnockbackBody>().DamagePercent, "같은 팀 탄은 안 맞는다");

            //  양성 대조 — 다른 팀이면 맞는다.
            teams["foe"] = 2;
            var (world2, me2, _) = ClientWorld();
            world2.IngestRemoteShot(Shot());
            Run(world2, 1, 60);
            Assert.AreEqual(12, me2.Get<KnockbackBody>().DamagePercent);
        }

        [Test]
        public void 떨어지는_몸은_표적이_아니다()
        {
            var (world, me, _) = ClientWorld();
            me.Get<GameFramework.World.Transform>().Position = new System.Numerics.Vector3(5f, -1f, 0f);
            me.Get<GroundState>().IsGrounded = false;
            world.IngestRemoteShot(Shot());
            Run(world, 1, 60);
            Assert.AreEqual(0, me.Get<KnockbackBody>().DamagePercent);
        }

        [Test]
        public void 날아가는_몸도_서버처럼_표적이다()
        {
            //  서버 KnockbackHitSystem은 서 있는 몸(바닥 위)이면 날아가는 중이어도 맞힌다 — 같아야 한다.
            var (world, me, _) = ClientWorld();
            me.Get<KnockbackBody>().KnockbackEndTick = 10_000;
            world.IngestRemoteShot(Shot());
            Run(world, 1, 60);
            Assert.AreEqual(12, me.Get<KnockbackBody>().DamagePercent);
        }

        [Test]
        public void 서_있는_기둥에_먼저_닿으면_안_맞는다()
        {
            var floor = C.CreateFloor();
            var pillars = new KnockbackPillars(new KnockbackPillarLayout(1, 3f, 0.9f, 3f), floor);
            var dir = pillars.Center(0).normalized;
            var behind = dir * 5f;
            var me = Body("me", behind.x, behind.y, simulated: true);
            var foe = Body("foe", 0f, 0f, simulated: false);
            var world = World(true, pillars, me, foe);
            world.IngestRemoteShot(new KnockbackShot("foe", FireTick, Vector2.zero, dir));
            Run(world, 1, 60);
            Assert.AreEqual(0, me.Get<KnockbackBody>().DamagePercent, "기둥 뒤에 숨으면 안 맞는다");

            //  양성 대조 — 기둥이 없으면 같은 자리에서 맞는다.
            DisposePillarColliders();
            var me2 = Body("me", behind.x, behind.y, simulated: true);
            var world2 = World(true, null, me2, Body("foe", 0f, 0f, simulated: false));
            world2.IngestRemoteShot(new KnockbackShot("foe", FireTick, Vector2.zero, dir));
            Run(world2, 1, 60);
            Assert.AreEqual(12, me2.Get<KnockbackBody>().DamagePercent);
        }

        [Test]
        public void 남의_몸은_예측하지_않는다()
        {
            //  탄 길에 남의 몸만 있다 — 남은 서버 결과(스냅샷)로만 바뀐다.
            var me = Body("me", 0f, 8f, simulated: true);
            var shooter = Body("foe", 0f, 0f, simulated: false);
            var other = Body("other", 5f, 0f, simulated: false);
            var world = World(true, null, me, shooter, other);
            world.IngestRemoteShot(Shot());
            long passTick = ExpectedHitTick(new Vector2(5f, 0f));

            Run(world, 1, passTick + 1);

            Assert.AreEqual(0, other.Get<KnockbackBody>().DamagePercent);
            Assert.AreEqual(long.MinValue, other.Get<KnockbackBody>().KnockbackEndTick);
            Assert.AreEqual(0f, other.Get<GameFramework.World.Velocity>().Linear.X, 1e-6f);
            Assert.AreEqual(1, world.Shots.Shots.Count, "남의 몸을 지나도 탄은 남는다(서버 맞음 사건이 지운다)");
        }

        [Test]
        public void 늦게_온_탄은_맞은_틱으로_되감으면_처음부터_안_것과_같다()
        {
            long expected = ExpectedHitTick(new Vector2(5f, 0f));
            long now = expected + 8;

            //  처음부터 안 월드(라이브).
            var (live, liveMe, _) = ClientWorld();
            live.IngestRemoteShot(Shot());
            Run(live, 1, now);

            //  맞은 틱이 이미 지난 뒤에 탄 사실을 안 월드.
            var (late, lateMe, _) = ClientWorld();
            Run(late, 1, now);
            Assert.AreEqual(0, lateMe.Get<KnockbackBody>().DamagePercent, "아직 모르면 안 맞았다");
            Assert.IsTrue(late.IngestRemoteShot(Shot(), out long? replayFrom));
            Assert.AreEqual(expected - 1, replayFrom, "맞은 틱 바로 앞 상태로 되감으라고 알려 준다");

            //  기존 되감기 그대로: 그 틱 상태를 불러 지금까지 다시 돌린다.
            Assert.IsTrue(late.LoadState(replayFrom.Value));
            Run(late, replayFrom.Value + 1, now);

            var a = liveMe.Get<KnockbackBody>();
            var b = lateMe.Get<KnockbackBody>();
            Assert.AreEqual(12, b.DamagePercent);
            Assert.AreEqual(a.DamagePercent, b.DamagePercent);
            Assert.AreEqual(a.KnockbackEndTick, b.KnockbackEndTick);
            Assert.AreEqual(liveMe.Get<GameFramework.World.Transform>().Position, lateMe.Get<GameFramework.World.Transform>().Position);
            Assert.AreEqual(liveMe.Get<GameFramework.World.Velocity>().Linear, lateMe.Get<GameFramework.World.Velocity>().Linear);
            Assert.AreEqual(0, late.Shots.Shots.Count);
        }

        [Test]
        public void 맞은_틱이_저장_범위_밖이면_되감지_않고_예측하지_않는다()
        {
            long expected = ExpectedHitTick(new Vector2(5f, 0f));
            long now = expected + 8;
            var (world, me, _) = ClientWorld();
            //  맞은 틱 바로 앞은 저장하지 않았다(그보다 뒤부터만 저장).
            for (long t = 1; t <= now; t++)
            {
                world.Tick(t, Dt);
                if (t >= expected) world.SaveState(t);
            }

            Assert.IsTrue(world.IngestRemoteShot(Shot(), out long? replayFrom));
            Assert.IsNull(replayFrom, "되감을 상태가 없으면 되감지 않는다");
            Run(world, now + 1, now + 40);
            Assert.AreEqual(0, me.Get<KnockbackBody>().DamagePercent, "서버 결과를 기다린다");

            //  양성 대조 — 앞 틱이 저장돼 있으면 되감으라고 한다.
            var (world2, _, _) = ClientWorld();
            Run(world2, 1, now);
            Assert.IsTrue(world2.IngestRemoteShot(Shot(), out long? replayFrom2));
            Assert.AreEqual(expected - 1, replayFrom2);
        }

        [Test]
        public void 맞은_틱보다_앞으로_되감아_다시_돌려도_같은_결과()
        {
            long expected = ExpectedHitTick(new Vector2(5f, 0f));
            long now = expected + 8;
            var (world, me, _) = ClientWorld();
            world.IngestRemoteShot(Shot());
            Run(world, 1, now);
            var body = me.Get<KnockbackBody>();
            long end = body.KnockbackEndTick;
            var pos = me.Get<GameFramework.World.Transform>().Position;
            Assert.AreEqual(12, body.DamagePercent);

            //  서버 스냅샷 되감기처럼 맞기 전 틱으로 돌아가 다시 계산한다.
            Assert.IsTrue(world.LoadState(expected - 3));
            Assert.AreEqual(0, body.DamagePercent);
            Run(world, expected - 2, now);

            Assert.AreEqual(12, body.DamagePercent, "탄이 장부로 돌아와 다시 한 번만 맞는다");
            Assert.AreEqual(end, body.KnockbackEndTick);
            Assert.AreEqual(pos, me.Get<GameFramework.World.Transform>().Position);
        }

        [Test]
        public void 맞은_틱보다_뒤로_되감으면_탄이_돌아오지_않는다()
        {
            long expected = ExpectedHitTick(new Vector2(5f, 0f));
            long now = expected + 8;
            var (world, me, _) = ClientWorld();
            world.IngestRemoteShot(Shot());
            Run(world, 1, now);

            Assert.IsTrue(world.LoadState(expected + 1));
            Run(world, expected + 2, now);
            Assert.AreEqual(12, me.Get<KnockbackBody>().DamagePercent, "이미 맞은 뒤라 다시 맞지 않는다");
            Assert.AreEqual(0, world.Shots.Shots.Count);
        }

        [Test]
        public void 맞은_탄이_또_와도_두_번_맞지_않는다()
        {
            var (world, me, _) = ClientWorld();
            world.IngestRemoteShot(Shot());
            Run(world, 1, 40);
            Assert.AreEqual(12, me.Get<KnockbackBody>().DamagePercent);

            Assert.IsFalse(world.IngestRemoteShot(Shot()), "재전송된 같은 탄은 받지 않는다");
            Run(world, 41, 80);
            Assert.AreEqual(12, me.Get<KnockbackBody>().DamagePercent);
        }

        [Test]
        public void 폭탄도_내_몸에_직접_맞으면_예측한다()
        {
            var (world, me, _) = ClientWorld(meZ: 0.4f);
            world.IngestRemoteShot(Shot(bomb: true));
            Assert.Greater(FirstFlyingTick(world, me, 1, 60), FireTick);
            Assert.AreEqual(12, me.Get<KnockbackBody>().DamagePercent);
            //  맞은 틱의 속도 — 날림이 끝나면 줄어든다.
            var v = me.Get<GameFramework.World.Velocity>().Linear;
            Assert.Greater(v.Z, 0.1f, "폭탄은 터진 자리에서 몸 쪽으로 민다(탄 방향만이 아니다)");
        }

        [Test]
        public void 예측하지_않는_월드는_맞음을_넣지_않는다()
        {
            //  서버 월드 — 모든 몸이 시뮬 대상이지만 맞음은 서버 KnockbackHitSystem만 넣는다.
            var me = Body("me", 5f, 0f, simulated: true);
            var foe = Body("foe", 0f, 0f, simulated: true);
            var world = World(false, null, me, foe);
            world.Shots.Add(Shot());
            Run(world, 1, 60);
            Assert.AreEqual(0, me.Get<KnockbackBody>().DamagePercent);
        }
    }
}
