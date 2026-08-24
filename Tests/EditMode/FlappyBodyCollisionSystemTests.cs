using System.Collections.Generic;
using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class FlappyBodyCollisionSystemTests
    {
        const float Tolerance = 1e-4f;

        static FlappyConfig Config()
            => new FlappyConfig(forwardSpeed: 11f, flapImpulse: 23f, gravity: 70f, maxFallSpeed: 30f,
                                bodyRadius: 0.45f, bodyHeight: 0.9f, restitution: 0.35f,
                                ghostTime: 0.8f, invulnTime: 0.6f);

        static Entity Bird(string id, Vector3 position, Vector3 velocity)
        {
            var entity = new Entity(id);
            entity.Add(new GameFramework.World.Transform { Position = position.ToNumerics() });
            entity.Add(new Velocity { Linear = velocity.ToNumerics() });
            return entity;
        }

        static Vector3 PositionOf(Entity e) => e.Get<GameFramework.World.Transform>().Position.ToUnity();
        static Vector3 VelocityOf(Entity e) => e.Get<Velocity>().Linear.ToUnity();

        [Test]
        public void 겹친_두_새가_절반씩_갈라진다()
        {
            var lower = Bird("bird-1", Vector3.zero, Vector3.zero);
            var upper = Bird("bird-2", new Vector3(0f, 0.5f, 0f), Vector3.zero);

            new FlappyBodyCollisionSystem(Config()).Resolve(new List<Entity> { lower, upper });

            // 겹침 0.4에서 허용 겹침 0.01을 뺀 0.39를 반씩 나눠 갖는다
            Assert.AreEqual(-0.195f, PositionOf(lower).y, Tolerance);
            Assert.AreEqual(0.695f, PositionOf(upper).y, Tolerance);
        }

        [Test]
        public void 부딪힌_세로_속도를_주고받는다()
        {
            var lower = Bird("bird-1", Vector3.zero, Vector3.zero);
            var upper = Bird("bird-2", new Vector3(0f, 0.5f, 0f), new Vector3(0f, -10f, 0f));

            new FlappyBodyCollisionSystem(Config()).Resolve(new List<Entity> { lower, upper });

            // FlappyBounce와 같은 값 — 위는 덜 떨어지고 아래는 더 밀린다
            Assert.AreEqual(-3.25f, VelocityOf(upper).y, Tolerance);
            Assert.AreEqual(-6.75f, VelocityOf(lower).y, Tolerance);
        }

        [Test]
        public void 안_겹친_새는_건드리지_않는다()
        {
            var a = Bird("bird-1", Vector3.zero, new Vector3(0f, -10f, 0f));
            var b = Bird("bird-2", new Vector3(0f, 5f, 0f), Vector3.zero);

            new FlappyBodyCollisionSystem(Config()).Resolve(new List<Entity> { a, b });

            Assert.AreEqual(Vector3.zero, PositionOf(a));
            Assert.AreEqual(-10f, VelocityOf(a).y, Tolerance);
            Assert.AreEqual(0f, VelocityOf(b).y, Tolerance);
        }

        [Test]
        public void 두_새가_같은_충돌을_각자_보고_계산한다()
        {
            // 한쪽을 먼저 고쳐 놓고 다른 쪽이 그 새 값을 보면 순서가 결과를 바꾼다.
            // 목록 순서를 뒤집어도 결과가 같아야 클·서가 갈리지 않는다.
            var forward = new List<Entity>
            {
                Bird("bird-1", Vector3.zero, Vector3.zero),
                Bird("bird-2", new Vector3(0f, 0.5f, 0f), new Vector3(0f, -10f, 0f)),
            };
            var reversed = new List<Entity>
            {
                Bird("bird-2", new Vector3(0f, 0.5f, 0f), new Vector3(0f, -10f, 0f)),
                Bird("bird-1", Vector3.zero, Vector3.zero),
            };

            new FlappyBodyCollisionSystem(Config()).Resolve(forward);
            new FlappyBodyCollisionSystem(Config()).Resolve(reversed);

            Assert.AreEqual(VelocityOf(forward[0]).y, VelocityOf(reversed[1]).y, Tolerance);
            Assert.AreEqual(VelocityOf(forward[1]).y, VelocityOf(reversed[0]).y, Tolerance);
            Assert.AreEqual(PositionOf(forward[0]).y, PositionOf(reversed[1]).y, Tolerance);
            Assert.AreEqual(PositionOf(forward[1]).y, PositionOf(reversed[0]).y, Tolerance);
        }
    }
}
