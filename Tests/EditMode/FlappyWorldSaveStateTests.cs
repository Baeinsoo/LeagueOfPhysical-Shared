using NUnit.Framework;

namespace LOP.Tests
{
    // 클라 되감기는 IWorld.LoadState(tick)으로 그 틱 상태로 돌아간 뒤 다시 굴린다.
    // 위치·속도는 WorldBase가 되돌리지만, 스턴 타이머까지 되돌아가지 않으면
    // 재생 중 새가 "이미 풀린 줄 알고" 움직여 예측이 어긋난다.
    public class FlappyWorldSaveStateTests
    {
        [Test]
        public void 되감으면_스턴_타이머도_그_틱으로_돌아간다()
        {
            var world = FlappyWorldFixture.Create(new FlappyWorldFixture.AlwaysHit(), out var bird);
            world.GameplayStartTick = 0;   // 이 파일은 되감기를 다룬다, 출발 게이트가 아니다

            world.Tick(1, 0.02f);        // 스턴 진입
            world.SaveState(1);
            float atSave = bird.Get<FlappyStun>().StunRemaining;

            for (long t = 2; t <= 10; t++) { world.Tick(t, 0.02f); world.SaveState(t); }
            Assert.That(bird.Get<FlappyStun>().StunRemaining, Is.LessThan(atSave));   // 줄었다

            Assert.That(world.LoadState(1), Is.True);

            Assert.That(bird.Get<FlappyStun>().StunRemaining, Is.EqualTo(atSave).Within(0.0001f));
        }

        [Test]
        public void 되감으면_무적_타이머도_그_틱으로_돌아간다()
        {
            // 위 테스트는 StunRemaining(스턴)만 확인한다 — InvulnRemaining(무적)을
            // FlappySavedState.Capture/RestoreTo에서 지워도 이 파일은 계속 초록불이었다.
            // 그래서 무적 구간에 저장/복원해 InvulnRemaining도 round-trip하는지 따로 확인한다.
            var world = FlappyWorldFixture.Create(new FlappyWorldFixture.AlwaysHit(), out var bird);
            world.GameplayStartTick = 0;   // 이 파일은 되감기를 다룬다, 출발 게이트가 아니다

            // 스턴(0.8초)이 다 지나 무적(0.6초)으로 넘어가는 첫 틱까지 굴린다 — 그 순간이
            // StunRemaining=0, InvulnRemaining>0이라 InvulnRemaining round-trip을 확인할 수 있는 지점이다.
            const long invulnEnterTick = 41;   // 0.02f * 40 = 0.8s(StunTime)을 다 지난 첫 틱
            for (long t = 1; t <= invulnEnterTick; t++) { world.Tick(t, 0.02f); }
            world.SaveState(invulnEnterTick);
            float stunAtSave = bird.Get<FlappyStun>().StunRemaining;
            float invulnAtSave = bird.Get<FlappyStun>().InvulnRemaining;
            Assert.That(stunAtSave, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(invulnAtSave, Is.GreaterThan(0f));

            for (long t = invulnEnterTick + 1; t <= invulnEnterTick + 10; t++) { world.Tick(t, 0.02f); world.SaveState(t); }
            Assert.That(bird.Get<FlappyStun>().InvulnRemaining, Is.LessThan(invulnAtSave));   // 줄었다

            Assert.That(world.LoadState(invulnEnterTick), Is.True);

            Assert.That(bird.Get<FlappyStun>().StunRemaining, Is.EqualTo(stunAtSave).Within(0.0001f));
            Assert.That(bird.Get<FlappyStun>().InvulnRemaining, Is.EqualTo(invulnAtSave).Within(0.0001f));
        }

        [Test]
        public void 저장된_틱의_스턴을_되돌려_읽을_수_있다()
        {
            //  보정 핸들러가 "그 틱에 내가 뭘 예측했나"를 서버 값과 비교하려면 이 조회가 필요하다.
            var world = FlappyWorldFixture.Create(new FlappyWorldFixture.AlwaysHit(), out var bird);
            world.GameplayStartTick = 0;

            for (long t = 1; t <= 5; t++) { world.Tick(t, 0.02f); world.SaveState(t); }
            float atFive = bird.Get<FlappyStun>().StunRemaining;

            Assert.IsTrue(world.TryGetSavedStun(5, bird.Id, out var saved));
            Assert.AreEqual(atFive, saved.StunRemaining, 1e-4f);
        }

        [Test]
        public void 저장이_없는_틱은_false다()
        {
            var world = FlappyWorldFixture.Create(new FlappyWorldFixture.AlwaysHit(), out var bird);
            world.GameplayStartTick = 0;

            Assert.IsFalse(world.TryGetSavedStun(999, bird.Id, out _));
        }
    }
}
