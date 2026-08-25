using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class FlappyMoveSystemTests
    {
        const float Tolerance = 1e-4f;

        // TbFlappyConfig 기본값
        static FlappyConfig Config()
            => new FlappyConfig(forwardSpeed: 11f, flapImpulse: 23f, gravity: 70f, maxFallSpeed: 30f,
                                bodyRadius: 0.45f, bodyHeight: 0.9f, restitution: 0.35f,
                                stunTime: 0.8f, invulnTime: 0.6f);

        static Entity Bird(Vector3 velocity, bool? jump = null)
        {
            var entity = new Entity("bird-1");
            entity.Add(new GameFramework.World.Transform());
            entity.Add(new Velocity { Linear = velocity.ToNumerics() });
            if (jump.HasValue)
            {
                var buffer = new InputBuffer();
                buffer.Current = new InputCommand { Jump = jump.Value };
                entity.Add(buffer);
            }
            return entity;
        }

        static Vector3 VelocityOf(Entity entity) => entity.Get<Velocity>().Linear.ToUnity();

        [Test]
        public void 중력이_세로_속도를_깎는다()
        {
            var bird = Bird(Vector3.zero, jump: false);

            new FlappyMoveSystem(Config()).Tick(bird, 0.1f);

            Assert.AreEqual(-7f, VelocityOf(bird).y, Tolerance);   // 70 × 0.1
        }

        [Test]
        public void 낙하_속도가_상한을_넘지_않는다()
        {
            var bird = Bird(new Vector3(0f, -30f, 0f), jump: false);

            new FlappyMoveSystem(Config()).Tick(bird, 0.1f);

            Assert.AreEqual(-30f, VelocityOf(bird).y, Tolerance);
        }

        [Test]
        public void 플랩은_낙하를_지우고_늘_같은_높이로_띄운다()
        {
            var falling = Bird(new Vector3(0f, -25f, 0f), jump: true);
            var rising = Bird(new Vector3(0f, 5f, 0f), jump: true);

            new FlappyMoveSystem(Config()).Tick(falling, 0.1f);
            new FlappyMoveSystem(Config()).Tick(rising, 0.1f);

            // 눌렀을 때의 세로 속도와 무관하게 같은 값 — 그래야 플랩 높이가 예측 가능하다
            Assert.AreEqual(23f, VelocityOf(falling).y, Tolerance);
            Assert.AreEqual(23f, VelocityOf(rising).y, Tolerance);
        }

        [Test]
        public void 전진_속도는_상수로_고정된다()
        {
            var bird = Bird(new Vector3(999f, 0f, 999f), jump: false);
            bird.Get<InputBuffer>().Current.Horizontal = 1f;   // 좌우 입력을 넣어도
            bird.Get<InputBuffer>().Current.Vertical = 1f;

            new FlappyMoveSystem(Config()).Tick(bird, 0.1f);

            Assert.AreEqual(11f, VelocityOf(bird).x, Tolerance);   // 전진은 조작 대상이 아니다
            Assert.AreEqual(0f, VelocityOf(bird).z, Tolerance);
        }

        [Test]
        public void 입력이_아예_없어도_중력과_전진은_돈다()
        {
            var bird = Bird(Vector3.zero);   // InputBuffer 없음 — 서버가 조종하지 않는 새

            new FlappyMoveSystem(Config()).Tick(bird, 0.1f);

            Assert.AreEqual(-7f, VelocityOf(bird).y, Tolerance);
            Assert.AreEqual(11f, VelocityOf(bird).x, Tolerance);
        }
    }
}
