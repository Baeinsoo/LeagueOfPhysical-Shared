namespace LOP
{
    /// <summary>
    /// 뛰어올라 아직 착지하지 않았다. 젤다처럼 <b>점프 중에는 좌우 조작이 잠기는데</b>, 그 잠금이
    /// 언제 풀리는지(착지)를 알려면 "뛰었다"는 사실을 들고 있어야 한다 — 걸어서 가장자리를 넘은
    /// 것과 구분되는 유일한 근거다. 둘 다 그냥 떠 있는 상태라 위치·속도만 봐서는 못 가른다.
    ///
    /// <para>이동 상태 중 이것만 저장한다(<see cref="SkydiveSavedState"/>). 나머지 상태는
    /// 접지·패러세일·세로 속도에서 매 틱 다시 구할 수 있지만, 이건 과거에 일어난 일이라 그럴 수 없다.</para>
    /// </summary>
    public class JumpState : GameFramework.World.Component
    {
        public bool IsJumping;
    }
}
