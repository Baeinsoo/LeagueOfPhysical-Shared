using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class KnockbackBarrelPhysicsTests
    {
        const float Dt = 0.02f;

        [Test]
        public void 마찰은_속도를_줄이고_0에서_멈춘다()
        {
            var v = new Vector2(1f, 0f);
            v = KnockbackBarrelPhysics.ApplyFriction(v, 2.5f, Dt);
            Assert.AreEqual(0.95f, v.x, 1e-5f, "한 틱에 friction*dt만큼 준다");

            var slow = KnockbackBarrelPhysics.ApplyFriction(new Vector2(0.01f, 0f), 2.5f, Dt);
            Assert.AreEqual(Vector2.zero, slow, "0 아래로(반대 방향으로) 가지 않는다");
            Assert.AreEqual(Vector2.zero, KnockbackBarrelPhysics.ApplyFriction(Vector2.zero, 2.5f, Dt));
        }

        [Test]
        public void 기둥에_부딪히면_밖으로_밀려나고_속도가_반발계수로_되튄다()
        {
            var pos = new Vector2(1.0f, 0f);
            var vel = new Vector2(-4f, 0f);
            KnockbackBarrelPhysics.BounceOffFixedCircle(ref pos, ref vel, Vector2.zero, 1.5f, 0.6f);
            Assert.AreEqual(1.5f, pos.x, 1e-5f, "겹침을 푼다");
            Assert.AreEqual(2.4f, vel.x, 1e-4f, "들어오던 속도의 0.6배로 되튄다");
        }

        [Test]
        public void 기둥에서_멀어지는_중이면_속도를_건드리지_않는다()
        {
            var pos = new Vector2(1.0f, 0f);
            var vel = new Vector2(3f, 0f);
            KnockbackBarrelPhysics.BounceOffFixedCircle(ref pos, ref vel, Vector2.zero, 1.5f, 0.6f);
            Assert.AreEqual(3f, vel.x, 1e-5f);
        }

        [Test]
        public void 두_드럼통은_겹침을_반씩_풀고_속도를_주고받는다()
        {
            var pa = new Vector2(0f, 0f);
            var pb = new Vector2(1.0f, 0f);
            var va = new Vector2(4f, 0f);
            var vb = Vector2.zero;
            KnockbackBarrelPhysics.BouncePair(ref pa, ref va, ref pb, ref vb, 1.2f, 0.6f);

            Assert.AreEqual(-0.1f, pa.x, 1e-5f);
            Assert.AreEqual(1.1f, pb.x, 1e-5f);
            Assert.AreEqual(4f, va.x + vb.x, 1e-4f, "같은 무게라 운동량 합이 그대로");
            Assert.AreEqual(0.6f * 4f, vb.x - va.x, 1e-4f, "떨어지는 상대 속도 = 반발 계수 × 다가오던 속도");
            Assert.Greater(vb.x, va.x);
        }

        [Test]
        public void 서로_멀어지는_드럼통은_겹침만_풀고_속도는_그대로다()
        {
            var pa = new Vector2(0f, 0f);
            var pb = new Vector2(1.0f, 0f);
            var va = new Vector2(-2f, 0f);
            var vb = new Vector2(3f, 0f);
            KnockbackBarrelPhysics.BouncePair(ref pa, ref va, ref pb, ref vb, 1.2f, 0.6f);
            Assert.AreEqual(-2f, va.x, 1e-5f);
            Assert.AreEqual(3f, vb.x, 1e-5f);
        }

        [Test]
        public void 다가오는_드럼통은_몸을_드럼에서_몸_쪽으로_세기는_상대속도에_배율로_친다()
        {
            bool hit = KnockbackBarrelPhysics.TryHitBody(Vector2.zero, new Vector2(6f, 0f), new Vector2(1f, 0f), new Vector2(-1f, 0f),
                                                         1.1f, 1.5f, out var dir, out float strength);
            Assert.IsTrue(hit);
            Assert.AreEqual(1f, dir.x, 1e-5f);
            Assert.AreEqual((6f + 1f) * 1.5f, strength, 1e-4f, "상대 접근 속도 7 × 1.5");
        }

        [Test]
        public void 다가오지_않거나_멀거나_너무_느리면_안_친다()
        {
            // 멀어지는 몸
            Assert.IsFalse(KnockbackBarrelPhysics.TryHitBody(Vector2.zero, new Vector2(6f, 0f), new Vector2(1f, 0f), new Vector2(8f, 0f),
                                                             1.1f, 1f, out _, out _));
            // 닿지 않는 거리
            Assert.IsFalse(KnockbackBarrelPhysics.TryHitBody(Vector2.zero, new Vector2(6f, 0f), new Vector2(3f, 0f), Vector2.zero,
                                                             1.1f, 1f, out _, out _));
            // 거의 선 드럼통
            Assert.IsFalse(KnockbackBarrelPhysics.TryHitBody(Vector2.zero, new Vector2(0.2f, 0f), new Vector2(1f, 0f), Vector2.zero,
                                                             1.1f, 1f, out _, out _));
        }

        [Test]
        public void 친_드럼통은_몸의_새_속도보다_몸_쪽으로_빠르지_않다_옆_성분은_그대로()
        {
            var v = KnockbackBarrelPhysics.YieldToBody(new Vector2(6f, 2f), Vector2.right, 4f);
            Assert.AreEqual(4f, v.x, 1e-5f);
            Assert.AreEqual(2f, v.y, 1e-5f);

            var slower = KnockbackBarrelPhysics.YieldToBody(new Vector2(3f, 0f), Vector2.right, 4f);
            Assert.AreEqual(3f, slower.x, 1e-5f, "이미 느리면 그대로");
        }

        [Test]
        public void ApplyPush는_속도와_날림만_넣고_위험도는_안_올린다()
        {
            var config = new KnockbackConfig(120f, 6f, 10f, 12f, 2f, 0.5f, 5f, 11f, 30f, 18f, 0.3f, 0.55f, 3, 0.95f, 0.26f, 1.2f, 12, 6f, 0.2f, 0.5f, 0.3f, 0.1f, 0.2f, 0.8f, 8f,
                                              new System.Collections.Generic.List<MountedArcheryZoneStageConfig>());
            var body = new KnockbackBody { DamagePercent = 20 };
            var velocity = new GameFramework.World.Velocity();

            KnockbackRules.ApplyPush(body, velocity, Vector2.right, 5f, 100, config);

            Assert.AreEqual(20, body.DamagePercent, "위험도는 그대로");
            Assert.AreEqual(5f, velocity.Linear.X, 1e-4f);
            Assert.AreEqual(KnockbackRules.StunEndTick(body, 100, config), body.KnockbackEndTick);
            Assert.Greater(body.KnockbackEndTick, 100);
        }
    }
}
