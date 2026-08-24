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
    }
}
