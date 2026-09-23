using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// 부스트 패드가 <b>월드에 실제로 물려 있는지</b>를 본다. 판정 자체는
    /// <see cref="FlappyBoostPadFieldTests"/>가 보고, 여기서는 배선을 지킨다 — 필드를 만들어 놓고
    /// 틱에서 안 물으면 아무 일도 일어나지 않는데 테스트는 전부 초록이 된다.
    /// </summary>
    public class FlappyWorldBoostPadTests
    {
        static FlappyWorld World(EntityRegistry registry, FlappyBoostPadField pads)
        {
            var world = new FlappyWorld(registry, new WorldEventBuffer(),
                new FlappyMoveSystem(FlappyWorldFixture.Config()),
                new FlappyStunSystem(FlappyWorldFixture.Config()),
                new FlappyDashSystem(FlappyWorldFixture.Config()),
                new FinishSystem(new FinishLineBounds(FinishAxis.X), FinishAxis.X, increasing: true),
                new FlappyWindmillField(), pads,
                new FlappyWorldFixture.NeverHit(), new FlappyWorldFixture.NoopMotionBridge(),
                layerMask: ~0);
            world.GameplayStartTick = 0;   // 출발 게이트는 이 파일의 관심사가 아니다
            return world;
        }

        static FlappyBoostPadField PadAtOrigin(float duration = 0.6f)
        {
            var pads = new FlappyBoostPadField();
            pads.Add(new FlappyBoostRect(-5f, 5f, -5f, 5f, duration));
            return pads;
        }

        [Test]
        public void 패드를_밟으면_게이지를_안_쓰고_대시가_붙는다()
        {
            //  한 칸 넘게 채워 두고 <b>대시를 누르지 않는다</b>. 그래도 대시가 붙고 게이지가
            //  줄지 않았다면, 그 대시는 패드가 준 것이지 게이지를 쓴 것이 아니다.
            //  (게이지가 조금 <i>느는</i> 것은 정상이다 — 매 틱 차는 기본 충전이 따로 있다.)
            var registry = new EntityRegistry();
            var bird = FlappyWorldFixture.Bird("bird-1");
            bird.Get<FlappyDash>().Charge = 1.5f;
            registry.Add(bird);

            World(registry, PadAtOrigin()).Tick(1, 0.02f);

            Assert.Greater(bird.Get<FlappyDash>().DashRemaining, 0f);
            Assert.GreaterOrEqual(bird.Get<FlappyDash>().Charge, 1.5f,
                                  "부스트는 공짜여야 한다 — 한 칸을 썼으면 0.5로 떨어졌을 것이다");
        }

        [Test]
        public void 게이지가_없어도_패드는_준다()
        {
            var registry = new EntityRegistry();
            var bird = FlappyWorldFixture.Bird("bird-1");
            bird.Get<FlappyDash>().Charge = 0f;
            registry.Add(bird);

            World(registry, PadAtOrigin()).Tick(1, 0.02f);

            Assert.Greater(bird.Get<FlappyDash>().DashRemaining, 0f);
        }

        [Test]
        public void 패드_밖이면_아무_일도_없다()
        {
            var registry = new EntityRegistry();
            var bird = FlappyWorldFixture.Bird("bird-1");
            bird.Get<GameFramework.World.Transform>().Position = new System.Numerics.Vector3(100f, 0f, 0f);
            bird.Get<FlappyDash>().Charge = 0f;
            registry.Add(bird);

            World(registry, PadAtOrigin()).Tick(1, 0.02f);

            Assert.AreEqual(0f, bird.Get<FlappyDash>().DashRemaining, 1e-5f);
        }

        [Test]
        public void 얼어_있는_새는_패드를_못_쓴다()
        {
            //  스턴 중에 붙으면 풀리는 순간 남은 대시가 되살아나 "맞고 나서 튀어나가는" 그림이 된다
            //  (FlappyDashSystem.Cancel이 막는 것과 같은 것).
            var registry = new EntityRegistry();
            var bird = FlappyWorldFixture.Bird("bird-1");
            bird.Get<FlappyStun>().StunRemaining = 0.8f;
            registry.Add(bird);

            World(registry, PadAtOrigin()).Tick(1, 0.02f);

            Assert.AreEqual(0f, bird.Get<FlappyDash>().DashRemaining, 1e-5f);
        }
    }
}
