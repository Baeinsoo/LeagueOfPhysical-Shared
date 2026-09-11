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

        static void Feed(Entity entity, float yaw, float pitch, bool drawing, bool release)
        {
            entity.Get<InputBuffer>().Current = new InputCommand
            {
                AimYaw = yaw,
                AimPitch = pitch,
                Drawing = drawing,
                Release = release,
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
            inputSystem.Enqueue(buffer, 100, new InputCommand { SequenceNumber = 1, Drawing = true });
            inputSystem.Consume(buffer, 100);
            aimSystem.Tick(archer, 100, TickInterval);

            // tick 101: 입력이 유실 — 서버가 PredictMissing으로 직전 값을 이어 쓴다
            inputSystem.PredictMissing(buffer, maxTicks: 30);
            aimSystem.Tick(archer, 101, TickInterval);

            Assert.IsTrue(archer.Get<ArcheryAim>().Drawing, "예측(유실 보정) 틱에서도 당김이 유지돼야 한다");

            // tick 102: 진짜 Release 커맨드가 도착
            inputSystem.Enqueue(buffer, 102, new InputCommand { SequenceNumber = 2, Release = true });
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
    }
}
