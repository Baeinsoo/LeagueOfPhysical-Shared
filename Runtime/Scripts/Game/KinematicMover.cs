using GameFramework;
using GameFramework.Physics;
using UnityEngine;

namespace LOP
{
    /// <summary>이동 커널 입력: 시작 위치·속도·캡슐 규격·dt·충돌 레이어.</summary>
    public readonly struct KinematicMoveInput
    {
        public readonly Vector3 position;   // 발밑 기준
        public readonly Vector3 velocity;
        public readonly float radius;
        public readonly float height;
        public readonly float deltaTime;
        public readonly int layerMask;

        public KinematicMoveInput(Vector3 position, Vector3 velocity, float radius,
            float height, float deltaTime, int layerMask)
        {
            this.position = position;
            this.velocity = velocity;
            this.radius = radius;
            this.height = height;
            this.deltaTime = deltaTime;
            this.layerMask = layerMask;
        }
    }

    /// <summary>이동 커널 결과: 최종 위치·(충돌 반영) 속도·바닥 접지 여부.</summary>
    public readonly struct KinematicMoveResult
    {
        public readonly Vector3 position;
        public readonly Vector3 velocity;
        public readonly bool grounded;

        public KinematicMoveResult(Vector3 position, Vector3 velocity, bool grounded)
        {
            this.position = position;
            this.velocity = velocity;
            this.grounded = grounded;
        }
    }

    /// <summary>
    /// 속도를 캡슐 sweep으로 "벽까지만 이동 + 미끄러짐"(collide-and-slide) 처리해 최종 위치를 낸다.
    /// 클·서 공유 구체 커널(같은 코드 = 예측이 권위와 일치). 물리 쿼리는 ICollisionQuery 포트 뒤로 격리.
    /// </summary>
    public static class KinematicMover
    {
        const int MaxSlides = 4;         // 미끄러짐 반복 상한(과회전·무한루프 방지)
        const float SkinWidth = 0.02f;   // 벽에서 살짝 띄우는 여유(끼임 방지)
        const float GroundNormalY = 0.7f;  // 면 법선의 위쪽 성분이 이보다 크면 바닥(≈45도)
        const float StepOffset = 0.1f;   // 수평 sweep을 이만큼 띄운다 — 발밑 바닥에 안 걸려(캐칭 방지) + 이 높이 이하 턱은 올라감(표준 step offset)
        const float GroundProbe = 0.05f; // 발밑을 이만큼 아래까지 훑어 지면을 찾는다. 한 틱 낙하분(≈0.028)보다 넉넉하되, 떠 있는 몸을 지면으로 오인하지 않을 만큼 짧게.

        /// <summary>
        /// 표준 컨트롤러처럼 수평/수직 스텝을 분리한다. 합쳐서 처리하면 "걷는 바닥"이 수평 이동을
        /// 취소해(발이 바닥에 붙어 있어 sweep이 바닥을 dist≈0로 맞음) 제자리에 낀다. 나눠서:
        /// (1) 수평은 캡슐을 StepOffset만큼 띄워 sweep → 벽만 막고 발밑 바닥은 통과.
        /// (2) 수직은 발밑에서 sweep → 바닥/천장에서 멈추고 접지 판정.
        /// </summary>
        public static KinematicMoveResult Move(in KinematicMoveInput input, ICollisionQuery query)
        {
            Vector3 pos = input.position;

            // (0) 지면 찾기 — 매 틱 다시 잰다(상태를 들지 않아야 롤백 재생이 라이브와 같은 답을 낸다).
            //     찾으면 바닥에서 SkinWidth만큼 띄운다: 딱 붙은 채로 수평 sweep을 쏘면 거리 0으로
            //     맞아 한 발도 못 나간다.
            //     올라가는 중에는 지면으로 치지 않는다 — 그러면 날갯짓해 뜨는 몸을 도로 붙여 버린다.
            bool onGround = false;
            Vector3 groundNormal = Vector3.up;
            if (input.velocity.y <= 0f)
            {
                CollisionHit floor = Cast(pos, SkinWidth, Vector3.down, SkinWidth + GroundProbe, input, query);
                if (floor.HasHit && floor.Normal.y >= GroundNormalY)
                {
                    onGround = true;
                    groundNormal = floor.Normal;
                    //  탐침은 SkinWidth 올린 자리에서 쐈으므로 실제 여유 = Distance - SkinWidth.
                    //  그 여유를 SkinWidth로 맞춘다.
                    pos.y += 2f * SkinWidth - floor.Distance;
                }
            }

            // (1) 수평 collide-and-slide — 캡슐을 StepOffset 띄워 sweep(발밑 바닥 캐칭 방지).
            Vector3 horizVel = new Vector3(input.velocity.x, 0f, input.velocity.z);
            Vector3 remaining = horizVel * input.deltaTime;
            for (int i = 0; i < MaxSlides; i++)
            {
                float dist = remaining.magnitude;
                if (dist < 1e-5f)
                {
                    break;
                }
                Vector3 dir = remaining / dist;
                CollisionHit hit = Cast(pos, StepOffset, dir, dist + SkinWidth, input, query);
                if (hit.HasHit == false)
                {
                    pos += remaining;
                    break;
                }
                float moveDist = Mathf.Max(hit.Distance - SkinWidth, 0f);
                pos += dir * moveDist;
                Vector3 leftover = remaining - dir * moveDist;
                remaining = Vector3.ProjectOnPlane(leftover, hit.Normal);
                horizVel = Vector3.ProjectOnPlane(horizVel, hit.Normal);
            }

            // (2) 수직 스텝(중력/점프) — 발밑에서 sweep. 바닥/천장에 닿으면 멈추고 수직 속도 소멸.
            bool grounded = onGround;
            float vy = input.velocity.y;
            float vDist = Mathf.Abs(vy) * input.deltaTime;
            if (vDist > 1e-5f)
            {
                Vector3 vDir = new Vector3(0f, Mathf.Sign(vy), 0f);
                CollisionHit vHit = Cast(pos, 0f, vDir, vDist + SkinWidth, input, query);
                if (vHit.HasHit)
                {
                    pos += vDir * Mathf.Max(vHit.Distance - SkinWidth, 0f);
                    if (vHit.Normal.y >= GroundNormalY)
                    {
                        grounded = true;
                    }
                    vy = 0f;
                }
                else
                {
                    pos += vDir * vDist;
                }
            }

            return new KinematicMoveResult(pos, new Vector3(horizVel.x, vy, horizVel.z), grounded);
        }

        // 발밑(pos)에서 lift만큼 올린 캡슐로 sweep. lift=0이면 발밑 기준.
        private static CollisionHit Cast(Vector3 pos, float lift, Vector3 dir, float dist,
            in KinematicMoveInput input, ICollisionQuery query)
        {
            Vector3 basePos = pos + Vector3.up * lift;
            Vector3 p1 = basePos + Vector3.up * input.radius;
            Vector3 p2 = basePos + Vector3.up * (input.height - input.radius);
            return query.CapsuleCast(p1, p2, input.radius, dir, dist, input.layerMask);
        }
    }
}
