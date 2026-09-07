using System;
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

        //  가로세로가 달라야(HalfWidth≠HalfDepth) 축이 바뀌는 실수가 값으로 드러난다.
        static Door MakeRotated() => new Door(
            center: new System.Numerics.Vector3(0f, 100f, 0f),
            halfWidth: 5f, halfDepth: 1f, thickness: 0.5f, axisAngle: MathF.PI / 2f,
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

        [Test]
        public void 회전한_문은_축을_구분한다()
        {
            //  90도 회전하면 문이 덮는 띠가 Z축을 따라 눕는다(HalfWidth=5). X축 쪽은 HalfDepth=1이라
            //  좁다 — cos/sin이 뒤바뀌면(축 혼동) 두 결과가 같이 뒤집힌다.
            Body(0f, 99.8f, 5.39f, out var alongAxis, out var alongAxisTop);
            Assert.That(DoorGeometry.Crushes(MakeRotated(), 12, alongAxis, alongAxisTop, Radius), Is.True, "문 축(Z) 방향 문턱 안쪽");

            Body(5.39f, 99.8f, 0f, out var acrossAxis, out var acrossAxisTop);
            Assert.That(DoorGeometry.Crushes(MakeRotated(), 12, acrossAxis, acrossAxisTop, Radius), Is.False, "HalfDepth=1이라 한참 밖");
        }

        // ── 물러난 패널 (굽기 검사가 쓴다) ────────────────────────────────────

        [Test]
        public void 물러난_패널은_구멍_밖의_띠를_차지한다()
        {
            //  openness=1이면 패널은 로컬 x [HalfWidth, 2·HalfWidth] = [5,10]으로 물러난다.
            //  구멍 한복판(0)은 비고, 그 띠 안(7.5)은 막힌다 — 열린 문의 콜라이더가 어디
            //  남는지가 부활 지점 검사(굽기)의 전부다.
            Body(0f, 99.8f, 0f, out var center, out var centerTop);
            Assert.That(DoorGeometry.PanelOverlaps(Make(), 1f, center, centerTop, Radius), Is.False, "구멍 한복판은 비어 있다");

            Body(7.5f, 99.8f, 0f, out var band, out var bandTop);
            Assert.That(DoorGeometry.PanelOverlaps(Make(), 1f, band, bandTop, Radius), Is.True, "물러난 띠 한복판");
        }

        [Test]
        public void 물러난_패널의_문턱도_양쪽을_잰다()
        {
            //  띠의 바깥 끝은 x=10, 반지름 0.4라 문턱은 정확히 10.4다.
            Body(10.41f, 99.8f, 0f, out var outside, out var outsideTop);
            Assert.That(DoorGeometry.PanelOverlaps(Make(), 1f, outside, outsideTop, Radius), Is.False, "바깥");

            Body(10.39f, 99.8f, 0f, out var inside, out var insideTop);
            Assert.That(DoorGeometry.PanelOverlaps(Make(), 1f, inside, insideTop, Radius), Is.True, "안쪽");
        }

        [Test]
        public void 닫힌_자세의_PanelOverlaps는_Crushes와_같은_답을_낸다()
        {
            //  Crushes가 이 함수를 openness 0으로 부르는 것이 전부다 — 갈라지면 굽기 검사가
            //  런타임 판정과 다른 상자를 재게 된다.
            Body(5.39f, 99.8f, 0f, out var inside, out var insideTop);
            Assert.That(DoorGeometry.PanelOverlaps(Make(), 0f, inside, insideTop, Radius),
                        Is.EqualTo(DoorGeometry.Crushes(Make(), 12, inside, insideTop, Radius)));

            Body(5.41f, 99.8f, 0f, out var outside, out var outsideTop);
            Assert.That(DoorGeometry.PanelOverlaps(Make(), 0f, outside, outsideTop, Radius),
                        Is.EqualTo(DoorGeometry.Crushes(Make(), 12, outside, outsideTop, Radius)));
        }
    }
}
