using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class ArcheryAimSystemTests
    {
        const float Tolerance = 1e-3f;
        const float TickInterval = 0.02f;   // 50Hz

        static Entity Archer(Vector3 position)
        {
            var entity = new Entity("archer-1");
            entity.Add(new GameFramework.World.Transform { Position = position.ToNumerics() });
            entity.Add(new ArcheryAim());
            entity.Add(new InputBuffer());
            return entity;
        }

        //  당김은 손가락이 끈 거리가 정한다 — 완전히 당긴 상태(1.0)를 기본으로 먹인다.
        //  임계치(DrawThreshold) 미만이면 떼도 안 쏘므로, 취소를 재는 테스트만 따로 낮춰 부른다.
        static void Feed(Entity entity, float yaw, float pitch, bool drawing, bool release,
                         float drawRatio = 1f)
        {
            entity.Get<InputBuffer>().Current = new InputCommand
            {
                AimYaw = yaw,
                AimPitch = pitch,
                Drawing = drawing,
                Release = release,
                //  떼는 틱은 Drawing=false로 온다(실제 클라와 같다) — 그때도 값을 싣되,
                //  시뮬은 당기던 동안 쌓아 둔 값을 쓰므로 여기 값은 무시된다.
                DrawRatio = drawRatio,
            };
        }

        [Test]
        public void 당기기_시작한_틱을_기억한다()
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem();

            Feed(archer, 0f, 0f, drawing: true, release: false);
            system.Tick(archer, 100, TickInterval);

            var aim = archer.Get<ArcheryAim>();
            Assert.IsTrue(aim.Drawing);
            Assert.AreEqual(100, aim.DrawStartTick);
        }

        [Test]
        public void 계속_당기고_있으면_시작_틱이_바뀌지_않는다()
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem();

            Feed(archer, 0f, 0f, drawing: true, release: false);
            system.Tick(archer, 100, TickInterval);
            system.Tick(archer, 110, TickInterval);

            Assert.AreEqual(100, archer.Get<ArcheryAim>().DrawStartTick);
        }

        [Test]
        public void 조준_각도가_상태에_들어온다()
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem();

            Feed(archer, 30f, -15f, drawing: false, release: false);
            system.Tick(archer, 1, TickInterval);

            var aim = archer.Get<ArcheryAim>();
            Assert.AreEqual(30f, aim.Yaw, Tolerance);
            Assert.AreEqual(-15f, aim.Pitch, Tolerance);
        }

        [Test]
        public void HeldSeconds_정수_틱_간격을_초로_바꾼다()
        {
            Assert.AreEqual(0.1f, ArcheryAimSystem.HeldSeconds(100, 105, TickInterval), 1e-5f);
        }

        [Test]
        public void HeldSeconds_소수_틱도_받는다()
        {
            // 화면은 렌더 시각(소수 틱)으로 묻는다 — 정수 틱만 받던 예전 식으로는 표현 못 하던 값.
            Assert.AreEqual(0.11f, ArcheryAimSystem.HeldSeconds(100, 105.5, TickInterval), 1e-5f);
        }

        [Test]
        public void HeldSeconds_시작_틱과_같으면_0이다()
        {
            Assert.AreEqual(0f, ArcheryAimSystem.HeldSeconds(100, 100, TickInterval), 1e-5f);
        }

        [Test]
        public void DrawRatio_시작_틱에서는_0이다()
        {
            Assert.AreEqual(0f, ArcheryAimSystem.DrawRatio(100, 100, TickInterval), 1e-5f);
        }

        [Test]
        public void DrawRatio_중간에는_0과_1_사이다()
        {
            float half = ArcheryAimSystem.FullDrawSeconds / 2f / TickInterval;
            float ratio = ArcheryAimSystem.DrawRatio(0, (long)half, TickInterval);
            Assert.Greater(ratio, 0f);
            Assert.Less(ratio, 1f);
        }

        [Test]
        public void DrawRatio_끝까지_당기면_1이다()
        {
            long fullTicks = (long)(ArcheryAimSystem.FullDrawSeconds / TickInterval);
            Assert.AreEqual(1f, ArcheryAimSystem.DrawRatio(0, fullTicks, TickInterval), 1e-5f);
        }

        [Test]
        public void DrawRatio_더_당겨도_1을_넘지_않는다()
        {
            Assert.AreEqual(1f, ArcheryAimSystem.DrawRatio(0, 10000, TickInterval), 1e-5f);
        }

        [Test]
        public void 오래_당길수록_화살이_빠르다()
        {
            float shortDraw = ArcheryAimSystem.SpeedFor(
                ArcheryAimSystem.DrawRatio(100, 105, TickInterval));      // 0.1초
            float longDraw = ArcheryAimSystem.SpeedFor(
                ArcheryAimSystem.DrawRatio(100, 130, TickInterval));      // 0.6초

            Assert.Greater(longDraw, shortDraw);
        }

        [Test]
        public void 끝까지_당긴_뒤_더_당겨도_같은_속도다()
        {
            float full = ArcheryAimSystem.SpeedFor(
                ArcheryAimSystem.DrawRatio(0, (long)(ArcheryAimSystem.FullDrawSeconds / TickInterval), TickInterval));
            float longer = ArcheryAimSystem.SpeedFor(ArcheryAimSystem.DrawRatio(0, 10000, TickInterval));

            Assert.AreEqual(ArcheryAimSystem.MaxSpeed, full, Tolerance);
            Assert.AreEqual(ArcheryAimSystem.MaxSpeed, longer, Tolerance);
        }

        [Test]
        public void 떼는_틱에만_화살이_나온다()
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem();

            Feed(archer, 0f, 0f, drawing: true, release: false);
            Assert.IsNull(system.Tick(archer, 100, TickInterval));

            Feed(archer, 0f, 0f, drawing: false, release: true);
            Assert.IsNotNull(system.Tick(archer, 120, TickInterval));

            Feed(archer, 0f, 0f, drawing: false, release: false);
            Assert.IsNull(system.Tick(archer, 121, TickInterval));
        }

        [Test]
        public void 당기지_않고_떼면_화살이_없다()
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem();

            Feed(archer, 0f, 0f, drawing: false, release: true);

            Assert.IsNull(system.Tick(archer, 100, TickInterval));
        }

        [Test]
        public void 화살은_눈높이에서_조준_방향으로_떠난다()
        {
            var archer = Archer(new Vector3(5f, 0f, -3f));
            var system = new ArcheryAimSystem();

            Feed(archer, 90f, 0f, drawing: true, release: false);
            system.Tick(archer, 100, TickInterval);
            Feed(archer, 90f, 0f, drawing: false, release: true);
            var shot = system.Tick(archer, 140, TickInterval);

            Assert.IsTrue(shot.HasValue);
            Assert.AreEqual("archer-1", shot.Value.ShooterId);
            Assert.AreEqual(140, shot.Value.FireTick);
            Assert.AreEqual(5f, shot.Value.Origin.x, Tolerance);
            Assert.AreEqual(ArcheryAimSystem.EyeHeight, shot.Value.Origin.y, Tolerance);
            Assert.AreEqual(-3f, shot.Value.Origin.z, Tolerance);
            // 좌우 90도 = +x 방향. 세로 성분은 없다.
            Assert.Greater(shot.Value.Velocity.x, 0f);
            Assert.AreEqual(0f, shot.Value.Velocity.y, Tolerance);
            Assert.AreEqual(0f, shot.Value.Velocity.z, Tolerance);
        }

        [Test]
        public void 쏘고_나면_당김이_풀린다()
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem();

            Feed(archer, 0f, 0f, drawing: true, release: false);
            system.Tick(archer, 100, TickInterval);
            Feed(archer, 0f, 0f, drawing: false, release: true);
            system.Tick(archer, 140, TickInterval);

            Assert.IsFalse(archer.Get<ArcheryAim>().Drawing);
        }

        // I1 회귀 방지: 입력이 한 틱 유실돼도 InputBufferSystem.PredictMissing이 Drawing/AimYaw/
        // AimPitch를 이어 써야 서버에서 당김이 끊기지 않는다. 이 셋을 이어 쓰기 목록에서 빼면
        // (I1 이전 상태로 되돌리면) 예측 틱에서 Drawing이 false로 리셋돼 이 테스트가 빨개진다.
        [Test]
        public void 입력이_한_틱_비어도_당김이_끊기지_않는다()
        {
            var archer = Archer(Vector3.zero);
            var aimSystem = new ArcheryAimSystem();
            var inputSystem = new InputBufferSystem();
            var buffer = archer.Get<InputBuffer>();

            // tick 100: 진짜 커맨드로 당기기 시작
            inputSystem.Enqueue(buffer, 100, new InputCommand { SequenceNumber = 1, Drawing = true, DrawRatio = 1f });
            inputSystem.Consume(buffer, 100);
            aimSystem.Tick(archer, 100, TickInterval);

            // tick 101: 입력이 유실 — 서버가 PredictMissing으로 직전 값을 이어 쓴다
            inputSystem.PredictMissing(buffer, maxTicks: 30);
            aimSystem.Tick(archer, 101, TickInterval);

            Assert.IsTrue(archer.Get<ArcheryAim>().Drawing, "예측(유실 보정) 틱에서도 당김이 유지돼야 한다");

            // tick 102: 진짜 Release 커맨드가 도착
            inputSystem.Enqueue(buffer, 102, new InputCommand { SequenceNumber = 2, Release = true, DrawRatio = 1f });
            inputSystem.Consume(buffer, 102);
            var shot = aimSystem.Tick(archer, 102, TickInterval);

            Assert.IsTrue(shot.HasValue, "예측 틱을 거쳐도 발사가 나와야 한다");
            Assert.Greater(shot.Value.Velocity.magnitude, ArcheryAimSystem.MinSpeed,
                "당김이 끊기지 않았으므로(DrawStartTick=100 유지) 최소속도보다 빨라야 한다");
        }

        // I1 옆 회귀 방지: Release=true인 커맨드가 (재전송 등으로) 다음 틱에도 그대로 남아 있어도
        // 두 번 쏘면 안 된다. 방어선은 발사 시 aim.Drawing=false로 떨어뜨리는 것뿐이라, 그 한 줄이
        // 빠지면 이 테스트가 빨개진다.
        [Test]
        public void 같은_릴리즈_커맨드가_두_틱_연속_남아도_두_번_쏘지_않는다()
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem();

            Feed(archer, 0f, 0f, drawing: true, release: false);
            system.Tick(archer, 100, TickInterval);

            Feed(archer, 0f, 0f, drawing: false, release: true);
            var first = system.Tick(archer, 120, TickInterval);
            // Current를 갱신하지 않고 그대로 둔 채 다시 Tick — aim.Drawing 가드가 유일한 방어선.
            var second = system.Tick(archer, 121, TickInterval);

            Assert.IsTrue(first.HasValue);
            Assert.IsNull(second, "당김 가드가 없으면 같은 Release 커맨드가 남아 두 번 쏜다");
        }

        //  손가락이 스치기만 해도 화살이 나가면 조준하다 실수로 쏘게 된다. 임계치가 그 선이다.
        [Test]
        public void 임계치를_못_넘고_떼면_쏘지_않는다()
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem();

            Feed(archer, 0f, 0f, drawing: true, release: false,
                 drawRatio: ArcheryAimSystem.DrawThreshold - 0.01f);
            system.Tick(archer, 100, TickInterval);

            Feed(archer, 0f, 0f, drawing: true, release: true,
                 drawRatio: ArcheryAimSystem.DrawThreshold - 0.01f);
            Assert.IsNull(system.Tick(archer, 101, TickInterval), "임계치 미만이면 취소여야 한다");
        }

        [Test]
        public void 임계치를_넘기면_쏜다()
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem();

            Feed(archer, 0f, 0f, drawing: true, release: false,
                 drawRatio: ArcheryAimSystem.DrawThreshold);
            system.Tick(archer, 100, TickInterval);

            Feed(archer, 0f, 0f, drawing: true, release: true,
                 drawRatio: ArcheryAimSystem.DrawThreshold);
            Assert.IsNotNull(system.Tick(archer, 101, TickInterval));
        }

        //  당긴 만큼 빨라진다 — 드래그 거리가 파워를 정한다는 계약 그 자체다.
        [Test]
        public void 많이_당길수록_화살이_빠르다()
        {
            float Speed(float ratio)
            {
                var archer = Archer(Vector3.zero);
                var system = new ArcheryAimSystem();
                Feed(archer, 0f, 0f, drawing: true, release: false, drawRatio: ratio);
                system.Tick(archer, 100, TickInterval);
                Feed(archer, 0f, 0f, drawing: true, release: true, drawRatio: ratio);
                return system.Tick(archer, 101, TickInterval).Value.Velocity.magnitude;
            }

            Assert.Less(Speed(0.3f), Speed(1f));
            //  상한이 있다 — 1을 넘겨 실어도 더 세지지 않는다.
            Assert.AreEqual(Speed(1f), Speed(2f), 1e-3f);
        }

    }
}
