using NUnit.Framework;

namespace LOP.Tests
{
    // 클라 되감기는 IWorld.LoadState(tick)으로 그 틱 상태로 돌아간 뒤 다시 굴린다.
    // 위치·속도는 WorldBase가 되돌리지만, 유령정지 타이머까지 되돌아가지 않으면
    // 재생 중 새가 "이미 풀린 줄 알고" 움직여 예측이 어긋난다.
    public class FlappyWorldSaveStateTests
    {
        [Test]
        public void 되감으면_유령_타이머도_그_틱으로_돌아간다()
        {
            var world = FlappyWorldFixture.Create(new FlappyWorldFixture.AlwaysHit(), out var bird);

            world.Tick(1, 0.02f);        // 유령 진입
            world.SaveState(1);
            float atSave = bird.Get<FlappyGhost>().Remaining;

            for (long t = 2; t <= 10; t++) { world.Tick(t, 0.02f); world.SaveState(t); }
            Assert.That(bird.Get<FlappyGhost>().Remaining, Is.LessThan(atSave));   // 줄었다

            Assert.That(world.LoadState(1), Is.True);

            Assert.That(bird.Get<FlappyGhost>().Remaining, Is.EqualTo(atSave).Within(0.0001f));
        }

        [Test]
        public void 되감으면_무적_타이머도_그_틱으로_돌아간다()
        {
            // 위 테스트는 Remaining(유령정지)만 확인한다 — InvulnRemaining(무적)을
            // FlappySavedState.Capture/RestoreTo에서 지워도 이 파일은 계속 초록불이었다.
            // 그래서 무적 구간에 저장/복원해 InvulnRemaining도 round-trip하는지 따로 확인한다.
            var world = FlappyWorldFixture.Create(new FlappyWorldFixture.AlwaysHit(), out var bird);

            // 유령정지(0.8초)가 다 지나 무적(0.6초)으로 넘어가는 첫 틱까지 굴린다 — 그 순간이
            // Remaining=0, InvulnRemaining>0이라 InvulnRemaining round-trip을 확인할 수 있는 지점이다.
            const long invulnEnterTick = 41;   // 0.02f * 40 = 0.8s(GhostTime)을 다 지난 첫 틱
            for (long t = 1; t <= invulnEnterTick; t++) { world.Tick(t, 0.02f); }
            world.SaveState(invulnEnterTick);
            float ghostAtSave = bird.Get<FlappyGhost>().Remaining;
            float invulnAtSave = bird.Get<FlappyGhost>().InvulnRemaining;
            Assert.That(ghostAtSave, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(invulnAtSave, Is.GreaterThan(0f));

            for (long t = invulnEnterTick + 1; t <= invulnEnterTick + 10; t++) { world.Tick(t, 0.02f); world.SaveState(t); }
            Assert.That(bird.Get<FlappyGhost>().InvulnRemaining, Is.LessThan(invulnAtSave));   // 줄었다

            Assert.That(world.LoadState(invulnEnterTick), Is.True);

            Assert.That(bird.Get<FlappyGhost>().Remaining, Is.EqualTo(ghostAtSave).Within(0.0001f));
            Assert.That(bird.Get<FlappyGhost>().InvulnRemaining, Is.EqualTo(invulnAtSave).Within(0.0001f));
        }
    }
}
