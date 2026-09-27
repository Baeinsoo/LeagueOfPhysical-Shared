using NUnit.Framework;

namespace LOP.Tests
{
    public class FlappyVerticalKernelTests
    {
        const float G = 59f, Flap = 18.6f, MaxFall = 30f, Dt = 0.02f, Up = 30f, Cap = 12f, Mult = 2f;

        static float Next(float vy, bool flap, FlappyAirflowKind air)
            => FlappyVerticalKernel.Next(vy, flap, air, Flap, G, MaxFall, Dt, Up, Cap, Mult);

        [Test]
        public void 기류가_없으면_지금_식과_비트까지_같다()
        {
            //  예전 식: vy − g·dt → 종단속도 클램프 → 날갯짓이면 덮기. 검사기 재생이 이것과 1 ulp만
            //  달라도 찾은 경로가 깨진다(2026-09-14에 실제로 겪었다).
            foreach (float vy in new[] { 0f, -5f, -29.9f, 18.6f, 3.3f })
            {
                float old = vy - G * Dt;
                if (old < -MaxFall) { old = -MaxFall; }
                Assert.AreEqual(old, Next(vy, false, FlappyAirflowKind.None));
                Assert.AreEqual(Flap, Next(vy, true, FlappyAirflowKind.None));
            }
        }

        [Test]
        public void 상승기류는_위로_가속하고_상한을_넘지_않는다()
        {
            Assert.That(Next(0f, false, FlappyAirflowKind.Up), Is.EqualTo(Up * Dt).Within(1e-6f));
            Assert.AreEqual(Cap, Next(11.9f, false, FlappyAirflowKind.Up));
            //  날갯짓으로 상한 위에 있으면 기류가 더 밀지 않고 중력만큼 줄어든다.
            Assert.That(Next(18.6f, false, FlappyAirflowKind.Up), Is.EqualTo(18.6f - G * Dt).Within(1e-6f));
            Assert.AreEqual(Flap, Next(0f, true, FlappyAirflowKind.Up));
        }

        [Test]
        public void 샤프트는_중력이_배수만큼_세고_종단속도는_같다()
        {
            Assert.That(Next(0f, false, FlappyAirflowKind.Down), Is.EqualTo(-G * Mult * Dt).Within(1e-6f));
            Assert.AreEqual(-MaxFall, Next(-29f, false, FlappyAirflowKind.Down));
        }
    }
}
