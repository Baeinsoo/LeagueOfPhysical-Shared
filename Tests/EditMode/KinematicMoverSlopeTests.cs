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
                    new KinematicMoveInput(pos, vel, Radius, Height, DeltaTime, ~0, stepOffset: 0f), map);
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
                new KinematicMoveInput(Vector3.zero, new Vector3(0f, -1f, 0f), Radius, Height, DeltaTime, ~0, stepOffset: 0f), map);

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
                new KinematicMoveInput(new Vector3(0f, 0.03f, 0f), new Vector3(0f, 5f, 0f), Radius, Height, DeltaTime, ~0, stepOffset: 0f), map);

            Assert.That(result.position.y, Is.GreaterThan(0.03f), "오르는 중엔 바닥에 붙이면 안 된다");
            Assert.IsFalse(result.grounded);
        }

        [Test]
        public void 턱_높이를_주면_그_이하의_턱을_오른다()
        {
            var map = new StepQuery { StepX = 1f, StepHeight = 0.1f };
            var result = KinematicMover.Move(
                new KinematicMoveInput(new Vector3(0.5f, 0f, 0f), new Vector3(10f, 0f, 0f),
                    0.35f, 1.5f, 0.1f, ~0, stepOffset: 0.15f), map);

            Assert.That(result.position.x, Is.GreaterThan(1f), "턱을 넘어가야 한다");
            Assert.That(result.position.y, Is.GreaterThan(0.05f), "턱 위로 올라가야 한다");
        }

        [Test]
        public void 턱_높이가_0이면_같은_턱에_막힌다()
        {
            //  나는 새에게 계단 오르기는 의미가 없다 — Flappy는 0을 넘긴다.
            var map = new StepQuery { StepX = 1f, StepHeight = 0.1f };
            var result = KinematicMover.Move(
                new KinematicMoveInput(new Vector3(0.5f, 0f, 0f), new Vector3(10f, 0f, 0f),
                    0.35f, 1.5f, 0.1f, ~0, stepOffset: 0f), map);

            Assert.That(result.position.x, Is.LessThan(1f), "턱 오르기를 끄면 막혀야 한다");
        }

        [Test]
        public void 내리막을_내려갈_때도_면에_붙어_매끄럽게_간다()
        {
            //  실측(2026-08-27): 39틱 내내 dy/dx ≈ -tan(32°)로 정확히 슬로프를 따라가며 흔들림이
            //  없었다(계단식 튐 없음, vel.y가 매 틱 0으로 수렴). 오르막과 달리 내리막은 몸이 지면
            //  "밖"으로 뜨는 방향이라 파묻힘 자체가 구조적으로 생기지 않는다 — 그래도 회귀로
            //  고정해 둔다: 세로 속도가 위를 향하거나(오르막 버그의 재발 신호) 몸이 경사 안으로
            //  들어가면 잡아낸다.
            var map = new HalfSpaceQuery();
            map.AddSlope(-32f, Vector3.zero);   // 내리막
            Vector3 pos = new Vector3(-1f, 0.6f, 0f);
            Vector3 vel = new Vector3(ForwardSpeed, 0f, 0f);

            for (int tick = 0; tick < 40; tick++)
            {
                vel.y -= Gravity * DeltaTime;
                var result = KinematicMover.Move(
                    new KinematicMoveInput(pos, vel, Radius, Height, DeltaTime, ~0, stepOffset: 0f), map);
                pos = result.position;
                vel = result.velocity;

                Assert.That(vel.y, Is.LessThanOrEqualTo(1e-3f),
                    $"t{tick}: 입력이 없는데 세로 속도가 +{vel.y:F2} — 경사가 몸을 밀어 올리고 있다");

                Vector3 p1 = pos + Vector3.up * Radius;
                Vector3 p2 = pos + Vector3.up * (Height - Radius);
                float clear = map.Clearance(map.Faces[0], p1, p2, Radius);
                Assert.That(clear, Is.GreaterThan(-1e-3f),
                    $"t{tick}: 이동 뒤 몸이 경사 안으로 {-clear:F4}m 파묻혔다");
            }
        }
    }
}
