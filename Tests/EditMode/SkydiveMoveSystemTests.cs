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
                spreadFallSpeed: 25f, diveFallSpeed: 45f, glideFallSpeed: 6f,
                spreadMoveSpeed: 12f, diveMoveSpeed: 18f, glideMoveSpeed: 14f,
                spreadTurnAccel: 22f, diveTurnAccel: 6f, glideTurnAccel: 18f,
                fallApproach: 30f, postureRate: 4f,
                bodyRadius: 0.4f, bodyHeight: 1.8f, groundY: 0f,
                staminaMax: 100f, glideDrain: 20f, groundRecover: 40f, emergencyGlideTime: 1f);

        static Entity Diver(float axis, bool gliding, Vector3 velocity, Vector3 position,
                            float h = 0f, float v = 0f)
        {
            var entity = new Entity("diver-1");
            entity.Add(new GameFramework.World.Transform { Position = position.ToNumerics() });
            entity.Add(new Velocity { Linear = velocity.ToNumerics() });
            entity.Add(new Posture { Axis = axis, Gliding = gliding });
            var buffer = new InputBuffer();
            buffer.Current = new InputCommand { Horizontal = h, Vertical = v };
            entity.Add(buffer);
            return entity;
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
        public void 임시_바닥에_닿으면_멈춘다()
        {
            var e = Diver(1f, false, new Vector3(0f, -40f, 0f), new Vector3(0f, 0.3f, 0f));

            new SkydiveMoveSystem().Tick(e, 0.1f, Config());

            Assert.AreEqual(0f, PositionOf(e).y, Tolerance);
            Assert.AreEqual(0f, VelocityOf(e).y, Tolerance);
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
