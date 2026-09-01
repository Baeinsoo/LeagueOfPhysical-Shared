namespace LOP
{
    /// <summary>
    /// 지금 몸이 어떤 식으로 움직이고 있나. 젤다 <i>왕국의 눈물</i>과 같은 구분이다 —
    /// 땅을 딛고 있거나, 그냥 떨어지거나, 스카이다이빙 중이거나.
    /// </summary>
    public enum SkydiveMotionState
    {
        /// <summary>발을 딛고 있다. 입력이 곧 속도와 방향이다.</summary>
        Walking,

        /// <summary>
        /// 떠 있지만 아직 스카이다이빙에 못 들어갔다 — 뛰어오른 중이거나, 낮은 데서 떨어지는 중.
        /// <b>좌우 입력을 받지 않고</b> 이륙할 때의 수평 속도를 그대로 들고 간다. 젤다에서 점프가
        /// 그렇다: 뛰기 전에 방향을 정해야 한다.
        /// </summary>
        Falling,

        /// <summary>
        /// 스카이다이빙 중. <b>자세 슬라이더가 자유롭게 먹는다</b>(대자·다이브·패러세일).
        /// 한 번 들어오면 <b>착지할 때까지</b> 유지된다 — 지면이 가까워졌다고 자세를 뺏지 않는다.
        /// 젤다도 패러세일을 땅에 닿기 직전까지 펼 수 있다.
        /// </summary>
        Skydiving,
    }

    /// <summary>
    /// 이동 상태를 저장하는 컴포넌트. 상태 하나만 들면 되는 이유는, 전이 조건 중 <b>"발밑 여유"가
    /// 진입 시점에 한 번만</b> 쓰이기 때문이다 — 그 한 번을 기억하지 않으면 매 틱 다시 재게 되고,
    /// 그러면 지면에 가까워질 때 자세를 도로 뺏긴다(패러세일이 착지 직전에 접히는 문제).
    /// </summary>
    public class MotionState : GameFramework.World.Component
    {
        public SkydiveMotionState Value;
    }

    /// <summary>상태 전이를 계산하는 순수 함수. 클·서가 같은 답을 낸다.</summary>
    public static class SkydiveMotion
    {
        /// <param name="current">지난 틱의 상태.</param>
        /// <param name="grounded">발을 딛고 있나.</param>
        /// <param name="hasClearanceBelow">발밑이 스카이다이빙에 들어갈 만큼 비어 있나.</param>
        public static SkydiveMotionState Advance(SkydiveMotionState current, bool grounded,
                                                 bool hasClearanceBelow)
        {
            // 닿으면 무조건 걷기로 돌아온다 — 자세도 여기서 풀린다.
            if (grounded)
            {
                return SkydiveMotionState.Walking;
            }

            // 한 번 들어온 스카이다이빙은 착지 전까지 유지한다. 발밑 여유를 매 틱 다시 보면
            // 땅이 가까워질 때 패러세일이 강제로 접혀 그대로 처박힌다.
            if (current == SkydiveMotionState.Skydiving)
            {
                return SkydiveMotionState.Skydiving;
            }

            // 떠 있고 발밑이 비면 그때 들어간다. 뛰어오른 중이든 걸어서 벗어났든 같다.
            return hasClearanceBelow ? SkydiveMotionState.Skydiving : SkydiveMotionState.Falling;
        }
    }
}
