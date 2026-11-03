using NUnit.Framework;

namespace LOP.Tests
{
    public class CardRushLanesTests
    {
        [Test]
        public void 레인_출발점은_가운데를_기준으로_좌우()
        {
            var o0 = CardRushLanes.LaneOrigin(UnityEngine.Vector3.zero, UnityEngine.Vector3.forward, 2.6f, 0);
            var o7 = CardRushLanes.LaneOrigin(UnityEngine.Vector3.zero, UnityEngine.Vector3.forward, 2.6f, 7);
            Assert.AreEqual(-9.1f, o0.x, 1e-4f); Assert.AreEqual(9.1f, o7.x, 1e-4f); Assert.AreEqual(0f, o0.z, 1e-4f);
        }

        [Test]
        public void 이동_중엔_고르게_멈춤_중엔_제자리()
        {
            var s = new CardRushRunnerState { Step = 5, MoveStartTick = 1000 };
            Assert.AreEqual(4f, CardRushLanes.Progress(s, 1000, 15), 1e-5f);
            Assert.AreEqual(4.6f, CardRushLanes.Progress(s, 1009, 15), 1e-5f);
            Assert.AreEqual(5f, CardRushLanes.Progress(s, 1015, 15), 1e-5f);
            var stunned = new CardRushRunnerState { Step = 5, MoveStartTick = 1000, StunEndTick = 1100 };
            Assert.AreEqual(5f, CardRushLanes.Progress(stunned, 1080, 15), 1e-5f);
        }
    }
}
