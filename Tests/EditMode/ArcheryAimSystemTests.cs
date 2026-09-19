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

        //  이 파일의 시험 대부분은 흔들림과 무관한 것을 잰다 — shakeMaxDegrees=0이면
        //  ArcheryShake.Offset이 항상 (0,0)을 내놓으므로(ArcheryShake.cs 참고) 조준이 그대로다.
        //  흔들림 자체를 재는 시험은 별도로 흔들리는 설정을 만들어 쓴다.
        static ArcheryConfig NoSwayConfig() => new ArcheryConfig(
            wavePeriodTicks: 100, minTargets: 1, maxTargets: 1,
            spawnRadius: 1f, spawnMinY: 0f, spawnMaxY: 1f, minSeparation: 1f,
            trapRatioMin: 0f, trapRatioMax: 0f,
            shakeFreeSeconds: 1f, shakeRampSeconds: 1f, shakeMaxDegrees: 0f,
            riseHeightMin: 0.1f, riseHeightMax: 0.1f, staggerTicks: 1, restTicks: 1,
            kinds: null);

        //  손가락을 대고 있는 동안 화면이 보내는 값은 목표(0 또는 1)다 — 완전히 당긴 상태(1.0)를
        //  기본으로 먹인다. 임계치(DrawThreshold) 미만이면 떼도 안 쏘므로, 취소를 재는 테스트만
        //  따로 낮춰 부른다.
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
            var system = new ArcheryAimSystem(NoSwayConfig());

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
            var system = new ArcheryAimSystem(NoSwayConfig());

            Feed(archer, 0f, 0f, drawing: true, release: false);
            system.Tick(archer, 100, TickInterval);
            system.Tick(archer, 110, TickInterval);

            Assert.AreEqual(100, archer.Get<ArcheryAim>().DrawStartTick);
        }

        [Test]
        public void 조준_각도가_상태에_들어온다()
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem(NoSwayConfig());

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
        public void 오래_당길수록_화살이_빠르다()
        {
            //  정적 헬퍼(구 DrawRatio)가 아니라 실제 Tick 시뮬을 통해 잰다 — 실제 램프는
            //  MoveTowards(직전 값에서 이어감)라 시작 틱에서의 선형 공식과 더 이상 일치하지 않는다.
            float ShootAfterTicks(int heldTicks)
            {
                var archer = Archer(Vector3.zero);
                var system = new ArcheryAimSystem(NoSwayConfig());
                for (long t = 0; t < heldTicks; t++)
                {
                    Feed(archer, 0f, 0f, drawing: true, release: false);
                    system.Tick(archer, t, TickInterval);
                }
                Feed(archer, 0f, 0f, drawing: false, release: true);
                return system.Tick(archer, heldTicks, TickInterval).Value.Velocity.magnitude;
            }

            float shortDraw = ShootAfterTicks(10);   // 0.2초 — 임계치(0.3)를 겨우 넘긴다
            float longDraw = ShootAfterTicks(30);    // 0.6초 — 만작

            Assert.Greater(longDraw, shortDraw);
        }

        [Test]
        public void 끝까지_당긴_뒤_더_당겨도_같은_속도다()
        {
            //  SpeedFor 자체의 상한 클램프를 잰다 — 만작(1.0)과 그 이상(2.0) 둘 다 MaxSpeed다.
            Assert.AreEqual(ArcheryAimSystem.MaxSpeed, ArcheryAimSystem.SpeedFor(1f), Tolerance);
            Assert.AreEqual(ArcheryAimSystem.MaxSpeed, ArcheryAimSystem.SpeedFor(2f), Tolerance);
        }

        [Test]
        public void 떼는_틱에만_화살이_나온다()
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem(NoSwayConfig());

            //  시위가 시간을 두고 차오르므로(예전엔 한 틱에 임계치를 넘었다) 임계치를 넘길
            //  때까지 여러 틱을 잡고 있는다 — 이 시험이 재는 것은 "떼는 틱에만 화살이 나온다"이지
            //  당김 속도가 아니다.
            for (long t = 100; t < 120; t++)
            {
                Feed(archer, 0f, 0f, drawing: true, release: false);
                Assert.IsNull(system.Tick(archer, t, TickInterval));
            }

            Feed(archer, 0f, 0f, drawing: false, release: true);
            Assert.IsNotNull(system.Tick(archer, 120, TickInterval));

            Feed(archer, 0f, 0f, drawing: false, release: false);
            Assert.IsNull(system.Tick(archer, 121, TickInterval));
        }

        [Test]
        public void 당기지_않고_떼면_화살이_없다()
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem(NoSwayConfig());

            Feed(archer, 0f, 0f, drawing: false, release: true);

            Assert.IsNull(system.Tick(archer, 100, TickInterval));
        }

        [Test]
        public void 화살은_눈높이에서_조준_방향으로_떠난다()
        {
            var archer = Archer(new Vector3(5f, 0f, -3f));
            var system = new ArcheryAimSystem(NoSwayConfig());

            //  임계치를 넘길 때까지 잡고 있어야 실제로 발사된다.
            for (long t = 100; t < 140; t++)
            {
                Feed(archer, 90f, 0f, drawing: true, release: false);
                system.Tick(archer, t, TickInterval);
            }
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
            var system = new ArcheryAimSystem(NoSwayConfig());

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
            var aimSystem = new ArcheryAimSystem(NoSwayConfig());
            var inputSystem = new InputBufferSystem();
            var buffer = archer.Get<InputBuffer>();

            // tick 100~108: 진짜 커맨드로 당기기 시작해 임계치를 넘길 때까지 잡고 있는다
            //  (시위가 시간을 두고 차오르는 데다, 당기기 시작한 첫 틱은 0에서 다시 출발하므로
            //  [Important 1] 한 틱만으론 임계치를 못 넘는다).
            for (long t = 100; t < 109; t++)
            {
                inputSystem.Enqueue(buffer, t, new InputCommand { SequenceNumber = t - 99, Drawing = true, DrawRatio = 1f });
                inputSystem.Consume(buffer, t);
                aimSystem.Tick(archer, t, TickInterval);
            }

            // tick 109: 입력이 유실 — 서버가 PredictMissing으로 직전 값을 이어 쓴다
            inputSystem.PredictMissing(buffer, maxTicks: 30);
            aimSystem.Tick(archer, 109, TickInterval);

            Assert.IsTrue(archer.Get<ArcheryAim>().Drawing, "예측(유실 보정) 틱에서도 당김이 유지돼야 한다");

            // tick 110: 진짜 Release 커맨드가 도착
            inputSystem.Enqueue(buffer, 110, new InputCommand { SequenceNumber = 11, Release = true, DrawRatio = 1f });
            inputSystem.Consume(buffer, 110);
            var shot = aimSystem.Tick(archer, 110, TickInterval);

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
            var system = new ArcheryAimSystem(NoSwayConfig());

            //  임계치를 넘길 때까지 잡고 있는다.
            for (long t = 100; t < 120; t++)
            {
                Feed(archer, 0f, 0f, drawing: true, release: false);
                system.Tick(archer, t, TickInterval);
            }

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
            var system = new ArcheryAimSystem(NoSwayConfig());

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
            var system = new ArcheryAimSystem(NoSwayConfig());

            //  목표(DrawThreshold)까지 시위가 차오를 시간을 준다 — 시위는 한 틱에 목표로
            //  순간이동하지 않는다.
            for (long t = 100; t < 110; t++)
            {
                Feed(archer, 0f, 0f, drawing: true, release: false,
                     drawRatio: ArcheryAimSystem.DrawThreshold);
                system.Tick(archer, t, TickInterval);
            }

            Feed(archer, 0f, 0f, drawing: true, release: true,
                 drawRatio: ArcheryAimSystem.DrawThreshold);
            Assert.IsNotNull(system.Tick(archer, 110, TickInterval));
        }

        //  당긴 만큼 빨라진다 — 드래그 거리가 파워를 정한다는 계약 그 자체다.
        [Test]
        public void 많이_당길수록_화살이_빠르다()
        {
            //  시위가 목표까지 차오를 시간을 준다 — 한 틱만 당기면 속도 상한에 걸려
            //  0.3이든 1.0이든 똑같이 한 틱분만 차서 둘이 구분되지 않는다.
            float Speed(float ratio)
            {
                var archer = Archer(Vector3.zero);
                var system = new ArcheryAimSystem(NoSwayConfig());
                for (long t = 100; t < 130; t++)
                {
                    Feed(archer, 0f, 0f, drawing: true, release: false, drawRatio: ratio);
                    system.Tick(archer, t, TickInterval);
                }
                Feed(archer, 0f, 0f, drawing: false, release: true, drawRatio: 0f);
                return system.Tick(archer, 130, TickInterval).Value.Velocity.magnitude;
            }

            Assert.Less(Speed(0.3f), Speed(1f));
            //  상한이 있다 — 1을 넘겨 실어도 더 세지지 않는다.
            Assert.AreEqual(Speed(1f), Speed(2f), 1e-3f);
        }


        //  손가락이 순간이동해도 활은 못 그런다 — 시위가 차오르는 데 최소 시간이 걸린다.
        [Test]
        public void 시위는_정해진_속도보다_빨리_당겨지지_않는다()
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem(NoSwayConfig());
            var aim = archer.Get<ArcheryAim>();

            //  첫 틱은 "안 당기다가 당기기 시작한" 그 틱이라 0에서 다시 출발한다(Important 1
            //  회귀 방지 — 새 당김의 시작 값). 그 첫 틱부터 램프가 돈다.
            Feed(archer, 0f, 0f, drawing: true, release: false, drawRatio: 1f);
            system.Tick(archer, 100, TickInterval);

            float perTick = ArcheryAimSystem.DrawRisePerSecond * TickInterval;
            Assert.AreEqual(perTick, aim.DrawRatio, 1e-4f, "당기기 시작한 첫 틱도 한 틱분 상승해야 한다");

            Feed(archer, 0f, 0f, drawing: true, release: false, drawRatio: 1f);
            system.Tick(archer, 101, TickInterval);
            Assert.AreEqual(perTick * 2f, aim.DrawRatio, 1e-4f);
        }

        //  Important 1 회귀 방지: 쏘고 나서 남은 당김(aim.DrawRatio)이 바로 이어 누른 다음
        //  당김의 시작값으로 새면 안 된다. 새면 버스트로 짧게 탭만 해도 두 번째 화살이 첫
        //  화살과 거의 같은 힘(MaxSpeed 근처)으로 나간다 — 0.5초 당김의 대가를 첫 발만 치르는
        //  셈이 된다. 고쳤다면 짧게 당긴 두 번째 발은 MinSpeed 쪽 낮은 속도여야 한다.
        [Test]
        public void 쏘고_바로_다시_당기면_두_번째_화살은_처음부터_다시_당긴다()
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem(NoSwayConfig());

            //  첫 발: 만작까지 잡고 있다가 놓아 쏜다. 당기기 시작한 첫 틱은 0에서 다시
            //  출발하므로(Important 1) 만작(25틱분)에 여유를 두고 30틱 잡는다.
            for (long t = 100; t < 130; t++)
            {
                Feed(archer, 0f, 0f, drawing: true, release: false, drawRatio: 1f);
                system.Tick(archer, t, TickInterval);
            }
            Feed(archer, 0f, 0f, drawing: false, release: true, drawRatio: 1f);
            var firstShot = system.Tick(archer, 130, TickInterval);
            Assert.IsNotNull(firstShot);
            Assert.AreEqual(ArcheryAimSystem.MaxSpeed, firstShot.Value.Velocity.magnitude, Tolerance,
                "첫 발은 만작이어야 한다");

            //  바로 다시 눌렀다가 짧게(임계치를 겨우 넘길 만큼만) 잡고 놓는다 — 버스트 탭.
            for (long t = 131; t < 140; t++)
            {
                Feed(archer, 0f, 0f, drawing: true, release: false, drawRatio: 1f);
                system.Tick(archer, t, TickInterval);
            }
            Feed(archer, 0f, 0f, drawing: false, release: true, drawRatio: 1f);
            var secondShot = system.Tick(archer, 140, TickInterval);

            Assert.IsNotNull(secondShot, "임계치는 넘겼으니 취소가 아니라 발사여야 한다");
            //  잔여 당김이 샜다면 이 두 번째 발도 MaxSpeed 근처로 나간다 — 절반 당김 속도보다도
            //  느려야 "처음부터 다시 당겼다"는 증거다.
            float halfDrawSpeed = ArcheryAimSystem.SpeedFor(0.5f);
            Assert.Less(secondShot.Value.Velocity.magnitude, halfDrawSpeed,
                "두 번째 화살이 여전히 빠르다 — 첫 발의 잔여 당김이 새고 있다는 뜻이다");
        }

        //  Important 2 회귀 방지: 서버는 클라가 보낸 DrawRatio 크기를 그대로 믿지 않는다.
        //  변조된 클라가 50 같은 큰 값을 계속 실어도 aim.DrawRatio는 절대 1을 넘으면 안 된다 —
        //  넘으면 그 뒤 놓을 때마다 몇 초간 매 탭이 만작으로 나간다.
        [Test]
        public void 조작된_클라가_큰_당김값을_보내도_1을_넘지_않는다()
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem(NoSwayConfig());
            var aim = archer.Get<ArcheryAim>();

            for (long t = 100; t < 200; t++)
            {
                Feed(archer, 0f, 0f, drawing: true, release: false, drawRatio: 50f);
                system.Tick(archer, t, TickInterval);
                Assert.LessOrEqual(aim.DrawRatio, 1f, $"틱 {t}에서 aim.DrawRatio가 1을 넘었다");
            }

            Assert.AreEqual(1f, aim.DrawRatio, Tolerance);
        }

        //  풀리는 속도는 당길 때보다 느리다 — 쏘는 순간 0으로 떨어뜨리면 화각이 한 프레임에
        //  벌어져 화면이 튄다. 이게 없으면 화면이 각자 완충을 대야 한다.
        [Test]
        public void 손을_떼면_시위가_정해진_속도로_풀린다()
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem(NoSwayConfig());
            var aim = archer.Get<ArcheryAim>();

            //  완전히 당길 때까지 충분히 먹인다.
            for (long t = 100; t < 140; t++)
            {
                Feed(archer, 0f, 0f, drawing: true, release: false, drawRatio: 1f);
                system.Tick(archer, t, TickInterval);
            }
            Assert.AreEqual(1f, aim.DrawRatio, 1e-4f, "충분히 당겼으면 최대여야 한다");

            //  떼는 틱: 화살은 나가되 시위는 한 번에 0이 되지 않는다.
            Feed(archer, 0f, 0f, drawing: false, release: true, drawRatio: 0f);
            Assert.IsNotNull(system.Tick(archer, 140, TickInterval), "떼면 화살은 나가야 한다");

            float perTick = ArcheryAimSystem.DrawFallPerSecond * TickInterval;
            Assert.AreEqual(1f - perTick, aim.DrawRatio, 1e-4f, "시위는 한 틱분만 풀려야 한다");

            //  손가락이 없어도 계속 풀린다.
            Feed(archer, 0f, 0f, drawing: false, release: false, drawRatio: 0f);
            system.Tick(archer, 141, TickInterval);
            Assert.AreEqual(1f - perTick * 2f, aim.DrawRatio, 1e-4f);
        }

        //  떼는 틱에도 시위가 한 틱분 풀린다 — 그 깎인 값으로 쏘면 놓을 때마다 힘이 모자란다.
        [Test]
        public void 쏘는_힘은_시위가_풀리기_전_값이다()
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem(NoSwayConfig());

            for (long t = 100; t < 140; t++)
            {
                Feed(archer, 0f, 0f, drawing: true, release: false, drawRatio: 1f);
                system.Tick(archer, t, TickInterval);
            }

            Feed(archer, 0f, 0f, drawing: false, release: true, drawRatio: 0f);
            var shot = system.Tick(archer, 140, TickInterval);

            Assert.AreEqual(ArcheryAimSystem.MaxSpeed, shot.Value.Velocity.magnitude, 1e-3f);
        }


        //  사람이 엄지로 끝까지 끄는 데 0.18초쯤 걸린다 — 그 손동작이 임계치(0.3=0.15초)를
        //  확실히 넘겨야 한다. 당김 상한이 너무 크면 끝까지 끌었는데도 안 나가서 "왜 안 쏴지지"가
        //  된다. 당기기 시작한 첫 틱은 0에서 다시 출발하므로(Important 1) 그만큼 여유를 둔다.
        [Test]
        public void 빠르게_끌었다_놓아도_발사된다()
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem(NoSwayConfig());

            //  0.18초(9틱) 동안 끝까지 끈 손가락.
            for (long t = 100; t < 109; t++)
            {
                Feed(archer, 0f, 0f, drawing: true, release: false, drawRatio: 1f);
                system.Tick(archer, t, TickInterval);
            }

            Feed(archer, 0f, 0f, drawing: false, release: true, drawRatio: 0f);
            Assert.IsNotNull(system.Tick(archer, 109, TickInterval),
                             "0.18초면 사람이 끝까지 끄는 시간이다 — 이걸로 안 나가면 임계치가 너무 크다");
        }

        //  흔들림 배선의 생명줄 — ArcheryShake는 잘 만들어져 있어도 ArcheryAimSystem이 실제로
        //  불러 방향에 섞지 않으면 아무 효과가 없다(이 슬라이스가 고치는 바로 그 문제). 같은
        //  조준·같은 사람이라도 얼마나 오래 당겼는지가 다르면 위상이 달라 발사 방향도 달라야
        //  이 배선이 살아있다는 뜻이다.
        [Test]
        public void 오래_당길수록_흔들림_위상이_달라_발사_방향이_달라진다()
        {
            var config = new ArcheryConfig(
                wavePeriodTicks: 100, minTargets: 1, maxTargets: 1,
                spawnRadius: 1f, spawnMinY: 0f, spawnMaxY: 1f, minSeparation: 1f,
                trapRatioMin: 0f, trapRatioMax: 0f,
                shakeFreeSeconds: 0.5f, shakeRampSeconds: 1f, shakeMaxDegrees: 5f,
                riseHeightMin: 0.1f, riseHeightMax: 0.1f, staggerTicks: 1, restTicks: 1,
                kinds: null);

            Vector3 ShootAfterHolding(long releaseTick)
            {
                var archer = Archer(Vector3.zero);
                var system = new ArcheryAimSystem(config);

                //  실제로 그만큼 당긴 시간 동안 매 틱 잡고 있어야 임계치를 넘겨 발사된다
                //  (시위가 시간을 두고 차오르므로 한 틱만으론 안 된다).
                for (long t = 0; t < releaseTick; t++)
                {
                    Feed(archer, 0f, 0f, drawing: true, release: false);
                    system.Tick(archer, t, TickInterval);   // 당기기 시작 — DrawStartTick=0
                }
                Feed(archer, 0f, 0f, drawing: false, release: true);
                return system.Tick(archer, releaseTick, TickInterval).Value.Velocity.normalized;
            }

            //  자유 구간(0.5초=25틱)을 지나야 흔들리기 시작한다 — 막 지난 시점과 한참 지난
            //  시점을 비교해 위상이 크게 벌어지게 한다.
            var releasedEarly = ShootAfterHolding(30);    // 0.6초 당김
            var releasedLate = ShootAfterHolding(150);    // 3.0초 당김

            Assert.Greater(Vector3.Distance(releasedEarly, releasedLate), 1e-3f,
                "같은 조준·같은 사람인데 당긴 시간만 다르면 흔들림 위상이 달라 방향도 달라야 한다 " +
                "— 같으면 ArcheryAimSystem이 ArcheryShake를 안 부르고 있다는 뜻이다");
        }

        //  조준 가이드선(ArcheryAimGuideView)은 실제 발사와 다른 클래스에서 방향을 만든다 —
        //  둘이 갈라지지 않는다는 계약은 "같은 입력을 같은 공유 함수(DirectionFor)에 넣으면
        //  같은 답이 나온다"는 것뿐이다. 가이드선을 여기서 직접 실행할 순 없지만(Unity 뷰),
        //  가이드선이 하는 일 — DirectionFor를 부르는 것 — 을 그대로 흉내 내 Tick이 실제로
        //  만든 발사와 비교하면 그 계약을 잰다. DirectionFor를 공유하기 전엔 이 시험이
        //  존재하지 않았다 — 그때는 "같은 입력"을 양쪽에 똑같이 넣는 것 자체가 두 벌의 산수를
        //  베껴 적는 일이라, 시험이 있어도 실수를 못 잡았을 것이다.
        [Test]
        public void 가이드선이_DirectionFor를_같은_입력으로_불렀다면_실제_발사와_같은_방향이다()
        {
            var config = new ArcheryConfig(
                wavePeriodTicks: 100, minTargets: 1, maxTargets: 1,
                spawnRadius: 1f, spawnMinY: 0f, spawnMaxY: 1f, minSeparation: 1f,
                trapRatioMin: 0f, trapRatioMax: 0f,
                shakeFreeSeconds: 0.5f, shakeRampSeconds: 1f, shakeMaxDegrees: 5f,
                riseHeightMin: 0.1f, riseHeightMax: 0.1f, staggerTicks: 1, restTicks: 1,
                kinds: null);

            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem(config);

            //  임계치를 넘길 때까지 매 틱 잡고 있는다 — 한 틱만으론 발사되지 않는다.
            for (long t = 0; t < 100; t++)
            {
                Feed(archer, 30f, -10f, drawing: true, release: false);
                system.Tick(archer, t, TickInterval);   // 당기기 시작 — DrawStartTick=0
            }
            //  가이드선이 읽는 것과 같은 값: 시위가 풀리기 시작하기 전, 놓는 순간의 당김.
            float drawAtRelease = archer.Get<ArcheryAim>().DrawRatio;
            Feed(archer, 30f, -10f, drawing: false, release: true);
            var shot = system.Tick(archer, 100, TickInterval);   // 2.0초 당김
            Assert.IsTrue(shot.HasValue);

            //  가이드선이 매 프레임 하는 일 그대로: 같은 조준·같은 당긴 시간·같은 당김·같은
            //  사람·같은 설정을 DirectionFor에 직접 넣는다.
            float heldSeconds = ArcheryAimSystem.HeldSeconds(0, 100, TickInterval);
            int phaseSeed = ArcheryShake.PhaseSeedOf("archer-1");
            var guideDirection = ArcheryAimSystem.DirectionFor(30f, -10f, heldSeconds, drawAtRelease, phaseSeed, config);

            //  완전히 당긴 상태(speed=MaxSpeed)에서는 "단위벡터 × speed" 후 다시 정규화하는
            //  왕복에서 부동소수점 맨 끝자리가 흔들릴 수 있다 — 그건 이 계약(같은 함수를
            //  같은 입력으로 불렀나)과 무관한 잡음이라 미세 허용오차로 비교한다.
            Assert.Less(Vector3.Distance(guideDirection, shot.Value.Velocity.normalized), 1e-5f,
                "실제 발사 방향과 가이드선이 만든 방향이 달라졌다 — DirectionFor가 더 이상 " +
                "공유되지 않거나 입력(당긴 시간·위상·설정)이 어긋났다는 뜻이다");
        }

        //  잡고 있는 동안 화면이 보내는 값은 늘 1이다 — 얼마나 당겨졌는지는 시뮬이 정한다.
        //  같은 **경과 시간**이면 틱 간격이 달라도 같은 값이 나와야 한다. 이게 이 변경의 계약이다:
        //  화면이 제 시계로 램프를 계산하면 프레임레이트가 다른 두 기기가 다른 화살을 쏜다.
        static float RampFor(float seconds, float tickInterval)
        {
            var archer = Archer(Vector3.zero);
            var system = new ArcheryAimSystem(NoSwayConfig());
            int ticks = Mathf.RoundToInt(seconds / tickInterval);
            for (int i = 0; i < ticks; i++)
            {
                Feed(archer, 0f, 0f, drawing: true, release: false, drawRatio: 1f);
                system.Tick(archer, 100 + i, tickInterval);
            }
            return archer.Get<ArcheryAim>().DrawRatio;
        }

        [Test]
        public void 같은_시간이면_틱_간격이_달라도_같은_힘이_된다()
        {
            //  50Hz와 30Hz — 같은 0.3초를 잡고 있었으면 정확히 같은 곳까지 당겨져 있어야 한다.
            //  0.3초를 고른 이유: 양쪽에서 정확히 15틱/9틱으로 떨어져 반올림 차가 안 생긴다.
            //
            //  당기기 시작한 첫 틱은 0에서 다시 출발하고 **그 틱부터 램프가 돈다**(이전 수정).
            //  따라서 두 틱레이트 모두 같은 경과시간을 같은 틱 수로 나누어(반올림 무시하고)
            //  정확히 같은 상승을 누적한다. 양쪽이 모두 0.3s = 15틱(50Hz) = 9틱(30Hz)일 때,
            //  각각 15 * 0.04 = 30 * (1/30 * 2) = 0.6의 같은 값에 도달한다.
            const float tolerance = 1e-3f;
            Assert.AreEqual(RampFor(0.3f, 1f/50f), RampFor(0.3f, 1f/30f), tolerance,
                "틱 간격이 달라도 같은 경과시간이면 같은 당김이어야 한다");
        }

        [Test]
        public void 만작까지_정해진_시간이_걸린다()
        {
            Assert.Less(RampFor(ArcheryAimSystem.FullDrawSeconds * 0.5f, TickInterval), 0.75f,
                "절반만 잡고 있었는데 거의 만작이다 — 램프가 너무 빠르다");
            Assert.AreEqual(1f, RampFor(ArcheryAimSystem.FullDrawSeconds + 0.05f, TickInterval), 1e-3f,
                "만작 시간을 넘겼는데 아직 1이 아니다");
        }

        [Test]
        public void 램프_속도는_만작_시간에서_유도된다()
        {
            //  두 상수가 서로 다른 만작 시간을 말하면 안 된다. 값을 바꿔도 이 관계는 남아야 한다.
            Assert.AreEqual(1f / ArcheryAimSystem.FullDrawSeconds,
                            ArcheryAimSystem.DrawRisePerSecond, 1e-5f);
        }
    }
}
