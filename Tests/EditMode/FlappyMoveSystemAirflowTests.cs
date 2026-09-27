using System.Linq;
using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 게임 쪽 이동이 기류를 실제로 읽는지. 커널(<see cref="FlappyVerticalKernel"/>)의 공식은
    /// 따로 시험되지만, 이동 시스템이 그 커널에 기류를 안 넘기면 검사기만 기류를 알고 게임은 모른다.
    /// </summary>
    public class FlappyMoveSystemAirflowTests
    {
        const float Tolerance = 1e-4f;
        const float Dt = 0.02f;
        const float Gravity = 70f, UpAccel = 30f, RiseCap = 12f;
        const float X0 = 10f;

        static FlappyConfig Config()
            => new FlappyConfig(forwardSpeed: 11f, flapImpulse: 23f, gravity: Gravity, maxFallSpeed: 30f,
                                bodyRadius: 0.45f, bodyHeight: 0.9f, restitution: 0.35f,
                                stunTime: 0.8f, invulnTime: 0.6f,
                                dashMult: 2f, dashDuration: 0.2f, dashChargeBase: 0.13f, dashChargeDive: 1.2f,
                                airflowUpAccel: UpAccel, airflowRiseCap: RiseCap, shaftGravityMult: 2f);

        static FlappyAirflowField Updraft()
        {
            var field = new FlappyAirflowField();
            field.Add(new FlappyAirflowRect(X0, X0 + 7f, -5f, 5f, FlappyAirflowKind.Up));
            return field;
        }

        static Entity Bird(float x, float vy)
        {
            var entity = new Entity("bird-1");
            entity.Add(new GameFramework.World.Transform { Position = new System.Numerics.Vector3(x, 0f, 0f) });
            entity.Add(new Velocity { Linear = new Vector3(11f, vy, 0f).ToNumerics() });
            var buffer = new InputBuffer();
            buffer.Current = new InputCommand { Jump = false };
            entity.Add(buffer);
            return entity;
        }

        static float VerticalOf(Entity entity) => entity.Get<Velocity>().Linear.ToUnity().y;

        [Test]
        public void 상승기류_안의_새는_중력_대신_기류_가속을_받는다()
        {
            var bird = Bird(X0 + 2f, vy: -2f);

            new FlappyMoveSystem(Config(), Updraft()).Tick(bird, Dt, dashing: false, finished: false);

            Assert.AreEqual(-2f + UpAccel * Dt, VerticalOf(bird), Tolerance);
        }

        [Test]
        public void 기류는_움직이기_전_자리로_고른다()
        {
            //  이번 틱에 0.22m 전진하니 움직인 뒤라면 기류 안이다 — 그래도 움직이기 전 자리(밖)로
            //  골라야 한다. 검사기 탐색·재생이 같은 자리를 보므로, 여기가 갈리면 클·검사기가 갈린다.
            Assert.That(Config().ForwardSpeed * Dt, Is.GreaterThan(0.1f));
            var bird = Bird(X0 - 0.1f, vy: -2f);

            new FlappyMoveSystem(Config(), Updraft()).Tick(bird, Dt, dashing: false, finished: false);

            Assert.AreEqual(-2f - Gravity * Dt, VerticalOf(bird), Tolerance);
        }

        [Test]
        public void DI는_기류를_받는_생성자를_쓴다()
        {
            //  기류 없는 생성자가 주입되면 게임에서만 기류가 조용히 꺼진다.
            var injected = typeof(FlappyMoveSystem).GetConstructors()
                .Single(c => c.GetCustomAttributes(typeof(VContainer.InjectAttribute), false).Length > 0);
            CollectionAssert.AreEqual(new[] { typeof(FlappyConfig), typeof(FlappyAirflowField) },
                                      injected.GetParameters().Select(p => p.ParameterType).ToArray());
        }
    }
}
