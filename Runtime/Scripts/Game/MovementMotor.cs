using UnityEngine;

namespace LOP
{
    /// <summary>이동 커널에 넣는 입력값 묶음 (클라이언트·서버 공통).</summary>
    public readonly struct MovementInput
    {
        public readonly Vector3 currentVelocity;
        public readonly float horizontal;
        public readonly float vertical;
        public readonly float speed;            // 최대 이동 속도(목표)
        public readonly float maxAcceleration;  // 목표 속도로 따라붙는 빠르기(클수록 즉각 반응)
        public readonly float deltaTime;

        public MovementInput(Vector3 currentVelocity, float horizontal, float vertical, float speed,
                             float maxAcceleration, float deltaTime)
        {
            this.currentVelocity = currentVelocity;
            this.horizontal = horizontal;
            this.vertical = vertical;
            this.speed = speed;
            this.maxAcceleration = maxAcceleration;
            this.deltaTime = deltaTime;
        }
    }

    /// <summary>이동 커널 결과. velocity=새 속도, rotation=바라볼 방향(방향 입력이 있을 때만).</summary>
    public readonly struct MovementResult
    {
        public readonly Vector3 velocity;
        public readonly bool hasRotation;
        public readonly Vector3 rotation;

        public MovementResult(Vector3 velocity, bool hasRotation, Vector3 rotation)
        {
            this.velocity = velocity;
            this.hasRotation = hasRotation;
            this.rotation = rotation;
        }
    }

    /// <summary>
    /// <b>입력 → 새 수평 속도(+바라볼 방향)</b>를 내는 순수 커널. 어느 게임이든 "사람이 땅에서
    /// 걷는" 느낌은 여기서 나온다 — 게임마다 상수를 베끼지 말고 이 함수를 부를 것. 그래야
    /// 한쪽만 고쳐져 느낌이 조용히 갈라지는 일이 안 생긴다.
    ///
    /// <para>짝이 되는 커널은 <see cref="KinematicMover"/>다. 둘의 역할이 다르다:
    /// 이쪽은 <b>얼마나 빠르게 어디로</b>(속도)를 정하고, 저쪽은 그 속도로 <b>실제로 어디까지</b>
    /// 갈 수 있는지(지형에 막히면 벽까지만 + 미끄러짐)를 정한다.</para>
    ///
    /// <para>컨텍스트가 없는 순수 함수라 <c>*System</c>이 아니다(가이드라인: static 커널에는
    /// System 이름을 붙이지 않는다). 스탯·어빌리티·넉백처럼 게임에 딸린 것을 읽어 이 커널에
    /// 넣어 주는 쪽이 <see cref="MovementSystem"/>이고, 그건 인스턴스다.</para>
    ///
    /// <para>산업 표준 매핑: 언리얼 <c>UCharacterMovementComponent::CalcVelocity</c>(가속·제동으로
    /// 새 속도를 구함)에 대응한다. 그래서 이름도 그 동사를 따랐다.</para>
    /// </summary>
    public static class MovementMotor
    {
        /// <summary>
        /// 지금 속도에서 목표 속도로 정해진 양만큼 당긴다. 입력이 있으면 목표=입력 방향×speed,
        /// 없으면 목표=0(정지). 그래서 방향전환 시 옆 관성이 안 남고, 입력을 떼면 0으로 제동해 멈춘다.
        /// 수평(좌우/앞뒤)만 다루고 수직(y)은 중력·점프 몫이라 그대로 돌려준다.
        /// </summary>
        public static MovementResult CalcVelocity(in MovementInput input)
        {
            // 좌우/앞뒤(수평) 속도만 다룬다. 위아래(y)는 중력·점프 몫이라 그대로 둔다.
            Vector3 horiz = new Vector3(input.currentVelocity.x, 0, input.currentVelocity.z);

            Vector3 dir = new Vector3(input.horizontal, 0, input.vertical);
            float push = dir.magnitude;
            bool hasRotation = push > 0f;
            Vector3 desired = Vector3.zero;  // 입력이 없으면 목표 0 → 0으로 제동(정지)
            Vector3 rotation = Vector3.zero;
            if (hasRotation)
            {
                Vector3 heading = dir / push;

                // 얼마나 밀었나가 곧 속도다 — 살짝 밀면 걷고 끝까지 밀면 뛴다.
                // 1을 넘는 건 잘라 낸다(대각선은 길이가 1.41이라 안 자르면 더 빨라진다).
                desired = heading * input.speed * Mathf.Min(push, 1f);

                // 방향은 얼마나 밀었든 그대로다 — 살살 밀어도 그 쪽을 본다.
                float angle = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg;
                rotation = new Vector3(0, angle, 0);
            }

            // 지금 속도에서 목표로 정해진 양만큼 당긴다(입력 방향 속도로, 없으면 0으로). 옆 관성이 안 남음.
            Vector3 newHoriz = Vector3.MoveTowards(horiz, desired, input.maxAcceleration * input.deltaTime);

            return new MovementResult(new Vector3(newHoriz.x, input.currentVelocity.y, newHoriz.z), hasRotation, rotation);
        }
    }
}
