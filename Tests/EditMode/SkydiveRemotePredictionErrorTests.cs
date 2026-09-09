using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 남을 "하던 자세를 계속한다"로 두고 굴렸을 때 9틱(≈180ms) 뒤에 얼마나 어긋나나.
    /// 자세를 바꾼 창이 이 설계의 진짜 위험이라 두 경우를 나눠 잰다.
    ///
    /// <para>왜 이 테스트가 있나: 몸싸움 설계 전체가 "남의 자세는 유지된다고 보고 굴려도 된다"는
    /// 가정 위에 서 있다. 같은 자리에서 Flappy Race는 실패했다(날갯짓은 순간의 사건이라 예측할
    /// 방법이 없다). 스카이다이브는 자세가 지속되는 값이라 성립한다는 것이 <b>근거이지 결론이
    /// 아니라서</b> 숫자로 남긴다.</para>
    /// </summary>
    public class SkydiveRemotePredictionErrorTests
    {
        const int LeadTicks = 9;          // ≈180ms — 클라가 서버보다 앞서는 대략의 폭
        const int PostureChangeTick = 4;  // 그 창 한가운데서 자세가 바뀐다
        const float Dt = 0.02f;

        [Test]
        public void 자세를_안_바꾸면_9틱_예측이_거의_정확하다()
        {
            float error = PredictionError(changePostureMidway: false);
            Assert.Less(error, 1e-3f,
                $"자세 유지 창에서 {error:F5}m 어긋났다 — 두 월드가 같은 규칙을 안 돌렸다는 뜻");
        }

        [Test]
        public void 자세를_바꾼_창의_오차가_물리가_허용하는_한계_안에_있다()
        {
            float error = PredictionError(changePostureMidway: true);

            //  천장은 물리에서 유도한다(스펙 §7②의 0.3m는 *라이브 보정량* 합격선이라 여기 쓰면
            //  다른 축의 숫자를 빌려 쓰는 것이 된다). 두 월드가 갈리는 것은 낙하 가속뿐이고,
            //  대자→다이브는 둘 다 "빨라지는" 쪽이라 차이가 FallApproach로 제한된다.
            //  ⚠️ 연속시간 공식(½at²)을 쓰지 않는다 — 시뮬은 매 틱 "속도를 먼저 a×dt만큼
            //  올리고 그 새 속도로 위치를 미는" 이산 적분이라, n틱 뒤 누적 거리는
            //  a×dt²×n(n+1)/2 다(연속시간보다 (n+1)/n배 크다). 연속시간 값을 쓰면 옳은
            //  구현조차 이 천장에 걸려 실패한다.
            int divergedTicks = LeadTicks - PostureChangeTick;
            float fallApproach = Config().FallApproach;   // 하드코딩하지 않고 튜닝값을 따라간다
            float ceiling = fallApproach * Dt * Dt * divergedTicks * (divergedTicks + 1) / 2f;

            TestContext.WriteLine(
                $"자세 변경 창 {LeadTicks}틱 예측 오차: {error:F5} m (물리 천장 {ceiling:F5} m)");

            //  위 천장만 걸면 "아무것도 안 갈렸다"가 조용히 통과한다 — 실제로 이 테스트를 처음 짰을 때
            //  오차가 정확히 0이었는데 통과했다(두 월드가 같은 속도로 나란히 떨어졌다). 아래쪽도 막아
            //  자세 변경이 실제로 낙하 속도에 닿았는지를 테스트가 스스로 확인하게 한다.
            //  0.5는 느슨한 "뭐라도 갈렸나" 확인용이지 다듬은 문턱이 아니다 — 측정값에 맞춰 조이지 않는다.
            Assert.Greater(error, ceiling * 0.5f,
                $"오차 {error:F5}m가 낙하 가속 한계 {ceiling:F5}m의 절반에도 못 미친다 — " +
                "자세 변경이 낙하 속도에 영향을 주지 못하는 상태다(테스트가 아무것도 재지 못하고 있다)");
            //  ⚠️ 이 천장은 <b>느슨한 상한이 아니라 실제로 닿는 값</b>이다. 자세 슬라이더가 창 내내
            //  올라가는 동안 목표 낙하 속도가 계속 앞서 도망가서, 진실 월드는 다섯 틱 전부를 최대
            //  가속(FallApproach)으로 떨어진다 — 그래서 오차가 천장과 <b>같아진다</b>. 부등호가 딱
            //  등호 자리에 서 있으므로 마지막 자리 반올림이 합격/불합격을 가른다. 아래 여유는
            //  <b>여유를 사려고</b> 천장을 늘린 것이 아니라 float32 잡음 몫이다: 위치가 ≈11 m
            //  근처라 한 눈금이 ≈1e-6 m이고, 아홉 틱을 누적해도 그 몇 배다. 반대로 "낙하 가속
            //  말고 다른 것이 끼었다"면 그 크기는 cm 단위라 이 여유로는 절대 못 숨는다.
            //  (y=1000에서 재던 시절에는 눈금이 ≈6.1e-5 m라 이 등호가 우연히 "0.17395 < 0.17400"으로
            //   보였다 — 그때의 초록은 실측이 아니라 반올림이었다.)
            const float FloatNoise = 1e-5f;
            Assert.LessOrEqual(error, ceiling + FloatNoise,
                "자세 변경만으로 설명되지 않는 크기다 — 두 월드가 낙하 가속 말고 다른 데서도 갈렸다");
        }

        //  진실 = 자세 입력이 바뀐 월드. 예측 = 그 입력을 못 받아 "하던 자세"로 계속 구른 월드.
        //  둘을 같은 틱 수만큼 굴려 위치 차이를 잰다.
        static float PredictionError(bool changePostureMidway)
        {
            var truth = Spawn(out Entity truthDiver);
            var predicted = Spawn(out Entity predictedDiver);

            Vector3 start = truthDiver.Get<GameFramework.World.Transform>().Position.ToUnity();

            for (int t = 0; t < LeadTicks; t++)
            {
                //  예측 월드는 입력을 못 받는다 — Current를 그대로 두는 것이 곧
                //  "하던 자세를 계속한다"다(ApplyPostureInput이 마지막 명령을 계속 읽는다).
                if (changePostureMidway && t == PostureChangeTick)
                {
                    truthDiver.Get<InputBuffer>().Current =
                        new InputCommand { Posture = 1f, Posing = true };   // 대자 → 다이브
                }
                truth.Tick(t, Dt);
                predicted.Tick(t, Dt);
            }

            Vector3 truthEnd = truthDiver.Get<GameFramework.World.Transform>().Position.ToUnity();
            Vector3 predictedEnd = predictedDiver.Get<GameFramework.World.Transform>().Position.ToUnity();

            //  스텁이 월드를 얼어붙게 만든 적이 있다(HalfSpaceQuery가 늘 None을 돌려주던 사고).
            //  안 움직였는데 "오차 0"으로 통과하는 것을 막는다.
            Assert.AreNotEqual(start, truthEnd, "월드가 움직이지 않았다 — 스텁이 막고 있다");

            return Vector3.Distance(truthEnd, predictedEnd);
        }

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

        //  고도는 이 테스트와 무관하지만(하늘에 지면이 없다) <b>정밀도</b>에는 상관이 있다.
        //  float32는 값이 클수록 눈금이 굵어져서, y=1000에서는 한 눈금이 ≈6.1e-5 m다 — 아래
        //  천장과 실측값 사이 여유(≈5e-5 m)보다 굵다. 즉 높은 데서 재면 합격/불합격이 반올림
        //  자리에서 갈린다. 원점 근처에서 재면 눈금이 ≈1e-6 m로 줄어 여유가 눈금의 수십 배가 된다.
        const float SpawnY = 0f;

        static Entity Diver(string id, bool simulated = true, EntityType kind = EntityType.Character)
        {
            var e = new Entity(id);
            e.Add(new GameFramework.World.Transform { Position = new Vector3(0f, SpawnY, 0f).ToNumerics() });
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

        //  두 월드를 완전히 같은 초기 상태로 세운다. 하늘(면 없는 HalfSpaceQuery)이라
        //  맵 접지가 끼어들지 않고, 낙하 물리만 남아 오차의 출처가 하나가 된다.
        static SkydiveWorld Spawn(out Entity diver)
        {
            var registry = new EntityRegistry();
            diver = Diver("a");
            //  활공 상태여야 자세 슬라이더가 먹는다(걷기·낙하에서는 대자로 되돌아간다).
            //  Posing = true로 첫 틱에 Skydiving으로 들어가게 해 둔다.
            diver.Get<InputBuffer>().Current = new InputCommand { Posture = 0f, Posing = true };
            //  정지 상태(속도 0)에서 스폰하면 안 된다 — 세로 속도 수렴(Approach)이 "정해진
            //  가속도로만 다가가는" 방식이라, 9틱(0.18초)으로는 종단속도 근처에도 못 가서
            //  목표값이 자세에 따라 달라져도 두 월드가 똑같이 움직인다(오차가 항상 0으로
            //  나와 이 테스트가 아무것도 못 잰다 — 실측으로 확인됨). 이미 대자 종단속도로
            //  떨어지고 있던 다이버로 스폰해야 예측 월드는 그 목표에 머물고, 진실 월드는
            //  자세가 바뀌며 목표가 옮겨가 실제로 갈라진다.
            diver.Get<Velocity>().Linear = new Vector3(0f, -Config().SpreadFallSpeed, 0f).ToNumerics();
            registry.Add(diver);

            var world = World(registry);
            world.GameplayStartTick = 0;
            return world;
        }
    }
}
