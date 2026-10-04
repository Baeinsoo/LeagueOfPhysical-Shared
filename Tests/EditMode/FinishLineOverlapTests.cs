using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class FinishLineOverlapTests
    {
        const float Tolerance = 1e-5f;

        //  결승선 판: x = 100에 두께 0.2로 서 있다 (min.x = 99.9)
        static Bounds Line(float center, float thickness = 0.2f)
            => new Bounds(new Vector3(center, 0f, 0f), new Vector3(thickness, 20f, 20f));

        //  새 몸: 지름 0.9 캡슐
        static Bounds Body(float centerX)
            => new Bounds(new Vector3(centerX, 0f, 0f), new Vector3(0.9f, 0.9f, 0.9f));

        [Test]
        public void 몸의_앞쪽_끝이_선에_닿으면_통과다()
        {
            //  중심 99.4 + 반지름 0.45 = 앞쪽 끝 99.85. 선의 앞면은 99.9라 아직 0.05 모자라다.
            Assert.Less(FinishLineOverlap.Past(Body(99.4f), Line(100f), FinishAxis.X, increasing: true), 0f);

            //  중심 99.5 → 앞쪽 끝 99.95. 선을 0.05 파고들었다.
            Assert.AreEqual(0.05f,
                FinishLineOverlap.Past(Body(99.5f), Line(100f), FinishAxis.X, increasing: true), Tolerance);
        }

        [Test]
        public void 몸_한가운데가_아니라_앞쪽_끝으로_판정한다()
        {
            //  옛 방식(중심 좌표)이면 중심 99.95는 아직 100에 못 미쳐 통과가 아니다.
            //  형상으로 보면 앞쪽 끝이 100.4라 이미 지났다 — 눈에 보이는 것과 답이 같아야 한다.
            Assert.Greater(FinishLineOverlap.Past(Body(99.95f), Line(100f), FinishAxis.X, increasing: true), 0f);
        }

        [Test]
        public void 더_앞선_몸이_더_깊이_넘는다()
        {
            float ahead = FinishLineOverlap.Past(Body(100.2f), Line(100f), FinishAxis.X, increasing: true);
            float behind = FinishLineOverlap.Past(Body(100.0f), Line(100f), FinishAxis.X, increasing: true);

            Assert.Greater(ahead, behind);
            Assert.AreEqual(0.2f, ahead - behind, Tolerance);   // 벌어진 거리 그대로
        }

        [Test]
        public void 축값이_줄어드는_방향도_같은_식으로_잰다()
        {
            //  Skydive: 아래로 떨어져 y = 0인 판에 닿는다.
            var line = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(50f, 0.2f, 50f));   // max.y = 0.1
            var falling = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(0.9f, 0.9f, 0.9f));   // min.y = 0.55

            Assert.Less(FinishLineOverlap.Past(falling, line, FinishAxis.Y, increasing: false), 0f);

            var touching = new Bounds(new Vector3(0f, 0.5f, 0f), new Vector3(0.9f, 0.9f, 0.9f));   // min.y = 0.05
            Assert.AreEqual(0.05f,
                FinishLineOverlap.Past(touching, line, FinishAxis.Y, increasing: false), Tolerance);
        }

        [Test]
        public void 땅_결승은_판_위에_내려야_한다()
        {
            //  Skydive 결승 제단: 지름 16m 판. 높이만 맞고 옆으로 벗어나면 결승이 아니다(정글에 내리면 걸어 올라와야 한다).
            var altar = new Bounds(new Vector3(20f, 24f, -100f), new Vector3(16f, 0.4f, 16f));   // max.y = 24.2
            var onAltar = new Bounds(new Vector3(25f, 24.5f, -95f), new Vector3(0.8f, 1.8f, 0.8f));   // min.y = 23.6
            var besideAltar = new Bounds(new Vector3(40f, 1f, -100f), new Vector3(0.8f, 1.8f, 0.8f));   // 높이는 한참 아래, 옆으로 20m

            Assert.GreaterOrEqual(FinishLineOverlap.Past(onAltar, altar, FinishAxis.Y, increasing: false), 0f);
            Assert.Less(FinishLineOverlap.Past(besideAltar, altar, FinishAxis.Y, increasing: false), 0f);
        }

        [Test]
        public void 크기_없는_결승_표식은_높이만_본다()
        {
            //  옛 맵(더미·피라미드)은 렌더러 없는 표식이라 두께 0인 무한 평면 — 어디서든 그 높이를 지나면 결승.
            var marker = new Bounds(new Vector3(0f, 1.5f, 0f), Vector3.zero);
            var farAway = new Bounds(new Vector3(300f, 1f, -200f), new Vector3(0.8f, 1.8f, 0.8f));

            Assert.GreaterOrEqual(FinishLineOverlap.Past(farAway, marker, FinishAxis.Y, increasing: false), 0f);
        }

        [Test]
        public void 축을_바꾸면_그_축으로_잰다()
        {
            var line = new Bounds(Vector3.zero, new Vector3(20f, 20f, 0.2f));
            var body = new Bounds(new Vector3(0f, 0f, 0.1f), Vector3.one);

            Assert.AreEqual(0.7f,
                FinishLineOverlap.Past(body, line, FinishAxis.Z, increasing: true), Tolerance);
        }
    }
}
