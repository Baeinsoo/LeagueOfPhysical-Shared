using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// <see cref="FlappyBodyShape.For"/> — BodyLength가 지름보다 크면 누운(X) 캡슐,
    /// 아니면 지금까지대로 선(Y) 캡슐을 뽑아야 한다. 클·서 FlappyBirdCreator가 공유하는
    /// 이 한 식이 틀리면 둘 중 하나가 다른 몸을 세운다.
    /// </summary>
    public class FlappyBodyShapeTests
    {
        private static FlappyConfig Config(float bodyLength)
            => new FlappyConfig(forwardSpeed: 4.5f, flapImpulse: 10.125f, gravity: 33.75f, maxFallSpeed: 11.25f,
                                bodyRadius: 0.45f, bodyHeight: 0.9f, restitution: 0.35f,
                                stunTime: 0.8f, invulnTime: 0.6f,
                                dashMult: 2f, dashDuration: 0.2f, dashChargeBase: 0.13f, dashChargeDive: 1.2f,
                                bodyLength: bodyLength);

        [Test]
        public void BodyLength가_1_28이면_누운_캡슐이다()
        {
            CapsuleShape shape = FlappyBodyShape.For(Config(1.28f));

            Assert.That(shape.Axis, Is.EqualTo(CapsuleAxis.X));
            Assert.That(shape.Length, Is.EqualTo(1.28f));
            Assert.That(shape.Height, Is.EqualTo(0.9f));
        }

        [Test]
        public void BodyLength가_0이면_선_캡슐이다()
        {
            CapsuleShape shape = FlappyBodyShape.For(Config(0f));

            Assert.That(shape.Axis, Is.EqualTo(CapsuleAxis.Y));
            Assert.That(shape.Height, Is.EqualTo(0.9f));
        }
    }
}
