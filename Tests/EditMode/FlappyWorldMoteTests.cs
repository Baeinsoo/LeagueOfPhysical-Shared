using GameFramework.Physics;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 빛 알갱이가 <b>월드에 실제로 물려 있는지</b>. 판정 자체는 <see cref="FlappyMoteFieldTests"/>가 본다 —
    /// 필드를 만들어 놓고 틱에서 안 물으면 아무 일도 안 일어나는데 판정 테스트는 전부 초록이 된다.
    /// </summary>
    public class FlappyWorldMoteTests
    {
        //  픽스처 새: 초속 11 m, 틱 0.02초 → 한 틱에 0.22 m 간다. 출발 높이 0.
        const float Dt = 0.02f;

        static FlappyMapRulesField Rules(bool moteCharge = true)
        {
            var rules = new FlappyMapRulesField();
            rules.Set(chaser: false, manualDash: true, moteCharge: moteCharge);
            return rules;
        }

        static FlappyMoteField Motes(params float[] xs)
        {
            var field = new FlappyMoteField();
            field.Add(new FlappyMoteSet(xs, new float[xs.Length], big: false));
            return field;
        }

        static FlappyWorld World(EntityRegistry registry, FlappyMoteField motes, FlappyMapRulesField rules,
                                 ICollisionQuery collision = null)
        {
            var config = FlappyWorldFixture.Config();
            var world = new FlappyWorld(registry, new WorldEventBuffer(),
                new FlappyMoveSystem(config),
                new FlappyStunSystem(config),
                new FlappyDashSystem(config, rules),
                new FinishSystem(new FinishLineBounds(FinishAxis.X), FinishAxis.X, increasing: true),
                new FlappyWindmillField(), new FlappyBoostPadField(),
                collision ?? new FlappyWorldFixture.NeverHit(), new FlappyWorldFixture.NoopMotionBridge(),
                layerMask: ~0, moteField: motes);
            world.GameplayStartTick = 0;
            return world;
        }

        static Entity Bird(EntityRegistry registry, float charge = 0f)
        {
            var bird = FlappyWorldFixture.Bird("bird-1");
            bird.Get<FlappyDash>().Charge = charge;
            registry.Add(bird);
            return bird;
        }

        [Test]
        public void 지나간_알갱이만큼_게이지가_찬다()
        {
            var registry = new EntityRegistry();
            var bird = Bird(registry);
            var world = World(registry, Motes(0.05f, 0.1f, 0.15f), Rules());

            world.Tick(1, Dt);

            Assert.AreEqual(3 * FlappyMapRulesField.DefaultMoteValue, bird.Get<FlappyDash>().Charge, 1e-6f);
        }

        [Test]
        public void 같은_알갱이는_다음_틱에_다시_안_먹는다()
        {
            var registry = new EntityRegistry();
            var bird = Bird(registry);
            var world = World(registry, Motes(0.1f), Rules());

            for (long tick = 1; tick <= 5; tick++)
            {
                world.Tick(tick, Dt);
            }

            Assert.AreEqual(FlappyMapRulesField.DefaultMoteValue, bird.Get<FlappyDash>().Charge, 1e-6f);
        }

        [Test]
        public void 높이가_멀면_안_먹는다()
        {
            var registry = new EntityRegistry();
            var bird = Bird(registry);
            var far = new FlappyMoteField();
            far.Add(new FlappyMoteSet(new[] { 0.1f }, new[] { 2f }, big: false));   // 0.45 + 0.5 = 0.95보다 멀다
            var world = World(registry, far, Rules());

            world.Tick(1, Dt);

            Assert.AreEqual(0f, bird.Get<FlappyDash>().Charge, 1e-6f);
        }

        [Test]
        public void 높이는_발밑이_아니라_몸_중심에서_잰다()
        {
            //  발밑 0 → 몸 중심 0.45. 알갱이 높이 1.35는 중심에서 0.9(닿는 거리 0.95 안), 발밑에서 1.35(밖)다.
            var registry = new EntityRegistry();
            var bird = Bird(registry);
            var above = new FlappyMoteField();
            above.Add(new FlappyMoteSet(new[] { 0.1f }, new[] { 1.35f }, big: false));
            var world = World(registry, above, Rules());

            world.Tick(1, Dt);

            Assert.AreEqual(FlappyMapRulesField.DefaultMoteValue, bird.Get<FlappyDash>().Charge, 1e-6f);
        }

        [Test]
        public void 알갱이_충전이_꺼진_맵에선_안_먹는다()
        {
            //  같은 새를 알갱이가 있는 판과 없는 판에 놓고 비교한다 — 꺼진 맵은 예전처럼 다이브(기본 충전)로
            //  차므로 0과 비교하면 안 되고, "알갱이가 있어도 결과가 똑같다"를 봐야 한다.
            var withRegistry = new EntityRegistry();
            var withMotes = Bird(withRegistry);
            World(withRegistry, Motes(0.1f), Rules(moteCharge: false)).Tick(1, Dt);

            var withoutRegistry = new EntityRegistry();
            var withoutMotes = Bird(withoutRegistry);
            World(withoutRegistry, new FlappyMoteField(), Rules(moteCharge: false)).Tick(1, Dt);

            Assert.AreEqual(withoutMotes.Get<FlappyDash>().Charge, withMotes.Get<FlappyDash>().Charge, 0f);
        }

        [Test]
        public void 얼어_있는_새는_안_먹는다()
        {
            var registry = new EntityRegistry();
            var bird = Bird(registry);
            bird.Get<FlappyStun>().StunRemaining = 0.8f;
            var world = World(registry, Motes(0.1f), Rules());

            world.Tick(1, Dt);

            Assert.AreEqual(0f, bird.Get<FlappyDash>().Charge, 1e-6f);
        }

        [Test]
        public void 상한_두_칸에서_멈춘다()
        {
            var registry = new EntityRegistry();
            var bird = Bird(registry, charge: FlappyDash.MaxCharge - 0.01f);
            var world = World(registry, Motes(0.05f, 0.1f, 0.15f), Rules());

            world.Tick(1, Dt);

            Assert.AreEqual(FlappyDash.MaxCharge, bird.Get<FlappyDash>().Charge, 1e-6f);
        }

        [Test]
        public void 되감아_다시_돌려도_한_번만_먹는다()
        {
            //  알갱이는 두 번째 틱 구간(0.22, 0.44]에 있다.
            var registry = new EntityRegistry();
            var bird = Bird(registry);
            var world = World(registry, Motes(0.3f), Rules());

            world.Tick(1, Dt);
            world.SaveState(1);
            world.Tick(2, Dt);
            float live = bird.Get<FlappyDash>().Charge;

            Assert.IsTrue(world.LoadState(1));
            world.Tick(2, Dt);

            Assert.AreEqual(FlappyMapRulesField.DefaultMoteValue, live, 1e-6f);
            Assert.AreEqual(live, bird.Get<FlappyDash>().Charge, 0f, "되감아 다시 돌린 결과가 라이브와 같아야 한다");
        }

        [Test]
        public void 알갱이_맵에선_다이브로_안_찬다()
        {
            var registry = new EntityRegistry();
            var bird = Bird(registry, charge: 0.3f);
            bird.Get<Velocity>().Linear = new System.Numerics.Vector3(0f, -30f, 0f);   // 최고 속도로 낙하
            var world = World(registry, new FlappyMoteField(), Rules());

            world.Tick(1, Dt);

            Assert.AreEqual(0.3f, bird.Get<FlappyDash>().Charge, 1e-6f);
        }

        [Test]
        public void 박아서_스턴이_걸리면_채우던_칸만_잃고_찬_칸은_지킨다()
        {
            //  10-23 사람 판: 전체 절반을 잃으니 도전적으로 붙을수록 게이지가 한 번도 안 찼다 → 모은 칸은 지킨다.
            var registry = new EntityRegistry();
            var bird = Bird(registry, charge: 1.7f);
            var world = World(registry, new FlappyMoteField(), Rules(), new FlappyWorldFixture.AlwaysHit());

            world.Tick(1, Dt);
            Assert.Greater(bird.Get<FlappyStun>().StunRemaining, 0f, "전제: 스턴이 걸렸다");
            Assert.AreEqual(1f, bird.Get<FlappyDash>().Charge, 1e-6f);

            world.Tick(2, Dt);   // 이미 스턴 중 — 또 닿아도 다시 잃지 않는다
            Assert.AreEqual(1f, bird.Get<FlappyDash>().Charge, 1e-6f);
        }

        [Test]
        public void 칸을_못_채운_채_박으면_0이_되고_두_칸이면_그대로다()
        {
            foreach (var (before, after) in new[] { (0.7f, 0f), (2f, 2f) })
            {
                var registry = new EntityRegistry();
                var bird = Bird(registry, charge: before);
                World(registry, new FlappyMoteField(), Rules(), new FlappyWorldFixture.AlwaysHit()).Tick(1, Dt);
                Assert.AreEqual(after, bird.Get<FlappyDash>().Charge, 1e-6f, $"{before}에서 박음");
            }
        }

        [Test]
        public void 무적_중에_닿으면_안_잃는다()
        {
            var registry = new EntityRegistry();
            var bird = Bird(registry, charge: 1.2f);
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;
            var world = World(registry, new FlappyMoteField(), Rules(), new FlappyWorldFixture.AlwaysHit());

            world.Tick(1, Dt);

            Assert.AreEqual(1.2f, bird.Get<FlappyDash>().Charge, 1e-6f);
        }

        [Test]
        public void 알갱이_맵이_아니면_박아도_안_잃는다()
        {
            var registry = new EntityRegistry();
            var bird = Bird(registry, charge: 1.2f);
            var world = World(registry, new FlappyMoteField(), Rules(moteCharge: false), new FlappyWorldFixture.AlwaysHit());

            world.Tick(1, Dt);

            Assert.Greater(bird.Get<FlappyStun>().StunRemaining, 0f, "전제: 스턴이 걸렸다");
            Assert.GreaterOrEqual(bird.Get<FlappyDash>().Charge, 1.2f);
        }

        [Test]
        public void 알갱이_맵은_출발_전에_게이지를_0으로_둔다()
        {
            var registry = new EntityRegistry();
            var bird = Bird(registry, charge: FlappyDash.InitialCharge);
            var world = World(registry, new FlappyMoteField(), Rules());
            world.GameplayStartTick = 100;

            world.Tick(1, Dt);

            Assert.AreEqual(0f, bird.Get<FlappyDash>().Charge);
        }

        //  움직이는 무더기(승강기 위): 진폭 5 m, 주기 2초, 위상 90° — 틱 0에서 +5 m, 1초(50틱)마다 반대.
        //  진폭을 크게 잡아 틱이 조금만 어긋나도(±12틱이면 오프셋 3.65) 손 닿는 거리(0.95)를 벗어나게 했다.
        const float MoveAmp = 5f;
        const float CenterAfterTick = 0.422f;   // 발밑 0에서 한 틱 떨어진 뒤 몸 중심 높이(대략)

        static FlappyMoteField MovingMote(float x, float baseY)
        {
            var field = new FlappyMoteField();
            field.Add(new FlappyMoteSet(new[] { x }, new[] { baseY }, big: false,
                                        moveAmplitude: MoveAmp, movePeriod: 2f, movePhase: 90f));
            return field;
        }

        [Test]
        public void 움직이는_알갱이는_그_틱의_높이로_판정한다()
        {
            //  원래 높이 = 몸 중심 - 5. 오프셋 +5(틱 100)면 몸 중심과 같은 높이 → 먹는다.
            //  오프셋을 무시하면 5 m 아래라 못 먹고, 틱이 어긋나도 못 먹는다.
            var registry = new EntityRegistry();
            var bird = Bird(registry);
            var world = World(registry, MovingMote(0.1f, CenterAfterTick - MoveAmp), Rules());
            world.Tick(100, Dt);

            Assert.AreEqual(FlappyMapRulesField.DefaultMoteValue, bird.Get<FlappyDash>().Charge, 1e-6f);
        }

        [Test]
        public void 움직이는_알갱이가_반대편에_있으면_안_먹는다()
        {
            //  원래 높이 = 몸 중심. 오프셋 -5(틱 50)면 5 m 아래 → 못 먹는다. 오프셋을 무시하면 먹어 버린다.
            var registry = new EntityRegistry();
            var bird = Bird(registry);
            var world = World(registry, MovingMote(0.1f, CenterAfterTick), Rules());
            world.Tick(50, Dt);

            Assert.AreEqual(0f, bird.Get<FlappyDash>().Charge, 1e-6f);
        }

        [Test]
        public void 무더기_마커는_케이지_자식이면_승강기_움직임을_싣고_원점에서_케이지_오프셋을_뺀다()
        {
            var liftGo = new GameObject("Lift");
            try
            {
                var lift = liftGo.AddComponent<FlappyLift>();
                lift.Amplitude = 1.8f; lift.Period = 3.5f; lift.Phase = 90f;
                var cage = new GameObject("Cage").transform;
                cage.SetParent(liftGo.transform, false);
                cage.localPosition = new Vector3(0f, 1.5f, 0f);   // 올라올 때 이미 1.5 m 올라가 있다
                lift.Cage = cage;
                var clusterGo = new GameObject("MoteCluster");
                clusterGo.transform.SetParent(cage, false);
                clusterGo.transform.position = new Vector3(10f, 2.5f, 0f);   // 원래 자리 1.0 + 케이지 1.5
                var cluster = clusterGo.AddComponent<FlappyMoteCluster>();
                cluster.Points = new[] { new Vector2(0f, -0.4f) };
                var field = new FlappyMoteField();

                cluster.Construct(field);

                var set = field.Sets[0];
                Assert.IsTrue(set.Moving);
                Assert.AreEqual(1.8f, set.MoveAmplitude);
                Assert.AreEqual(3.5f, set.MovePeriod);
                Assert.AreEqual(90f, set.MovePhase);
                Assert.AreEqual(1.0f, set.OriginY, 1e-5f, "원점은 케이지 오프셋을 뺀 원래 자리");
                Assert.AreEqual(0.6f, set.Ys[0], 1e-5f);
                Assert.AreEqual(1.5f, set.CurrentVisualOffset, 1e-5f, "그림 오프셋은 케이지의 지금 높이");
            }
            finally
            {
                Object.DestroyImmediate(liftGo);
            }
        }

        [Test]
        public void 케이지_밖_무더기는_고정이다()
        {
            var go = new GameObject("MoteCluster");
            try
            {
                go.transform.position = new Vector3(3f, 1f, 0f);
                var cluster = go.AddComponent<FlappyMoteCluster>();
                cluster.Points = new[] { new Vector2(0f, 0.2f) };
                var field = new FlappyMoteField();

                cluster.Construct(field);

                Assert.IsFalse(field.Sets[0].Moving);
                Assert.AreEqual(1.2f, field.Sets[0].Ys[0], 1e-5f);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void 알갱이_맵이_아니면_출발_게이지는_그대로다()
        {
            var registry = new EntityRegistry();
            var bird = Bird(registry, charge: FlappyDash.InitialCharge);
            var world = World(registry, new FlappyMoteField(), Rules(moteCharge: false));
            world.GameplayStartTick = 100;

            world.Tick(1, Dt);

            Assert.AreEqual(FlappyDash.InitialCharge, bird.Get<FlappyDash>().Charge);
        }
    }
}
