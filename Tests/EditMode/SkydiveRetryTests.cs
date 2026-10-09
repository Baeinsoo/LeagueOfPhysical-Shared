using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>별만 결승(사용자 10-07, A안) — 놓치고 구름 아래로 빠지면 출구 아래 공중에서 다시 떨어진다.</summary>
    public class SkydiveRetryTests
    {
        private static RetryField Field()
        {
            var f = new RetryField();
            f.Add(new RetryZone(belowY: -15f, point: new Vector3(0f, 270f, 0f)));
            return f;
        }

        private static Entity Diver(float y, bool finished = false)
        {
            var e = new Entity("d");
            e.Add(new GameFramework.World.Transform());
            e.Add(new Velocity());
            e.Add(new FinishState { FinishedTick = finished ? 5 : FinishState.NotFinished });
            e.Add(new Posture { Gliding = true, Axis = 1f });
            e.Add(new Stamina { Current = 3f });
            e.Teleport(new Vector3(10f, y, 0f));
            e.SetVelocity(new Vector3(0f, -60f, 0f));
            return e;
        }

        [Test]
        public void 구름_아래로_빠지면_다시_떨어질_자리를_준다()
        {
            Assert.IsTrue(SkydiveRetry.ShouldRetry(Diver(-20f), Field(), out var point));
            Assert.AreEqual(new Vector3(0f, 270f, 0f), point);
        }

        [Test]
        public void 아직_구름_위거나_이미_잡았으면_아니다()
        {
            Assert.IsFalse(SkydiveRetry.ShouldRetry(Diver(5f), Field(), out _));
            Assert.IsFalse(SkydiveRetry.ShouldRetry(Diver(-20f, finished: true), Field(), out _), "잡은 뒤엔 그냥 내려앉는다");
            Assert.IsFalse(SkydiveRetry.ShouldRetry(Diver(-20f), new RetryField(), out _), "다시 떨어질 자리가 없는 맵");
        }

        [Test]
        public void 다시_떨어질_때는_멈춘_채_스태미나_가득_대자로()
        {
            var d = Diver(-20f);
            int order = 0;
            SkydiveRespawn.ApplyAt(d, new Vector3(0f, 270f, 0f), 300f, ref order);
            var p = d.GetPosition();
            Assert.AreEqual(270f, p.y, 1e-3f);
            Assert.AreEqual(Vector3.zero, d.GetVelocity());
            Assert.AreEqual(300f, d.Get<Stamina>().Current);
            Assert.IsFalse(d.Get<Posture>().Gliding);
        }

        [Test]
        public void 다시_떨어지면_발밑이_비었다고_본다()
        {
            //  리뷰 3차: 서 있던 값이 남으면 다음 틱 판 실어 나르기가 순간이동한 자리 발밑을 "방금까지 서 있던 판"으로 오해한다.
            var d = Diver(-20f);
            d.Add(new GameFramework.World.GroundState { IsGrounded = true });
            int order = 0;
            SkydiveRespawn.ApplyAt(d, new Vector3(0f, 270f, 0f), 300f, ref order);
            Assert.IsFalse(d.Get<GameFramework.World.GroundState>().IsGrounded);
        }
    }
}
