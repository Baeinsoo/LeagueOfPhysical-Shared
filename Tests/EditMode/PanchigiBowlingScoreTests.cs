using System.Collections.Generic;
using NUnit.Framework;

namespace LOP.Tests
{
    public class PanchigiBowlingScoreTests
    {
        private const int Frames = 5;
        private const int Pins = 6;

        private static List<PanchigiRoll> R(params int[] flips)
        {
            var list = new List<PanchigiRoll>();
            foreach (int f in flips) { list.Add(f < 0 ? PanchigiRoll.Fouled : new PanchigiRoll(f)); }
            return list;   // -1 = 파울
        }

        private static int? Cum(List<PanchigiRoll> rolls, int frame) => PanchigiBowlingScore.Frames(rolls, Frames, Pins)[frame].Cumulative;
        private static string Marks(List<PanchigiRoll> rolls, int frame) => string.Join(" ", PanchigiBowlingScore.Frames(rolls, Frames, Pins)[frame].Marks);

        [Test]
        public void 오픈_프레임은_두_번의_합이다()
        {
            var rolls = R(2, 3);
            Assert.AreEqual(5, Cum(rolls, 0));
            Assert.AreEqual("2 3", Marks(rolls, 0));
        }

        [Test]
        public void 스페어는_다음_한_번을_더한다()
        {
            var rolls = R(4, 2, 3);
            Assert.AreEqual(9, Cum(rolls, 0));
            Assert.AreEqual("4 /", Marks(rolls, 0));
        }

        [Test]
        public void 스트라이크는_다음_두_번을_더한다()
        {
            var rolls = R(6, 2, 1);
            Assert.AreEqual(9, Cum(rolls, 0));
            Assert.AreEqual(12, Cum(rolls, 1));
            Assert.AreEqual("X", Marks(rolls, 0));
        }

        [Test]
        public void 연속_스트라이크는_다음_프레임의_첫_번째까지_더한다()
        {
            var rolls = R(6, 6, 2, 0);
            Assert.AreEqual(14, Cum(rolls, 0));
            Assert.AreEqual(22, Cum(rolls, 1));
            Assert.AreEqual(24, Cum(rolls, 2));
        }

        [Test]
        public void 보너스가_아직_없으면_누적은_비어_있다()
        {
            var rolls = R(6, 2);
            Assert.IsNull(Cum(rolls, 0));
            Assert.IsNull(Cum(rolls, 1), "앞 프레임이 비면 뒤도 비어 있다");
        }

        [Test]
        public void 파울은_0점이고_F로_표시한다()
        {
            var rolls = R(-1, 3);
            Assert.AreEqual(3, Cum(rolls, 0));
            Assert.AreEqual("F 3", Marks(rolls, 0));
        }

        [Test]
        public void 첫_번째_파울_뒤_두_번째에_다_뒤집으면_스페어다()
        {
            var rolls = R(-1, 6, 2);
            Assert.AreEqual("F /", Marks(rolls, 0));
            Assert.AreEqual(8, Cum(rolls, 0));
        }

        [Test]
        public void 영점은_대시로_표시한다()
        {
            Assert.AreEqual("- 4", Marks(R(0, 4), 0));
        }

        [Test]
        public void 마지막_프레임_스트라이크면_보너스_두_번을_친다()
        {
            var rolls = R(1, 1, 1, 1, 1, 1, 1, 1, 6, 6, 3);
            var pos = PanchigiBowlingScore.Locate(rolls, Frames, Pins);
            Assert.IsTrue(pos.Complete);
            Assert.AreEqual(8 + 15, PanchigiBowlingScore.Total(rolls, Frames, Pins));
            Assert.AreEqual("X X 3", Marks(rolls, 4));
        }

        [Test]
        public void 마지막_프레임_스페어면_보너스_한_번을_친다()
        {
            var before = R(1, 1, 1, 1, 1, 1, 1, 1, 2, 4);
            Assert.IsFalse(PanchigiBowlingScore.Locate(before, Frames, Pins).Complete);

            var rolls = R(1, 1, 1, 1, 1, 1, 1, 1, 2, 4, 5);
            Assert.IsTrue(PanchigiBowlingScore.Locate(rolls, Frames, Pins).Complete);
            Assert.AreEqual(8 + 11, PanchigiBowlingScore.Total(rolls, Frames, Pins));
            Assert.AreEqual("2 / 5", Marks(rolls, 4));
        }

        [Test]
        public void 마지막_프레임_오픈이면_두_번으로_끝난다()
        {
            var rolls = R(1, 1, 1, 1, 1, 1, 1, 1, 2, 3);
            Assert.IsTrue(PanchigiBowlingScore.Locate(rolls, Frames, Pins).Complete);
            Assert.AreEqual(13, PanchigiBowlingScore.Total(rolls, Frames, Pins));
        }

        [Test]
        public void 만점은_90이다()
        {
            var rolls = R(6, 6, 6, 6, 6, 6, 6);
            Assert.IsTrue(PanchigiBowlingScore.Locate(rolls, Frames, Pins).Complete);
            Assert.AreEqual(90, PanchigiBowlingScore.Total(rolls, Frames, Pins));
        }

        [Test]
        public void 위치는_프레임과_남은_동전을_알려준다()
        {
            var pos = PanchigiBowlingScore.Locate(R(4), Frames, Pins);
            Assert.AreEqual(0, pos.Frame);
            Assert.AreEqual(1, pos.RollInFrame);
            Assert.AreEqual(2, pos.Standing);

            pos = PanchigiBowlingScore.Locate(R(4, 1), Frames, Pins);
            Assert.AreEqual(1, pos.Frame);
            Assert.AreEqual(0, pos.RollInFrame);
            Assert.AreEqual(6, pos.Standing);
        }

        [Test]
        public void 마지막_프레임_스트라이크_뒤_두_번째가_일부면_남은_것만_선다()
        {
            var pos = PanchigiBowlingScore.Locate(R(1, 1, 1, 1, 1, 1, 1, 1, 6, 2), Frames, Pins);
            Assert.IsFalse(pos.Complete);
            Assert.AreEqual(4, pos.Frame);
            Assert.AreEqual(4, pos.Standing);
        }

        [Test]
        public void 아무것도_안_쳤으면_합계는_0이고_프레임은_다섯_칸이다()
        {
            var frames = PanchigiBowlingScore.Frames(new List<PanchigiRoll>(), Frames, Pins);
            Assert.AreEqual(5, frames.Count);
            Assert.AreEqual(0, PanchigiBowlingScore.Total(new List<PanchigiRoll>(), Frames, Pins));
        }
    }
}
