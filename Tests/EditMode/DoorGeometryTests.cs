using NUnit.Framework;

namespace LOP.Tests
{
    public class DoorGeometryTests
    {
        //  주기 20 = 열림 6 + 닫는중 4 + 닫힘 6 + 여는중 4
        static Door Make(int phase = 0) => new Door(
            center: new System.Numerics.Vector3(0f, 100f, 0f),
            halfWidth: 5f, halfDepth: 5f, thickness: 0.5f, axisAngle: 0f,
            period: 20, openTicks: 6, moveTicks: 4, phase: phase);

        [Test]
        public void 열림_구간은_1이다()
        {
            Assert.That(DoorGeometry.Openness(Make(), 0), Is.EqualTo(1f));
            Assert.That(DoorGeometry.Openness(Make(), 5), Is.EqualTo(1f));
        }

        [Test]
        public void 닫힘_구간은_0이다()
        {
            //  10 = 6 + 4. 닫힘은 [10, 16)
            Assert.That(DoorGeometry.Openness(Make(), 10), Is.EqualTo(0f));
            Assert.That(DoorGeometry.Openness(Make(), 15), Is.EqualTo(0f));
        }

        [Test]
        public void 닫히는_중에는_1에서_0으로_줄어든다()
        {
            float a = DoorGeometry.Openness(Make(), 6);
            float b = DoorGeometry.Openness(Make(), 8);
            Assert.That(a, Is.GreaterThan(b));
            Assert.That(b, Is.GreaterThan(0f));
        }

        [Test]
        public void 여는_중에는_0에서_1로_늘어난다()
        {
            //  여는 중은 [16, 20)
            Assert.That(DoorGeometry.Openness(Make(), 18),
                Is.GreaterThan(DoorGeometry.Openness(Make(), 16)));
        }

        [Test]
        public void 위상은_주기를_밀어준다()
        {
            //  phase 10이면 tick 0이 원래 tick 10(닫힘)과 같아야 한다
            Assert.That(DoorGeometry.Openness(Make(phase: 10), 0), Is.EqualTo(0f));
        }

        [Test]
        public void 음수_틱도_주기_안으로_접힌다()
        {
            //  -20은 주기(20)의 정확한 배수라 순진한 %로도 0으로 맞아떨어져 부호 처리를 검증 못한다.
            //  배수가 아닌 -7을 써야 순진한 %(부호 보정 없이 그대로 남김)이 틀린 값을 내는 것을 잡는다.
            //  -7 + 20 = 13 이므로 둘은 같은 자리를 가리켜야 맞다.
            Assert.That(DoorGeometry.Openness(Make(), -7), Is.EqualTo(DoorGeometry.Openness(Make(), 13)));
        }

        [Test]
        public void 같은_틱은_같은_답이다()
        {
            //  누가 문에 상태를 넣으면 여기서 깨진다.
            for (int i = 0; i < 5; i++)
            {
                Assert.That(DoorGeometry.Openness(Make(), 7), Is.EqualTo(DoorGeometry.Openness(Make(), 7)));
            }
        }

        [Test]
        public void 닫히면_두_패널이_구멍을_정확히_덮는다()
        {
            Door d = Make();
            var a = DoorGeometry.PanelCenter(d, 0, 0f);
            var b = DoorGeometry.PanelCenter(d, 1, 0f);
            //  각 패널은 반폭의 절반 길이라, 중심이 ±halfWidth/2에 있으면 둘이 딱 맞물린다.
            Assert.That(a.X, Is.EqualTo(-2.5f).Within(1e-4f));
            Assert.That(b.X, Is.EqualTo(2.5f).Within(1e-4f));
        }

        [Test]
        public void 열리면_두_패널이_구멍_밖으로_물러난다()
        {
            Door d = Make();
            var a = DoorGeometry.PanelCenter(d, 0, 1f);
            //  물러난 패널의 안쪽 끝(-2.5 + 2.5 = ... )이 구멍 가장자리(-5) 밖에 있어야 한다.
            Assert.That(a.X + 2.5f, Is.LessThanOrEqualTo(-5f + 1e-4f));
        }
    }
}
