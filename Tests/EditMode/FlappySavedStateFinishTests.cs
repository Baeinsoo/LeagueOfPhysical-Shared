using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// 되돌리기가 통과 기록까지 담는지. 클라가 통과를 예측하므로, 되돌릴 때 같이 안 되돌리면
    /// 재생 뒤에도 "이미 통과함"이 남아 새가 영영 감속한 채로 있는다.
    /// </summary>
    public class FlappySavedStateFinishTests
    {
        private static Entity Bird()
        {
            var bird = new Entity("bird");
            bird.Add(new FlappyStun());
            bird.Add(new FlappyDash());
            bird.Add(new FinishState());
            return bird;
        }

        [Test]
        public void 통과_전의_사진으로_되돌리면_통과가_취소된다()
        {
            var bird = Bird();
            var before = FlappySavedState.Capture(bird);

            bird.Get<FinishState>().FinishedTick = 500;
            bird.Get<FinishState>().Depth = 0.3f;
            before.RestoreTo(bird);

            Assert.AreEqual(FinishState.NotFinished, bird.Get<FinishState>().FinishedTick);
            Assert.AreEqual(0f, bird.Get<FinishState>().Depth);
        }

        [Test]
        public void 통과_뒤의_사진은_그대로_되살아난다()
        {
            var bird = Bird();
            bird.Get<FinishState>().FinishedTick = 500;
            bird.Get<FinishState>().Depth = 0.3f;
            var after = FlappySavedState.Capture(bird);

            bird.Get<FinishState>().FinishedTick = FinishState.NotFinished;
            bird.Get<FinishState>().Depth = 0f;
            after.RestoreTo(bird);

            Assert.AreEqual(500, bird.Get<FinishState>().FinishedTick);
            Assert.That(bird.Get<FinishState>().Depth, Is.EqualTo(0.3f).Within(1e-4f));
        }
    }
}
