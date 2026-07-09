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
        const float SkinWidth = 0.02f;   // 벽에서 살짝 띄우는 여유(끼임 방지)

        public static KinematicMoveResult Move(in KinematicMoveInput input, ICollisionQuery query)
        {
            Vector3 pos = input.position;
            Vector3 remaining = input.velocity * input.deltaTime;
            Vector3 velocity = input.velocity;

            float dist = remaining.magnitude;
            if (dist > 1e-5f)
            {
                Vector3 dir = remaining / dist;
                Vector3 p1 = pos + Vector3.up * input.radius;
                Vector3 p2 = pos + Vector3.up * (input.height - input.radius);
                CollisionHit hit = query.CapsuleCast(p1, p2, input.radius, dir, dist + SkinWidth, input.layerMask);
                if (hit.HasHit)
                {
                    float moveDist = Mathf.Max(hit.Distance - SkinWidth, 0f);
                    pos += dir * moveDist;
                    velocity = Vector3.zero;   // 임시: 충돌 시 정지 (다음 테스트에서 일반화)
                }
                else
                {
                    pos += remaining;
                }
            }
            return new KinematicMoveResult(pos, velocity, false);
        }
    }
}
