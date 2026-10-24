using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    public class FlappyStunSystemTests
    {
        private static FlappyConfig Config()
            => new FlappyConfig(5f, 6f, 20f, 30f, 0.35f, 1.5f, 0.5f, stunTime: 0.8f, invulnTime: 0.6f,
                                dashMult: 2f, dashDuration: 0.2f, dashChargeBase: 0.13f, dashChargeDive: 1.2f);

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

        [Test]
        public void 기절이나_무적이면_통과_상태다()
        {
            var system = new FlappyStunSystem(Config());
            var bird = Bird();
            Assert.That(system.IsGhost(bird), Is.False);

            bird.Get<FlappyStun>().StunRemaining = 0.1f;
            Assert.That(system.IsGhost(bird), Is.True);

            bird.Get<FlappyStun>().StunRemaining = 0f;
            bird.Get<FlappyStun>().InvulnRemaining = 0.1f;
            Assert.That(system.IsGhost(bird), Is.True);
        }

        [Test]
        public void 무적을_붙들면_적어도_한_틱은_남는다()
        {
            var system = new FlappyStunSystem(Config());
            var bird = Bird();

            system.HoldInvuln(bird, 0.02f);   // 무적이 이미 0이었어도(이번 틱 감소로 끝난 새) 다시 한 틱
            Assert.That(bird.Get<FlappyStun>().InvulnRemaining, Is.EqualTo(0.02f).Within(0.0001f));

            //  넉넉히 남아 있으면 줄이지 않는다.
            bird.Get<FlappyStun>().InvulnRemaining = 0.5f;
            system.HoldInvuln(bird, 0.02f);
            Assert.That(bird.Get<FlappyStun>().InvulnRemaining, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void 기절_중엔_무적을_붙들지_않는다()
        {
            //  기절 중엔 이미 통과 상태고, 기절이 끝나는 틱에 Tick이 무적을 채운다 — 미리 건드릴 까닭이 없다.
            var system = new FlappyStunSystem(Config());
            var bird = Bird();
            system.Enter(bird);

            system.HoldInvuln(bird, 0.02f);

            Assert.That(bird.Get<FlappyStun>().InvulnRemaining, Is.EqualTo(0f));
        }
    }
}
