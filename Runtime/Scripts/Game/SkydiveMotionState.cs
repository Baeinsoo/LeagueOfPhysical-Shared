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
        /// 뛰어올라 아직 올라가는 중. <b>좌우 입력을 받지 않고 이륙할 때의 수평 속도를 그대로
        /// 들고 간다</b> — 젤다에서 점프가 그렇다. 뛰기 전에 방향을 정해야 한다.
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
        /// <param name="verticalSpeed">이번 틱 시작 시점의 세로 속도(위가 +).</param>
        public static SkydiveMotionState Resolve(bool grounded, bool gliding, float verticalSpeed)
        {
            if (grounded)
            {
                return SkydiveMotionState.Walking;
            }

            // 올라가는 동안만 점프다. 정점을 지나 내려가기 시작하면 조종이 돌아온다 —
            // 젤다도 뛰는 동안은 못 꺾지만 떨어지기 시작하면 방향을 잡을 수 있다.
            // 발판에서 그냥 걸어 나가면 세로 속도가 곧장 음수라 이 갈래에 안 걸린다(조종 가능).
            if (verticalSpeed > 0f)
            {
                return SkydiveMotionState.Jumping;
            }

            if (gliding)
            {
                return SkydiveMotionState.Gliding;
            }

            return SkydiveMotionState.Skydiving;
        }
    }
}
