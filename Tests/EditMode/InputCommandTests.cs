using NUnit.Framework;

namespace LOP.Tests
{
    public class InputCommandTests
    {
        // 진단 로그를 눈으로 읽는 습관이 있으므로 모양을 고정해 둔다.
        [Test]
        public void ToString_ShowsEveryField_InLogShape()
        {
            var cmd = new InputCommand { Horizontal = 0.5f, Vertical = -1f, Jump = true, AbilityId = 7 };

            Assert.That(cmd.ToString(), Is.EqualTo("h=0.50 v=-1.00 jump=True ability=7 posture=0.00 glide=False posing=False dash=False aim=(0.0,0.0) draw=False(0.00) release=False fire=False view=0"));
        }

        [Test]
        public void ViewTick_은_ToString에_나온다()
        {
            var cmd = new InputCommand { ViewTick = 123 };

            StringAssert.Contains("view=123", cmd.ToString());
        }
    }
}
