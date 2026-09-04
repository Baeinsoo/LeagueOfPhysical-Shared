using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 결승선 통과 판정. 서버가 등수를 매길 때 쓰는 것과 <b>같은 식</b>(FinishLineOverlap)을 쓰되,
    /// 몸 바운드를 콜라이더가 아니라 진실원본에서 조립한다 — 되돌리기 재생 중엔 콜라이더가 얼어 있어
    /// 같은 코드가 라이브와 재생에서 다른 답을 내기 때문이다.
    /// </summary>
    public class FinishSystemTests
    {
        private const float Radius = 0.45f;
        private const float Height = 0.9f;

        //  결승선은 x=100에 두께 2로 선다 — 근접면이 99다.
        private static FinishLineBounds Line()
        {
            var line = new FinishLineBounds(FinishAxis.X);
            line.Register(new Bounds(new Vector3(100f, 0f, 0f), new Vector3(2f, 50f, 2f)));
            return line;
        }

        private static Entity Bird(float x)
        {
            var bird = new Entity("bird");
            bird.Add(new GameFramework.World.Transform { Position = new System.Numerics.Vector3(x, 0f, 0f) });
            bird.Add(new CapsuleShape(Radius, Height));
            bird.Add(new FinishState());
            return bird;
        }

        [Test]
        public void 아직이면_틱이_없음_표시다()
        {
            var bird = Bird(50f);

            new FinishSystem(Line(), FinishAxis.X, increasing: true).Tick(bird, 10);

            Assert.AreEqual(FinishState.NotFinished, bird.Get<FinishState>().FinishedTick);
            Assert.IsFalse(bird.Get<FinishState>().Finished);
        }

        [Test]
        public void 부리가_닿으면_통과다()
        {
            //  근접면 99. 중심 98.6이면 부리는 99.05라 닿았다 — 중심 기준이면 아직이다.
            var bird = Bird(98.6f);

            new FinishSystem(Line(), FinishAxis.X, increasing: true).Tick(bird, 10);

            Assert.IsTrue(bird.Get<FinishState>().Finished);
            Assert.AreEqual(10, bird.Get<FinishState>().FinishedTick);
            Assert.That(bird.Get<FinishState>().Depth, Is.EqualTo(0.05f).Within(1e-3f));
        }

        [Test]
        public void 부리가_아직_안_닿았으면_통과가_아니다()
        {
            //  중심 98.5면 부리는 98.95라 근접면 99에 못 미친다.
            var bird = Bird(98.5f);

            new FinishSystem(Line(), FinishAxis.X, increasing: true).Tick(bird, 10);

            Assert.IsFalse(bird.Get<FinishState>().Finished);
        }

        [Test]
        public void 처음_넘은_틱만_기록한다()
        {
            //  등수는 처음 닿은 순간이 정답이다. 뒤 틱이 덮어쓰면 더 오래 달린 사람이 유리해진다.
            var bird = Bird(99f);
            var system = new FinishSystem(Line(), FinishAxis.X, increasing: true);

            system.Tick(bird, 10);
            bird.Get<GameFramework.World.Transform>().Position = new System.Numerics.Vector3(200f, 0f, 0f);
            system.Tick(bird, 11);

            Assert.AreEqual(10, bird.Get<FinishState>().FinishedTick);
            Assert.That(bird.Get<FinishState>().Depth, Is.EqualTo(0.45f).Within(1e-3f));
        }

        [Test]
        public void 아래로_달리는_축도_같은_규칙이다()
        {
            //  Skydive는 y가 작아지는 방향이다. 몸의 아랫면이 선의 윗면을 지나면 통과.
            var line = new FinishLineBounds(FinishAxis.Y);
            line.Register(new Bounds(new Vector3(0f, 10f, 0f), new Vector3(50f, 2f, 50f)));

            var diver = new Entity("diver");
            //  캡슐은 발밑이 기준이라(collider.center.y = height/2) 바운드 아랫면이 곧 위치의 y다.
            diver.Add(new GameFramework.World.Transform { Position = new System.Numerics.Vector3(0f, 10.9f, 0f) });
            diver.Add(new CapsuleShape(Radius, Height));
            diver.Add(new FinishState());

            new FinishSystem(line, FinishAxis.Y, increasing: false).Tick(diver, 7);

            Assert.IsTrue(diver.Get<FinishState>().Finished);
            Assert.AreEqual(7, diver.Get<FinishState>().FinishedTick);
        }

        [Test]
        public void 결승선을_모르면_아무도_통과하지_않는다()
        {
            //  맵이 아직 안 올라온 순간이 실제로 있다. 그때 전원 통과로 읽으면 판이 즉시 끝난다.
            var bird = Bird(9999f);

            new FinishSystem(new FinishLineBounds(FinishAxis.X), FinishAxis.X, increasing: true).Tick(bird, 10);

            Assert.IsFalse(bird.Get<FinishState>().Finished);
        }
    }
}
