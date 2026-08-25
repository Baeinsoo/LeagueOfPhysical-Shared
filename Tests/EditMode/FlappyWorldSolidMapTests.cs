using GameFramework.Physics;
using NUnit.Framework;

namespace LOP.Tests
{
    public class FlappyWorldSolidMapTests
    {
        //  앞을 막는 벽. 이웃 테스트 파일들의 스텁과 같은 모양으로 둔다.
        private class WallQuery : ICollisionQuery
        {
            public CollisionHit CapsuleCast(UnityEngine.Vector3 p1, UnityEngine.Vector3 p2, float radius,
                UnityEngine.Vector3 direction, float distance, int layerMask)
                => new CollisionHit(true, 0f, -direction, p1);
        }

        private const float Dt = 0.02f;

        private static FlappyWorld Started(ICollisionQuery query, out GameFramework.World.Entity bird)
        {
            var world = FlappyWorldFixture.Create(query, out bird);
            world.GameplayStartTick = 0;
            return world;
        }

        [Test]
        public void 앞이_막혀_있으면_벽을_넘어가지_않는다()
        {
            var world = Started(new WallQuery(), out var bird);
            world.Tick(0, Dt);

            //  거리 0인 벽이므로 한 틱을 굴려도 시작 위치를 벗어나면 안 된다.
            Assert.AreEqual(System.Numerics.Vector3.Zero, bird.Get<GameFramework.World.Transform>().Position);
        }

        [Test]
        public void 막힌_채로_여러_틱_굴려도_위치가_흔들리지_않는다()
        {
            var world = Started(new WallQuery(), out var bird);
            var stun = bird.Get<FlappyStun>();
            //  무적 없이 두면 첫 충돌로 0.8초 스턴에 걸려, 그동안은 속도가 0이라 "안 움직여서
            //  안 흔들리는" 것과 "매 틱 전진을 시도하다 막혀서 안 흔들리는" 것을 구분할 수
            //  없다(49틱=0.98초 중 앞 39틱이 그랬다). 49틱 내내 무적을 걸어 둬 매 틱 실제로
            //  전진을 시도하다 막히게 만든다 — 그래야 이 테스트가 이름대로 진동을 검사한다.
            stun.InvulnRemaining = 10f;

            world.Tick(0, Dt);
            var afterFirst = bird.Get<GameFramework.World.Transform>().Position;

            for (long tick = 1; tick < 50; tick++)
            {
                world.Tick(tick, Dt);
                //  진동이란 "막힌 채 매 틱 위치가 흔들리는 것"이다. 불변이면 진동이 없다는 뜻이다.
                Assert.AreEqual(afterFirst, bird.Get<GameFramework.World.Transform>().Position,
                    $"tick {tick}에서 위치가 움직였다");
            }
        }

        [Test]
        public void 무적_중에도_벽을_넘어가지_않는다()
        {
            var world = Started(new WallQuery(), out var bird);
            var stun = bird.Get<FlappyStun>();

            //  스턴은 끝나고 무적만 남은 상태를 직접 만든다.
            stun.StunRemaining = 0f;
            stun.InvulnRemaining = 0.5f;

            world.Tick(0, Dt);

            Assert.AreEqual(System.Numerics.Vector3.Zero, bird.Get<GameFramework.World.Transform>().Position);
        }

        [Test]
        public void 아무것도_안_닿으면_그대로_간다()
        {
            var world = Started(new EmptySkyQuery(), out var bird);
            world.Tick(0, Dt);

            //  전진은 상수(11)라 한 틱 뒤 x는 계산으로 나온다. "0보다 크다"로 두면 기어가도 통과한다.
            Assert.AreEqual(11f * Dt, bird.Get<GameFramework.World.Transform>().Position.X, 1e-4f);
        }

        [Test]
        public void 바닥에_막힌_채_계속_굴려도_낙하속도가_쌓이지_않는다()
        {
            var world = Started(new FloorQuery(), out var bird);
            //  그냥 두면 첫 접촉에 스턴이 걸려 속도를 0으로 덮어써 버린다 — 그러면 "막힌 축의
            //  속도를 지우는가"가 아니라 "스턴이 속도를 지우는가"를 보게 된다. 무적으로 막아 둔다.
            bird.Get<FlappyStun>().InvulnRemaining = 10f;

            for (long tick = 0; tick < 60; tick++)
            {
                world.Tick(tick, Dt);
            }

            //  바닥에 막혀 실제로는 안 떨어지는데 속도만 쌓이면, 벽이 사라지는 순간 새가 그
            //  누적 속도(MaxFallSpeed 30)로 튀어 나간다 — 실측 y = −4,730이 그 결과였다.
            Assert.AreEqual(0f, bird.Get<GameFramework.World.Velocity>().Linear.Y, 1e-3f);
        }

        [Test]
        public void 법선에_z가_섞인_벽을_미끄러져도_레인을_벗어나지_않는다()
        {
            var world = Started(new SkewedWallQuery(), out var bird);
            bird.Get<FlappyStun>().InvulnRemaining = 10f;   //  스턴으로 서 버리면 미끄러질 일이 없다

            for (long tick = 0; tick < 10; tick++)
            {
                world.Tick(tick, Dt);
            }

            //  미끄러짐은 남은 이동을 충돌면에 투영한다 — 법선에 z가 섞이면 매 틱 조금씩 옆으로
            //  새어 나가 새가 x-y 레인을 영영 벗어난다.
            Assert.AreEqual(0f, bird.Get<GameFramework.World.Transform>().Position.Z);
            //  속도에 남은 z도 지워야 한다 — 스냅샷에 실려 나가면 남의 화면에서 그 속도로
            //  외삽되는 동안 새가 레인 밖으로 벌어져 보인다.
            Assert.AreEqual(0f, bird.Get<GameFramework.World.Velocity>().Linear.Z);
        }

        //  바닥만 있는 하늘 — 아래로 향하는 sweep만 맞는다.
        private class FloorQuery : ICollisionQuery
        {
            public CollisionHit CapsuleCast(UnityEngine.Vector3 p1, UnityEngine.Vector3 p2, float radius,
                UnityEngine.Vector3 direction, float distance, int layerMask)
                => direction.y < 0f
                    ? new CollisionHit(true, 0f, UnityEngine.Vector3.up, p1)
                    : CollisionHit.None;
        }

        //  비스듬히 선 벽 — 법선에 z가 섞여 있어 미끄러짐이 새를 레인 밖으로 밀어낸다.
        //  수평 sweep에만 답해 드리프트가 미끄러짐에서 온다는 걸 분명히 한다.
        private class SkewedWallQuery : ICollisionQuery
        {
            public CollisionHit CapsuleCast(UnityEngine.Vector3 p1, UnityEngine.Vector3 p2, float radius,
                UnityEngine.Vector3 direction, float distance, int layerMask)
                => direction.y != 0f
                    ? CollisionHit.None
                    : new CollisionHit(true, 0.05f, new UnityEngine.Vector3(-0.707f, 0f, -0.707f),
                                       p1 + direction * 0.05f);
        }

        //  이웃 테스트 파일들과 같은 모양의 "아무것도 안 맞는" 스텁.
        private class EmptySkyQuery : ICollisionQuery
        {
            public CollisionHit CapsuleCast(UnityEngine.Vector3 p1, UnityEngine.Vector3 p2, float radius,
                UnityEngine.Vector3 direction, float distance, int layerMask)
                => CollisionHit.None;
        }
    }
}
