using GameFramework;
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

        public static KinematicMoveResult Move(in KinematicMoveInput input, ICollisionQuery query)
        {
            Vector3 pos = input.position;
            Vector3 remaining = input.velocity * input.deltaTime;
            Vector3 velocity = input.velocity;

            for (int i = 0; i < MaxSlides; i++)
            {
                float dist = remaining.magnitude;
                if (dist < 1e-5f)
                {
                    break;
                }
                Vector3 dir = remaining / dist;

                Vector3 p1 = pos + Vector3.up * input.radius;
                Vector3 p2 = pos + Vector3.up * (input.height - input.radius);
                CollisionHit hit = query.CapsuleCast(p1, p2, input.radius, dir, dist + SkinWidth, input.layerMask);
                if (hit.HasHit == false)
                {
                    pos += remaining;
                    break;
                }

                float moveDist = Mathf.Max(hit.Distance - SkinWidth, 0f);
                pos += dir * moveDist;

                // 남은 이동과 속도를 충돌면(plane)에 투영 → 벽을 따라 미끄러짐. 정면 벽이면 0으로 소멸.
                Vector3 leftover = remaining - dir * moveDist;
                remaining = Vector3.ProjectOnPlane(leftover, hit.Normal);
                velocity = Vector3.ProjectOnPlane(velocity, hit.Normal);
            }

            return new KinematicMoveResult(pos, velocity, false);
        }
    }
}
