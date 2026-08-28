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
            //  ⚠️ 이 두 단언은 옛(버그) 커널에서도 통과한다 — 내리막은 구조적으로 안 깨지는
            //  방향이라 "이번 수정의 증거"가 아니라 "미래 회귀를 막는 가드"일 뿐이다. 내리막이
            //  실제로 매끄럽다는 근거는 이 단언이 아니라 이 태스크의 Step 7에서 직접 찍어 본
            //  궤적(dy/dx가 매 틱 -tan32°와 정확히 일치, 계단식 튐 없음)이다.
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

        [Test]
        public void 완전히_멈춘_몸도_지면_탐침을_받아_바닥에서_뜬다()
        {
            //  지면 탐침 게이트는 `velocity.y <= 0f`다 — `< 0f`가 아니다. 스턴 중인 새처럼
            //  velocity 전체가 정확히 0인 몸도 게이트를 통과해야 한다. `< 0f`로 바꾸면 이 탐침이
            //  안 돌아 몸이 바닥에 딱 붙은 채로 남고, 다음 틱 수평 sweep이 거리 0으로 막혀
            //  제자리에 낀다 — 이 슬라이스가 고친 "캐칭" 버그가 그대로 재발한다.
            //  (실제로 게이트를 `< 0f`로 바꿔 돌려서 이 테스트가 빨간불이 되는 것을 확인했다 —
            //  task-3-report.md의 "2번 확인" 절 참고.)
            var map = new HalfSpaceQuery();
            map.AddGround(0f);

            var result = KinematicMover.Move(
                new KinematicMoveInput(Vector3.zero, Vector3.zero, Radius, Height, DeltaTime, ~0, stepOffset: 0f), map);

            Assert.That(result.position.y, Is.EqualTo(0.02f).Within(1e-3f),
                "완전히 멈춘 몸도 SkinWidth만큼 떠야 한다");
            Assert.IsTrue(result.grounded);
        }

        [Test]
        public void 턱_오르기를_켠_채_계속_막혀도_캐스트_예산_안에서_끝난다()
        {
            //  FlapWang이 실제로 쓰는 stepOffset(0.1)로, 낙하 중(중력 있음) 벽에 막힌 채 미는
            //  경로를 재현한다. stepOffset=0인 AlwaysBlocked_TerminatesWithinMaxSlides
            //  (KinematicMoverTests.cs)는 이 경로를 안 밟는다 — 턱 오르기가 막힐 때마다 위·앞
            //  2-sweep을 추가로 쓰기 때문에(아래로는 못 내려가 착지 sweep까진 안 감 — "올려도
            //  못 지나간다"에서 먼저 실패) 예산이 다르다.
            //  실측: 지면 탐침 1 + 슬라이드 4회×(주 sweep 1 + TryStepUp 2) + 수직 스텝 1 = 14.
            //  여유를 얹어 20을 예산으로 잡는다 — "무한 루프 방지"가 아니라 "이 예산을 넘지
            //  않는다"를 지키는 게 이 테스트의 목적이다.
            const int ExpectedBudget = 20;
            var query = new AlwaysBlockedCountingQuery();
            KinematicMover.Move(
                new KinematicMoveInput(Vector3.zero, new Vector3(10f, -5f, 0f), Radius, Height, DeltaTime, ~0,
                    stepOffset: 0.1f), query);

            Assert.That(query.CastCount, Is.LessThanOrEqualTo(ExpectedBudget),
                $"턱 오르기를 켠 채 계속 막히면 캐스트가 예산({ExpectedBudget})을 넘으면 안 된다");
        }

        //  어느 방향으로 sweep하든 못 걷는 벽(수평 법선)으로 막는다 — TryStepUp의 위/아래 sweep도
        //  다 막아 "올려도 못 지나간다"로 매번 실패시켜, 슬라이드 루프가 상한까지 도는 최악 경로를 만든다.
        private class AlwaysBlockedCountingQuery : GameFramework.Physics.ICollisionQuery
        {
            public int CastCount;

            public GameFramework.Physics.CollisionHit CapsuleCast(Vector3 p1, Vector3 p2, float radius,
                Vector3 direction, float distance, int layerMask)
            {
                CastCount++;
                if (Mathf.Abs(direction.y) > 0.5f)
                {
                    return GameFramework.Physics.CollisionHit.None;   // 위/아래는 뚫려 있다 — 턱 오르기 시도 자체는 계속 일어나야 최악 경로가 나온다
                }
                return new GameFramework.Physics.CollisionHit(true, 0.01f,
                    new Vector3(-0.7071f, 0f, -0.7071f), Vector3.zero, null);
            }

            public GameFramework.Physics.CollisionHit Raycast(Vector3 origin, Vector3 direction, float distance, int layerMask)
                => GameFramework.Physics.CollisionHit.None;

            public GameFramework.Physics.CollisionHit[] OverlapSphere(Vector3 center, float radius, int layerMask)
                => System.Array.Empty<GameFramework.Physics.CollisionHit>();
        }
    }
}
