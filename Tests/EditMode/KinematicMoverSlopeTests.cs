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
            //  참고: 이 커널 단독 루프에는 게임 루프의 Depenetrate 되밀어내기가 없다. 그래서 수평 스텝이
            //  몸을 파묻으면 같은 틱의 수직 스텝이 그 면을 못 보고(시작 겹침) 쌓인 중력만큼 그대로 떨어져,
            //  실패 깊이가 설계 문서의 "틱당 2.7cm"보다 크게 찍힌다. 근본 결함은 같다 — 들어올린 캡슐로
            //  검사하고 안 올린 몸을 옮기는 것.
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

        [Test]
        public void 바닥에_딱_붙어_있으면_SkinWidth만큼_띄운다()
        {
            //  밀어내기는 바닥에 딱 붙게 민다. 그 상태로 수평 sweep을 쏘면 거리 0으로 맞아
            //  한 발도 못 나간다(예전에 통짜 들어올리기가 가려 주던 경우).
            var map = new HalfSpaceQuery();
            map.AddGround(0f);

            var result = KinematicMover.Move(
                new KinematicMoveInput(Vector3.zero, new Vector3(0f, -1f, 0f), Radius, Height, DeltaTime, ~0), map);

            Assert.That(result.position.y, Is.EqualTo(0.02f).Within(1e-3f), "바닥에서 SkinWidth만큼 떠 있어야 한다");
            Assert.IsTrue(result.grounded);
        }

        [Test]
        public void 위로_오르는_중에는_바닥으로_끌어당기지_않는다()
        {
            //  날갯짓해서 뜨는 새를 지면으로 스냅하면 플랩이 먹히지 않는다.
            var map = new HalfSpaceQuery();
            map.AddGround(0f);

            var result = KinematicMover.Move(
                new KinematicMoveInput(new Vector3(0f, 0.03f, 0f), new Vector3(0f, 5f, 0f), Radius, Height, DeltaTime, ~0), map);

            Assert.That(result.position.y, Is.GreaterThan(0.03f), "오르는 중엔 바닥에 붙이면 안 된다");
            Assert.IsFalse(result.grounded);
        }
    }
}
