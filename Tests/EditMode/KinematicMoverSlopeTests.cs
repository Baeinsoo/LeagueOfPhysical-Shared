using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class KinematicMoverSlopeTests
    {
        const float Radius = 0.45f;
        const float Height = 0.9f;
        const float DeltaTime = 0.02f;
        const float Gravity = 70f;
        const float ForwardSpeed = 11f;

        [Test]
        public void 오르막을_지나도_몸이_경사_안으로_파묻히지_않는다()
        {
            //  수평 sweep이 캡슐을 들어올려 검사하면서 실제 위치는 안 올리면, 오르막에서
            //  그 차이만큼 몸이 언덕에 박힌다(실측 2.7cm). 박힘이 곧 떨림의 씨앗이다.
            var map = new HalfSpaceQuery();
            map.AddSlope(32f, Vector3.zero);

            Vector3 pos = new Vector3(-1f, 0.6f, 0f);
            Vector3 vel = new Vector3(ForwardSpeed, 0f, 0f);

            for (int tick = 0; tick < 60; tick++)
            {
                vel.y -= Gravity * DeltaTime;
                var result = KinematicMover.Move(
                    new KinematicMoveInput(pos, vel, Radius, Height, DeltaTime, ~0), map);
                pos = result.position;
                vel = result.velocity;

                Vector3 p1 = pos + Vector3.up * Radius;
                Vector3 p2 = pos + Vector3.up * (Height - Radius);
                float clear = map.Clearance(map.Faces[0], p1, p2, Radius);
                Assert.That(clear, Is.GreaterThan(-1e-3f),
                    $"t{tick}: 이동 뒤 몸이 경사 안으로 {-clear:F4}m 파묻혔다");
            }
        }
    }
}
