using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class SkydiveMoveSystemTests
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
                groundMoveSpeed: 4f, groundAccel: 100f, jumpPower: 11f, poseClearance: 5f, fallBrake: 150f);

        static Entity Diver(float axis, bool gliding, Vector3 velocity, Vector3 position,
                            float h = 0f, float v = 0f)
        {
            var entity = new Entity("diver-1");
            entity.Add(new GameFramework.World.Transform { Position = position.ToNumerics() });
            entity.Add(new Velocity { Linear = velocity.ToNumerics() });
            entity.Add(new Posture { Axis = axis, Gliding = gliding });
            entity.Add(new MotionState { Value = SkydiveMotionState.Skydiving });
            var buffer = new InputBuffer();
            buffer.Current = new InputCommand { Horizontal = h, Vertical = v };
            entity.Add(buffer);
            return entity;
        }

        static Entity WithState(Entity e, SkydiveMotionState state)
        {
            e.Get<MotionState>().Value = state;
            return e;
        }

        static Vector3 VelocityOf(Entity e) => e.Get<Velocity>().Linear.ToUnity();
        static Vector3 PositionOf(Entity e) => e.Get<GameFramework.World.Transform>().Position.ToUnity();

        // 한 자세로 오래 굴려 정상 상태(목표 속도에 수렴한 뒤)의 하강·수평 속도를 잰다.
        static (float fall, float side) Settle(float axis, bool gliding)
        {
            var e = Diver(axis, gliding, Vector3.zero, new Vector3(0f, 1000f, 0f), h: 1f);
            var sys = new SkydiveMoveSystem();
            for (int i = 0; i < 600; i++) { sys.Tick(e, 0.02f, Config()); }
            var vel = VelocityOf(e);
            return (-vel.y, new Vector2(vel.x, vel.z).magnitude);
        }

        [Test]
        public void 하강_속도는_다이브가_가장_빠르고_패러세일이_가장_느리다()
        {
            float dive = Settle(1f, false).fall;
            float spread = Settle(0f, false).fall;
            float glide = Settle(0f, true).fall;

            Assert.Greater(dive, spread, "다이브가 대자보다 빨라야 한다");
            Assert.Greater(spread, glide, "대자가 패러세일보다 빨라야 한다");
        }

        [Test]
        public void 높이당_수평거리는_패러세일_대자_다이브_순이다()
        {
            var dive = Settle(1f, false);
            var spread = Settle(0f, false);
            var glide = Settle(0f, true);

            float diveRatio = dive.side / dive.fall;
            float spreadRatio = spread.side / spread.fall;
            float glideRatio = glide.side / glide.fall;

            Assert.Greater(glideRatio, spreadRatio, "패러세일이 대자보다 멀리 가야 한다");
            Assert.Greater(spreadRatio, diveRatio, "대자가 다이브보다 멀리 가야 한다");
        }

        [Test]
        public void 선회는_대자가_가장_민첩하다()
        {
            // 정지 상태에서 한 틱만 굴려 수평 가속의 크기를 비교한다.
            float Accel(float axis, bool gliding)
            {
                var e = Diver(axis, gliding, Vector3.zero, new Vector3(0f, 1000f, 0f), h: 1f);
                new SkydiveMoveSystem().Tick(e, 0.02f, Config());
                var vel = VelocityOf(e);
                return new Vector2(vel.x, vel.z).magnitude;
            }

            Assert.Greater(Accel(0f, false), Accel(0f, true), "대자가 패러세일보다 민첩해야 한다");
            Assert.Greater(Accel(0f, true), Accel(1f, false), "패러세일이 다이브보다 민첩해야 한다");
        }

        [Test]
        public void 위치는_건드리지_않는다()
        {
            //  맵 충돌까지 봐야 최종 위치가 정해지므로, 위치는 SkydiveWorld가 정한다.
            //  MoveSystem이 여기서 위치를 미리 옮기면 그 값이 sweep의 출발점을 오염시킨다.
            //  (바닥에 닿으면 멈추는지는 이제 SkydiveWorldTests가 진짜 지오메트리로 잰다.)
            var e = Diver(0f, false, new Vector3(0f, -25f, 0f), new Vector3(0f, 500f, 0f), h: 1f);

            new SkydiveMoveSystem().Tick(e, 0.02f, Config());

            Assert.AreEqual(500f, PositionOf(e).y, Tolerance, "MoveSystem은 위치를 쓰지 않는다");
            Assert.AreEqual(0f, PositionOf(e).x, Tolerance);
        }

        // 발 딛고 있는 다이버. 접지 여부는 이동 커널이 적어 주는 값이라 테스트가 직접 세운다.
        static Entity GroundedDiver(Vector3 velocity, float h = 0f, float v = 0f)
        {
            var e = Diver(0f, false, velocity, new Vector3(0f, 0f, 0f), h, v);
            e.Add(new GroundState { IsGrounded = true });
            return WithState(e, SkydiveMotionState.Walking);
        }

        // 걷기는 다른 게임과 같은 커널을 부른다. 상수를 베낀 게 아니라 같은 함수라는 것을,
        // 그 커널을 직접 돌린 결과와 대조해 붙잡는다 — 한쪽만 바뀌면 여기서 깨진다.
        [Test]
        public void 걷기는_공용_이동_커널과_같은_답을_낸다()
        {
            var config = Config();
            var e = GroundedDiver(new Vector3(3f, 0f, 1f), h: 1f, v: 0f);

            new SkydiveMoveSystem().Tick(e, 0.02f, config);

            var expected = MovementMotor.CalcVelocity(new MovementInput(
                new Vector3(3f, 0f, 1f), 1f, 0f,
                config.GroundMoveSpeed, config.GroundAccel, 0.02f));

            Assert.AreEqual(expected.velocity.x, VelocityOf(e).x, Tolerance);
            Assert.AreEqual(expected.velocity.z, VelocityOf(e).z, Tolerance);
        }

        [Test]
        public void 걸으면_이동_방향으로_몸이_돈다()
        {
            // +x로 걸으면 y회전 90도. 이게 없으면 달리기 애니메이션이 옆을 본 채로 나와
            // 게걸음처럼 보인다.
            var e = GroundedDiver(Vector3.zero, h: 1f, v: 0f);

            new SkydiveMoveSystem().Tick(e, 0.02f, Config());

            Vector3 euler = e.Get<GameFramework.World.Transform>().Rotation.ToUnity().eulerAngles;
            Assert.AreEqual(90f, euler.y, 0.01f, "+x로 걸으면 그쪽을 봐야 한다");
        }

        [Test]
        public void 손을_떼면_보던_방향을_유지한다()
        {
            // 입력이 없을 때 0도로 튕겨 돌아가면, 멈출 때마다 몸이 홱 돈다.
            var e = GroundedDiver(Vector3.zero, h: 1f, v: 0f);
            var sys = new SkydiveMoveSystem();
            sys.Tick(e, 0.02f, Config());

            e.Get<InputBuffer>().Current = new InputCommand();   // 손을 뗀다
            sys.Tick(e, 0.02f, Config());

            Vector3 euler = e.Get<GameFramework.World.Transform>().Rotation.ToUnity().eulerAngles;
            Assert.AreEqual(90f, euler.y, 0.01f, "손을 떼도 마지막으로 보던 쪽이어야 한다");
        }

        [Test]
        public void 공중에서는_몸을_돌리지_않는다()
        {
            // 낙하 자세 기울기가 이 회전 위에 얹히므로, 공중에서 돌리면 별개 판단이 필요해진다.
            var e = Diver(0f, false, Vector3.zero, new Vector3(0f, 500f, 0f), h: 1f);
            e.Add(new GroundState { IsGrounded = false });

            new SkydiveMoveSystem().Tick(e, 0.02f, Config());

            Vector3 euler = e.Get<GameFramework.World.Transform>().Rotation.ToUnity().eulerAngles;
            Assert.AreEqual(0f, euler.y, 0.01f, "공중에서는 스폰 방향 그대로여야 한다");
        }

        [Test]
        public void 땅에서는_손을_떼면_거의_바로_선다()
        {
            // 공중 가속(22)이면 12m/s에서 멈추는 데 0.55초가 걸려 3m를 미끄러진다.
            // 걷기 가속(100)이면 0.1초 안에 선다 — 이 차이가 "얼음 위" 느낌의 정체다.
            var e = GroundedDiver(new Vector3(8f, 0f, 0f), h: 0f);
            var sys = new SkydiveMoveSystem();

            for (int i = 0; i < 5; i++) { sys.Tick(e, 0.02f, Config()); }   // 0.1초

            Assert.AreEqual(0f, VelocityOf(e).x, Tolerance, "걷다 손을 떼면 0.1초 안에 서야 한다");
        }

        [Test]
        public void 공중에서는_같은_시간에_아직_미끄러진다()
        {
            // 위 테스트의 대조군 — 같은 0.1초를 공중에서 굴리면 아직 한참 남아 있어야 한다.
            // 둘을 같이 재야 "땅만 바뀌었다"가 증명된다(공중 값을 건드리면 코스 검산이 무너진다).
            var e = Diver(0f, false, new Vector3(8f, 0f, 0f), new Vector3(0f, 500f, 0f), h: 0f);
            var sys = new SkydiveMoveSystem();

            for (int i = 0; i < 5; i++) { sys.Tick(e, 0.02f, Config()); }

            Assert.Greater(VelocityOf(e).x, 5f, "공중 제동은 그대로 완만해야 한다");
        }

        [Test]
        public void 땅에서는_걷기_최고속을_넘지_않는다()
        {
            var e = GroundedDiver(Vector3.zero, h: 1f);
            var sys = new SkydiveMoveSystem();

            for (int i = 0; i < 100; i++) { sys.Tick(e, 0.02f, Config()); }

            Assert.AreEqual(4f, VelocityOf(e).x, Tolerance, "자세별 속도(12~18)가 아니라 걷기 속도여야 한다");
        }

        [Test]
        public void 땅에서_점프하면_설정한_세로_속도로_뜬다()
        {
            var e = GroundedDiver(Vector3.zero);
            e.Get<InputBuffer>().Current = new InputCommand { Jump = true };

            new SkydiveMoveSystem().Tick(e, 0.02f, Config());

            Assert.AreEqual(11f, VelocityOf(e).y, Tolerance, "점프는 지금까지의 세로 속도를 지우고 새로 준다");
        }

        [Test]
        public void 공중에서는_점프가_안_된다()
        {
            // 접지가 아니면 눌러도 무시한다 — 안 그러면 떨어지는 내내 세로 속도를 리셋해
            // 무한 활공이 된다.
            var e = Diver(0f, false, new Vector3(0f, -25f, 0f), new Vector3(0f, 500f, 0f));
            e.Add(new GroundState { IsGrounded = false });
            e.Get<InputBuffer>().Current = new InputCommand { Jump = true };

            new SkydiveMoveSystem().Tick(e, 0.02f, Config());

            Assert.Less(VelocityOf(e).y, 0f, "공중에서 누른 점프는 무시돼야 한다");
        }

        [Test]
        public void 낙하_상태에서는_좌우_입력을_안_받는다()
        {
            // 젤다의 점프는 뛰기 전에 방향을 정하는 것이다 — 뛴 뒤에 꺾을 수 없다.
            // 낮은 데서 떨어지는 것도 같다: 아직 스카이다이빙에 못 들어갔으면 조종이 없다.
            var e = Diver(0f, false, new Vector3(3f, 5f, 0f), new Vector3(0f, 10f, 0f), h: 1f);
            e.Add(new GroundState { IsGrounded = false });
            WithState(e, SkydiveMotionState.Falling);

            new SkydiveMoveSystem().Tick(e, 0.02f, Config());

            Assert.AreEqual(3f, VelocityOf(e).x, Tolerance, "낙하 중에는 좌우가 그대로여야 한다");
        }

        [Test]
        public void 낙하_상태에서는_내려가는_중에도_잠긴다()
        {
            // 정점을 지나도 안 풀린다 — 젤다는 착지하거나 스카이다이빙에 들어가야 조종이 돌아온다.
            var e = Diver(0f, false, new Vector3(3f, -8f, 0f), new Vector3(0f, 10f, 0f), h: 1f);
            e.Add(new GroundState { IsGrounded = false });
            WithState(e, SkydiveMotionState.Falling);

            new SkydiveMoveSystem().Tick(e, 0.02f, Config());

            Assert.AreEqual(3f, VelocityOf(e).x, Tolerance, "내려가는 중에도 낙하면 잠겨야 한다");
        }

        [Test]
        public void 낙하_상태에서는_점프를_못_한다()
        {
            // 걷기에서만 뛴다 — 안 그러면 공중에서 계속 눌러 무한히 뜬다.
            var e = Diver(0f, false, new Vector3(0f, -25f, 0f), new Vector3(0f, 500f, 0f));
            e.Add(new GroundState { IsGrounded = false });
            WithState(e, SkydiveMotionState.Falling);
            e.Get<InputBuffer>().Current = new InputCommand { Jump = true };

            new SkydiveMoveSystem().Tick(e, 0.02f, Config());

            Assert.Less(VelocityOf(e).y, 0f, "공중에서 누른 점프는 무시돼야 한다");
        }

        [Test]
        public void 상태_전이는_착지_활공유지_여유_순으로_갈린다()
        {
            // 닿으면 무조건 걷기
            Assert.AreEqual(SkydiveMotionState.Walking,
                SkydiveMotion.Advance(SkydiveMotionState.Skydiving, grounded: true, hasClearanceBelow: true));
            // 한 번 들어온 활공은 발밑이 막혀도 유지 — 패러세일이 착지 직전에 접히면 안 된다
            Assert.AreEqual(SkydiveMotionState.Skydiving,
                SkydiveMotion.Advance(SkydiveMotionState.Skydiving, grounded: false, hasClearanceBelow: false));
            // 떠 있고 여유가 있으면 들어간다
            Assert.AreEqual(SkydiveMotionState.Skydiving,
                SkydiveMotion.Advance(SkydiveMotionState.Falling, grounded: false, hasClearanceBelow: true));
            // 여유가 없으면 아직 낙하
            Assert.AreEqual(SkydiveMotionState.Falling,
                SkydiveMotion.Advance(SkydiveMotionState.Walking, grounded: false, hasClearanceBelow: false));
        }

        [Test]
        public void 되감으면_이동_상태도_돌아온다()
        {
            // 안 되돌리면 재생 중 조작 잠금·슬라이더 허용이 라이브와 달라져 궤적이 갈린다.
            var e = GroundedDiver(Vector3.zero);
            WithState(e, SkydiveMotionState.Skydiving);
            var snap = SkydiveSavedState.Capture(e);

            WithState(e, SkydiveMotionState.Walking);
            snap.RestoreTo(e);

            Assert.AreEqual(SkydiveMotionState.Skydiving, e.Get<MotionState>().Value);
        }

        [Test]
        public void 점프_높이가_설정값에서_나오는_높이와_맞는다()
        {
            // 도달 높이 = JumpPower² / (2 × FallApproach) = 121/60 ≈ 2.02m.
            // 숫자를 코드에 박지 않고 config에서 끌어내는지를 이 테스트가 붙잡는다.
            var e = GroundedDiver(Vector3.zero);
            e.Get<InputBuffer>().Current = new InputCommand { Jump = true };
            var sys = new SkydiveMoveSystem();
            var config = Config();

            sys.Tick(e, 0.02f, config);
            e.Get<InputBuffer>().Current = new InputCommand();      // 뛰는 중엔 어차피 좌우가 안 먹는다
            e.Get<GroundState>().IsGrounded = false;                // 떴으니 이제 공중이다

            // 세로 속도를 적분해 정점을 찾는다(월드가 하는 일을 여기선 손으로).
            float height = 0f;
            for (int i = 0; i < 200 && VelocityOf(e).y > 0f; i++)
            {
                height += VelocityOf(e).y * 0.02f;
                sys.Tick(e, 0.02f, config);
            }

            float expected = config.JumpPower * config.JumpPower / (2f * config.FallApproach);
            Assert.AreEqual(expected, height, 0.15f, "도달 높이가 설정값이 말하는 높이와 달라졌다");
        }

        [Test]
        public void 패러세일을_펴면_하강이_급격히_꺾인다()
        {
            // 낙하산은 펴는 순간 속도가 꺾여야 한다. 중력과 같은 비율(29)로 줄면
            // 60에서 6까지 1.9초가 걸려 낙하산이 아니게 된다.
            var e = Diver(0f, true, new Vector3(0f, -60f, 0f), new Vector3(0f, 500f, 0f));
            e.Add(new GroundState { IsGrounded = false });
            var sys = new SkydiveMoveSystem();

            for (int i = 0; i < 25; i++) { sys.Tick(e, 0.02f, Config()); }   // 0.5초

            Assert.AreEqual(-6f, VelocityOf(e).y, Tolerance, "0.5초 안에 활공 속도로 내려앉아야 한다");
        }

        [Test]
        public void 빨라질_때는_중력_비율_그대로다()
        {
            // 위 테스트의 대조군 — 감속만 커야지 가속까지 커지면 낙하가 통째로 달라진다.
            var e = Diver(0f, false, Vector3.zero, new Vector3(0f, 500f, 0f));
            e.Add(new GroundState { IsGrounded = false });

            new SkydiveMoveSystem().Tick(e, 0.02f, Config());

            // 29 m/s² × 0.02s = 0.58
            Assert.AreEqual(-0.58f, VelocityOf(e).y, Tolerance, "빨라질 때는 중력(FallApproach)이다");
        }

        [Test]
        public void 입력이_없으면_수평_속도가_줄어든다()
        {
            var e = Diver(0f, false, new Vector3(10f, 0f, 0f), new Vector3(0f, 1000f, 0f), h: 0f);

            new SkydiveMoveSystem().Tick(e, 0.1f, Config());

            Assert.Less(VelocityOf(e).x, 10f, "입력이 없으면 목표가 0이라 감속해야 한다");
        }

        [Test]
        public void 자세가_바뀌어도_속도가_한_틱에_튀지_않는다()
        {
            // 대자로 수렴시킨 뒤 다이브로 바꿔 한 틱 — 하강 속도 변화가 목표 차이보다 훨씬 작아야 한다.
            var e = Diver(0f, false, Vector3.zero, new Vector3(0f, 1000f, 0f));
            var sys = new SkydiveMoveSystem();
            for (int i = 0; i < 600; i++) { sys.Tick(e, 0.02f, Config()); }

            float before = -VelocityOf(e).y;
            e.Get<Posture>().Axis = 1f;
            sys.Tick(e, 0.02f, Config());
            float after = -VelocityOf(e).y;

            Assert.Less(after - before, 5f, "한 틱에 목표까지 점프하면 안 된다 (수렴이어야 한다)");
            Assert.Greater(after, before, "그래도 빨라지는 방향이어야 한다");
        }
    }
}
