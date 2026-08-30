using NUnit.Framework;
using System.Collections.Generic;

namespace LOP.Tests
{
    public class SkydiveProgressTests
    {
        [Test]
        public void 결승고도보다_아래면_통과다()
        {
            var progress = new SkydiveProgress(10f);
            Assert.IsTrue(progress.HasFinished(9.9f));
        }

        [Test]
        public void 결승고도에_정확히_있으면_통과다()
        {
            var progress = new SkydiveProgress(10f);
            Assert.IsTrue(progress.HasFinished(10f));
        }

        [Test]
        public void 결승고도보다_위면_아직이다()
        {
            var progress = new SkydiveProgress(10f);
            Assert.IsFalse(progress.HasFinished(10.1f));
        }

        [Test]
        public void 전원이_내려와야_전원완주다()
        {
            var progress = new SkydiveProgress(10f);
            Assert.IsTrue(progress.AllFinished(new List<float> { 5f, 9f }));
            Assert.IsFalse(progress.AllFinished(new List<float> { 5f, 11f }));
        }

        [Test]
        public void 아무도_없으면_전원완주가_아니다()
        {
            // 스폰 직전(몸이 아직 없을 때) "전원 완주"로 끝내면 시작하자마자 판이 끝난다.
            var progress = new SkydiveProgress(10f);
            Assert.IsFalse(progress.AllFinished(new List<float>()));
        }
    }
}
