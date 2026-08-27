using GameFramework;
using GameFramework.Physics;
using UnityEngine;

namespace LOP
{
    /// <summary>이동 커널 입력: 시작 위치·속도·캡슐 규격·dt·충돌 레이어·턱 높이.</summary>
    public readonly struct KinematicMoveInput
    {
        public readonly Vector3 position;   // 발밑 기준
        public readonly Vector3 velocity;
        public readonly float radius;
        public readonly float height;
        public readonly float deltaTime;
        public readonly int layerMask;
        //  막혔을 때 이 높이까지는 넘어가 본다. 0이면 턱 오르기를 아예 안 한다.
        //  예전엔 커널 상수로 모든 수평 sweep을 이만큼 들어올렸는데, 그게 오르막에서 몸을
        //  파묻히게 만들었다. 이제는 "막혔을 때만" 쓰는 값이라 게임이 정한다.
        public readonly float stepOffset;

        public KinematicMoveInput(Vector3 position, Vector3 velocity, float radius,
            float height, float deltaTime, int layerMask, float stepOffset)
        {
            this.position = position;
            this.velocity = velocity;
            this.radius = radius;
            this.height = height;
            this.deltaTime = deltaTime;
            this.layerMask = layerMask;
            this.stepOffset = stepOffset;
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
        const float GroundProbe = 0.05f; // 발밑을 이만큼 아래까지 훑어 지면을 찾는다. 한 틱 낙하분(≈0.028)보다 넉넉하되, 떠 있는 몸을 지면으로 오인하지 않을 만큼 짧게.

        /// <summary>
        /// 표준 컨트롤러처럼 수평/수직 스텝을 분리한다. 합쳐서 처리하면 "걷는 바닥"이 수평 이동을
        /// 취소해(발이 바닥에 붙어 있어 sweep이 바닥을 dist≈0로 맞음) 제자리에 낀다. 나눠서:
        /// (0) 먼저 발밑 지면을 찾는다 — 있으면 이동을 그 평면에 투영해 경사를 따라간다.
        /// (1) 수평은 실제 몸 자리에서 sweep → 지면 위면 정면으로 안 부딪히니 안 막힌다. 벽처럼
        ///     못 걷는 면에 막히면 그때만 턱 오르기를 시도한다.
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

            // (1) 수평 collide-and-slide — 실제 몸 자리에서 검사한다.
            //     지면 위면 이동을 지면 평면에 투영해 경사를 "따라" 간다. 그래야 sweep이 바닥을
            //     정면으로 만나지 않아, 예전처럼 캡슐을 들어올려 속일 필요가 없다.
            //     (들어올리면 검사한 몸과 옮기는 몸이 달라져 오르막에서 실제 몸이 언덕에 파묻혔다.)
            Vector3 horizVel = new Vector3(input.velocity.x, 0f, input.velocity.z);
            Vector3 remaining = horizVel * input.deltaTime;
            if (onGround)
            {
                //  경사를 "따라" 가되 수평 진행은 깎지 않는다. 평면에 그냥 투영하면 수평 성분이
                //  cos²θ만큼 줄어 언덕이 감속 구간이 되고, 내리막이 평지보다 느려진다(32°에서 -28%).
                //  수평 성분은 그대로 두고 세로만 램프에 얹는다 — 언리얼 CMC의 기본값
                //  (bMaintainHorizontalGroundVelocity)이 하는 것과 같다.
                //  groundNormal.y >= GroundNormalY(0.7)이므로 0으로 나눌 일은 없다.
                remaining.y = -(remaining.x * groundNormal.x + remaining.z * groundNormal.z) / groundNormal.y;
            }
            for (int i = 0; i < MaxSlides; i++)
            {
                float dist = remaining.magnitude;
                if (dist < 1e-5f)
                {
                    break;
                }
                Vector3 dir = remaining / dist;
                CollisionHit hit = Cast(pos, 0f, dir, dist + SkinWidth, input, query);
                if (hit.HasHit == false)
                {
                    pos += remaining;
                    break;
                }
                float moveDist = Mathf.Max(hit.Distance - SkinWidth, 0f);
                pos += dir * moveDist;
                Vector3 leftover = remaining - dir * moveDist;

                //  걸을 수 없는 면(벽·턱)에 막혔을 때만 넘어가 본다.
                if (input.stepOffset > 0f && hit.Normal.y < GroundNormalY
                    && TryStepUp(ref pos, leftover, input, query))
                {
                    break;
                }

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

        // 막힌 앞을 넘어가 본다: 위로 들었다 → 앞으로 쓸고 → 다시 내려 착지.
        // 착지면이 걸을 수 있는 면일 때만 채택한다 — 그래야 벽을 기어오르지 않는다.
        // 성공하면 pos를 옮기고 true. 표준 컨트롤러(언리얼 CMC StepUp)의 3-sweep 그대로다.
        private static bool TryStepUp(ref Vector3 pos, Vector3 leftover,
            in KinematicMoveInput input, ICollisionQuery query)
        {
            float dist = leftover.magnitude;
            if (dist < 1e-5f)
            {
                return false;
            }
            Vector3 dir = leftover / dist;

            CollisionHit up = Cast(pos, 0f, Vector3.up, input.stepOffset + SkinWidth, input, query);
            float rise = up.HasHit ? Mathf.Max(up.Distance - SkinWidth, 0f) : input.stepOffset;
            if (rise <= SkinWidth)
            {
                return false;   // 머리 위가 막혀 못 올라간다
            }

            Vector3 lifted = pos + Vector3.up * rise;
            CollisionHit forward = Cast(lifted, 0f, dir, dist + SkinWidth, input, query);
            float advance = forward.HasHit ? Mathf.Max(forward.Distance - SkinWidth, 0f) : dist;
            if (advance <= SkinWidth)
            {
                return false;   // 올려도 못 지나간다 = 진짜 벽
            }

            Vector3 ahead = lifted + dir * advance;
            CollisionHit down = Cast(ahead, 0f, Vector3.down, rise + SkinWidth, input, query);
            if (down.HasHit == false || down.Normal.y < GroundNormalY)
            {
                return false;   // 발 디딜 곳이 아니다
            }

            pos = ahead + Vector3.down * Mathf.Max(down.Distance - SkinWidth, 0f);
            return true;
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
