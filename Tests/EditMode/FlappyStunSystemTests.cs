using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    public class FlappyStunSystemTests
    {
        private static FlappyConfig Config()
            => new FlappyConfig(5f, 6f, 20f, 30f, 0.35f, 1.5f, 0.5f, stunTime: 0.8f, invulnTime: 0.6f);

        private static Entity Bird()
        {
            var e = new Entity("bird");
            e.Add(new FlappyStun());
            return e;
        }

        [Test]
        public void 부딪히면_정지_시간이_찬다()
        {
            var system = new FlappyStunSystem(Config());
            var bird = Bird();

            system.Enter(bird);

            Assert.That(bird.Get<FlappyStun>().StunRemaining, Is.EqualTo(0.8f).Within(0.0001f));
            Assert.That(system.IsStunned(bird), Is.True);
        }

        [Test]
        public void 정지가_끝나면_무적으로_넘어간다()
        {
            var system = new FlappyStunSystem(Config());
            var bird = Bird();
            system.Enter(bird);

            for (int i = 0; i < 40; i++) system.Tick(bird, 0.02f);   // 0.8초

            var stun = bird.Get<FlappyStun>();
            Assert.That(stun.StunRemaining, Is.EqualTo(0f));
            Assert.That(stun.InvulnRemaining, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(system.IsStunned(bird), Is.False);
        }

        [Test]
        public void 무적_중에는_다시_걸리지_않는다()
        {
            var system = new FlappyStunSystem(Config());
            var bird = Bird();
            system.Enter(bird);
            for (int i = 0; i < 40; i++) system.Tick(bird, 0.02f);

            system.Enter(bird);   // 무적 중 재충돌

            Assert.That(system.IsStunned(bird), Is.False);
        }

        [Test]
        public void 무적이_끝나면_다시_걸린다()
        {
            var system = new FlappyStunSystem(Config());
            var bird = Bird();
            system.Enter(bird);
            for (int i = 0; i < 70; i++) system.Tick(bird, 0.02f);   // 0.8 + 0.6 초과

            system.Enter(bird);

            Assert.That(system.IsStunned(bird), Is.True);
        }

        [Test]
        public void 정지_중_재충돌은_시간을_늘리지_않는다()
        {
            var system = new FlappyStunSystem(Config());
            var bird = Bird();
            system.Enter(bird);
            system.Tick(bird, 0.4f);

            system.Enter(bird);

            Assert.That(bird.Get<FlappyStun>().StunRemaining, Is.EqualTo(0.4f).Within(0.0001f));
        }
    }
}
