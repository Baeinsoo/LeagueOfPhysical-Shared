using NUnit.Framework;

namespace LOP.Tests
{
    public class DoorCrushTests
    {
        const float Radius = 0.4f;

        static Door Make() => new Door(
            center: new System.Numerics.Vector3(0f, 100f, 0f),
            halfWidth: 5f, halfDepth: 5f, thickness: 0.5f, axisAngle: 0f,
            period: 20, openTicks: 6, moveTicks: 4, phase: 0);

        //  몸은 선 캡슐이다 — 이동 커널과 같은 규격으로 축을 반지름만큼 안으로 당긴다.
        static void Body(float x, float y, float z, out System.Numerics.Vector3 b, out System.Numerics.Vector3 t)
        {
            b = new System.Numerics.Vector3(x, y + Radius, z);
            t = new System.Numerics.Vector3(x, y + 1.8f - Radius, z);
        }

        [Test]
        public void 닫힌_패널_안에_있으면_죽는다()
        {
            Body(0f, 99.8f, 0f, out var b, out var t);   // 패널 높이(100)에 몸이 걸침
            Assert.That(DoorGeometry.Crushes(Make(), 12, b, t, Radius), Is.True);
        }

        [Test]
        public void 열려_있으면_안_죽는다()
        {
            Body(0f, 99.8f, 0f, out var b, out var t);
            Assert.That(DoorGeometry.Crushes(Make(), 0, b, t, Radius), Is.False);
        }

        [Test]
        public void 닫히는_중에는_안_죽는다()
        {
            //  닫히는 동안은 벽일 뿐이다 — 밀려날 기회를 준다.
            Body(0f, 99.8f, 0f, out var b, out var t);
            Assert.That(DoorGeometry.Crushes(Make(), 8, b, t, Radius), Is.False);
        }

        [Test]
        public void 패널보다_아래로_지나갔으면_안_죽는다()
        {
            //  구멍 안이어도 이미 통과했으면 산다. "부피"는 구멍이 아니라 패널이다.
            Body(0f, 95f, 0f, out var b, out var t);
            Assert.That(DoorGeometry.Crushes(Make(), 12, b, t, Radius), Is.False);
        }

        [Test]
        public void 옆으로_벗어나_있으면_안_죽는다()
        {
            Body(20f, 99.8f, 0f, out var b, out var t);
            Assert.That(DoorGeometry.Crushes(Make(), 12, b, t, Radius), Is.False);
        }

        [Test]
        public void 문턱의_양쪽을_잰다()
        {
            //  패널 바깥 끝은 x=5, 몸 반지름은 0.4라 문턱은 정확히 5.4다. 그 바로 양옆을 잰다.
            Body(5.41f, 99.8f, 0f, out var outside, out var outsideTop);
            Assert.That(DoorGeometry.Crushes(Make(), 12, outside, outsideTop, Radius), Is.False, "바깥");

            Body(5.39f, 99.8f, 0f, out var inside, out var insideTop);
            Assert.That(DoorGeometry.Crushes(Make(), 12, inside, insideTop, Radius), Is.True, "안쪽");
        }
    }
}
