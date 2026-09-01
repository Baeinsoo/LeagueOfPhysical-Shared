using NUnit.Framework;

namespace LOP.Tests
{
    public class FinishOrderTrackerTests
    {
        [Test]
        public void 아직_안_닿았으면_기록되지_않는다()
        {
            var tracker = new FinishOrderTracker();

            tracker.Observe("a", tick: 10, past: -5f);

            Assert.IsFalse(tracker.HasFinished("a"));
            Assert.AreEqual(0, tracker.FinishedCount);
        }

        [Test]
        public void 닿는_순간_틱과_넘어간_깊이를_적는다()
        {
            var tracker = new FinishOrderTracker();

            tracker.Observe("a", tick: 10, past: -0.5f);
            tracker.Observe("a", tick: 11, past: 0.3f);

            Assert.AreEqual(11, tracker.Ordered[0].Tick);
            Assert.AreEqual(0.3f, tracker.Ordered[0].Past, 1e-5f);
        }

        [Test]
        public void 먼저_닿은_틱이_앞선다()
        {
            var tracker = new FinishOrderTracker();

            tracker.Observe("늦은쪽", tick: 12, past: 5f);   // 깊이는 더 깊지만 틱이 늦다
            tracker.Observe("빠른쪽", tick: 11, past: 0.1f);

            Assert.AreEqual("빠른쪽", tracker.Ordered[0].EntityId);
        }

        [Test]
        public void 같은_틱이면_더_깊이_넘은_쪽이_먼저다()
        {
            //  Flappy처럼 속도가 같은 게임에서 이게 기본 상황이다. 틱만 세면 못 가른다.
            var tracker = new FinishOrderTracker();

            tracker.Observe("뒤", tick: 11, past: 0.1f);
            tracker.Observe("앞", tick: 11, past: 0.9f);

            Assert.AreEqual("앞", tracker.Ordered[0].EntityId);
            Assert.AreEqual("뒤", tracker.Ordered[1].EntityId);
        }

        [Test]
        public void 틱도_깊이도_같으면_동점이다()
        {
            var tracker = new FinishOrderTracker();

            tracker.Observe("a", tick: 11, past: 0.5f);
            tracker.Observe("b", tick: 11, past: 0.5f);

            Assert.IsTrue(tracker.Ordered[0].SameRankAs(tracker.Ordered[1]));
        }

        [Test]
        public void 틱이_같아도_깊이가_다르면_동점이_아니다()
        {
            var tracker = new FinishOrderTracker();

            tracker.Observe("a", tick: 11, past: 0.5f);
            tracker.Observe("b", tick: 11, past: 0.4f);

            Assert.IsFalse(tracker.Ordered[0].SameRankAs(tracker.Ordered[1]));
        }

        [Test]
        public void 처음_닿은_순간이_등수다()
        {
            //  닿은 뒤에도 관측이 계속 들어오는데, 그때마다 갈아치우면 안 된다.
            var tracker = new FinishOrderTracker();

            tracker.Observe("a", tick: 11, past: 0.3f);
            tracker.Observe("a", tick: 12, past: 20f);

            Assert.AreEqual(1, tracker.FinishedCount);
            Assert.AreEqual(11, tracker.Ordered[0].Tick);
            Assert.AreEqual(0.3f, tracker.Ordered[0].Past, 1e-5f);
        }

        [Test]
        public void 선에_정확히_닿기만_해도_통과다()
        {
            var tracker = new FinishOrderTracker();

            tracker.Observe("a", tick: 11, past: 0f);

            Assert.IsTrue(tracker.HasFinished("a"));
        }

        [Test]
        public void 초기화하면_기록이_사라진다()
        {
            var tracker = new FinishOrderTracker();
            tracker.Observe("a", tick: 11, past: 1f);

            tracker.Reset();

            Assert.AreEqual(0, tracker.FinishedCount);
            Assert.IsFalse(tracker.HasFinished("a"));
        }
    }
}
