using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class FlappyWorldSlopeTremorTests
    {
        [Test]
        public void 날갯짓하지_않았는데_세로_속도가_위를_향하면_안_된다()
        {
            //  파묻힌 몸을 밀어내면서 "표면으로 파고드는 속도"를 지우는데, 고정 전진 속도가
            //  경사 법선에 크게 걸려 있어 그 제거가 세로 +4.16m/s짜리 발길질이 된다.
            //  파묻혔다/안 파묻혔다를 매 틱 오가므로 25Hz로 떤다.
            //  입력이 없는 새의 세로 속도는 중력·지면이 소유한다 — 위를 향할 이유가 없다.
            var map = new HalfSpaceQuery();
            map.AddSlope(32f, Vector3.zero);

            var world = FlappyWorldFixture.Create(map, new HalfSpaceMotionBridge(map), out var bird);
            world.GameplayStartTick = 0;
            bird.Get<GameFramework.World.Transform>().Position = new System.Numerics.Vector3(-1f, 0.6f, 0f);

            for (long tick = 0; tick < 120; tick++)
            {
                world.Tick(tick, 0.02f);
                float vy = bird.Get<GameFramework.World.Velocity>().Linear.Y;
                Assert.That(vy, Is.LessThanOrEqualTo(1e-3f),
                    $"t{tick}: 입력이 없는데 세로 속도가 +{vy:F2} — 경사가 새를 밀어 올리고 있다");
            }
        }
    }
}
