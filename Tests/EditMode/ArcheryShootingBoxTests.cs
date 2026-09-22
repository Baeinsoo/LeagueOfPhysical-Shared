using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    using Box = LOP.ArcheryShootingBox;

    //  사수는 자기 **사대** 안에서만 움직인다. 앞으로 걸어 나갈 수 있으면 90m 과녁 앞으로
    //  30m 걸어가 60m로 만들 수 있고, 그러면 거리 여섯을 둔 의미도 거리별 과녁 크기도
    //  전부 근거를 잃는다.
    //
    //  자르기는 **레인 기준**이다 — 레인마다 보는 쪽이 다르므로 월드 축으로 자르면
    //  어떤 레인은 좌우가 앞뒤가 된다.
    public class ArcheryShootingBoxTests
    {
        static readonly Vector3 Origin = new Vector3(10f, 2f, 5f);
        static readonly Vector3 Forward = Vector3.forward;   // 레인이 +Z를 본다
        const float HalfWidth = 2f;
        const float HalfDepth = 1f;

        static Vector3 Clamp(Vector3 p) => Box.Clamp(p, Origin, Forward, HalfWidth, HalfDepth);

        [Test]
        public void 상자_안이면_그대로_둔다()
        {
            var inside = Origin + new Vector3(1f, 0f, 0.5f);

            var result = Clamp(inside);

            Assert.AreEqual(inside.x, result.x, 1e-4f);
            Assert.AreEqual(inside.y, result.y, 1e-4f);
            Assert.AreEqual(inside.z, result.z, 1e-4f);
        }

        [Test]
        public void 좌우로_벗어나면_옆면까지만()
        {
            var far = Origin + new Vector3(9f, 0f, 0f);

            Assert.AreEqual(Origin.x + HalfWidth, Clamp(far).x, 1e-4f, "오른쪽 옆면을 넘었다");
            Assert.AreEqual(Origin.x - HalfWidth, Clamp(Origin + new Vector3(-9f, 0f, 0f)).x, 1e-4f);
        }

        //  ⭐ 이 게임의 핵심 제약 — 앞으로 걸어 나가 거리를 줄일 수 없어야 한다.
        [Test]
        public void 앞으로는_사대_깊이까지만()
        {
            var charging = Origin + new Vector3(0f, 0f, 40f);   // 과녁 쪽으로 40m

            Assert.AreEqual(Origin.z + HalfDepth, Clamp(charging).z, 1e-4f,
                "앞으로 걸어 나갔다 — 거리가 무의미해진다");
        }

        [Test]
        public void 뒤로도_사대_깊이까지만()
        {
            var backing = Origin + new Vector3(0f, 0f, -40f);

            Assert.AreEqual(Origin.z - HalfDepth, Clamp(backing).z, 1e-4f);
        }

        //  높이는 중력·충돌이 정한다. 여기서 건드리면 바닥에 내려앉지 못하거나 공중에 뜬다.
        [Test]
        public void 높이는_건드리지_않는다()
        {
            var falling = Origin + new Vector3(0f, -13.5f, 0f);

            Assert.AreEqual(Origin.y - 13.5f, Clamp(falling).y, 1e-4f, "자르기가 높이를 건드렸다");
        }

        //  레인이 월드 축과 어긋난 경우. 월드 축으로 자르면 여기서 좌우와 앞뒤가 뒤바뀐다.
        [Test]
        public void 레인이_돌아가_있어도_그_레인_기준이다()
        {
            Vector3 forward = Vector3.right;   // 레인이 +X를 본다
            //  이 레인에서 "앞"은 +X다. 40m 앞으로 나가면 깊이까지만 잘려야 한다.
            var charging = Origin + new Vector3(40f, 0f, 0f);

            var result = Box.Clamp(charging, Origin, forward, HalfWidth, HalfDepth);

            Assert.AreEqual(Origin.x + HalfDepth, result.x, 1e-4f,
                "돌아간 레인에서 앞뒤를 좌우로 잘랐다");
        }

        //  0이면 "사대에 못 박혀 있다" — 이동을 안 켠 맵(원형)이 이 값을 그대로 쓴다.
        [Test]
        public void 크기가_0이면_제자리에_묶인다()
        {
            var wandered = Origin + new Vector3(3f, 0f, 7f);

            var result = Box.Clamp(wandered, Origin, Forward, 0f, 0f);

            Assert.AreEqual(Origin.x, result.x, 1e-4f);
            Assert.AreEqual(Origin.z, result.z, 1e-4f);
        }

        //  forward가 0이면(마커가 이상하게 찍혔다) 축을 못 세운다. 터지지 말고 제자리로.
        [Test]
        public void 앞이_0이면_원점으로_둔다()
        {
            var result = Box.Clamp(Origin + new Vector3(5f, 0f, 5f), Origin, Vector3.zero, HalfWidth, HalfDepth);

            Assert.AreEqual(Origin.x, result.x, 1e-4f);
            Assert.AreEqual(Origin.z, result.z, 1e-4f);
        }
    }
}
