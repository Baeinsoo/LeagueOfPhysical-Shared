namespace LOP
{
    /// <summary>
    /// 지금 몸이 어떤 식으로 움직이고 있나. 젤다 <i>왕국의 눈물</i>이 <b>걷기 / 점프 /
    /// 스카이다이빙 / 패러세일</b>을 서로 다르게 다루는 것과 같은 구분이다 — 상태마다 하강 속도도,
    /// 좌우 조작도 다르다.
    ///
    /// <para>컴포넌트로 저장하지 않고 매 틱 다시 판단한다(<see cref="SkydiveMotion.Resolve"/>).
    /// 판단 근거(접지·패러세일·세로 속도)가 이미 전부 저장·복원되는 값이라, 되감아도 같은 답이
    /// 나온다 — 저장하면 롤백이 되돌려야 할 것만 하나 늘어난다.</para>
    /// </summary>
    public enum SkydiveMotionState
    {
        /// <summary>발을 딛고 있다. 입력이 곧 속도와 방향이다.</summary>
        Walking,

        /// <summary>
        /// 뛰어올라 <b>착지하기 전까지</b>. 좌우 입력을 받지 않고 이륙할 때의 수평 속도를 그대로
        /// 들고 간다 — 젤다에서 점프가 그렇다. 뛰기 전에 방향을 정해야 한다.
        /// </summary>
        Jumping,

        /// <summary>떨어지는 중. 자세(대자~다이브)가 하강 속도와 좌우 조작을 정한다.</summary>
        Skydiving,

        /// <summary>패러세일을 편 상태. 천천히 내려오며 좌우가 가장 잘 든다.</summary>
        Gliding,
    }

    /// <summary>이동 상태를 판단하는 순수 함수. 상태를 들지 않으므로 클·서가 같은 답을 낸다.</summary>
    public static class SkydiveMotion
    {
        /// <param name="jumping">뛰어올라 아직 착지하지 않았나(<see cref="JumpState"/>).</param>
        public static SkydiveMotionState Resolve(bool grounded, bool jumping, bool gliding)
        {
            if (grounded)
            {
                return SkydiveMotionState.Walking;
            }

            // 패러세일이 점프보다 먼저다 — 뛰자마자 펴면 그 순간부터 활공이고, 조작이 잠기면 안 된다.
            if (gliding)
            {
                return SkydiveMotionState.Gliding;
            }

            // 착지할 때까지 점프다. 걸어서 가장자리를 넘은 것은 여기 안 걸려 곧바로 조종된다.
            if (jumping)
            {
                return SkydiveMotionState.Jumping;
            }

            return SkydiveMotionState.Skydiving;
        }
    }
}
