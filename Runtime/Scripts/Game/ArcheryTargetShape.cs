namespace LOP
{
    /// <summary>
    /// 과녁의 생김새. 판정하는 법과 "맞은 자리"의 뜻이 여기서 갈린다.
    /// <b>데이터가 고르는 값이지 인터페이스가 아니다</b> — 두 모양 모두 클·서가 같은 코드를 돌린다.
    /// </summary>
    public enum ArcheryTargetShape
    {
        /// <summary>공. 어느 쪽에서 와도 맞는다. 띠를 나눌 면이 없으므로 띠는 하나여야 한다.</summary>
        Sphere = 0,

        /// <summary>사수를 향해 선 원판. 뒤에서 온 화살은 안 맞고, 맞은 자리로 띠를 가른다.</summary>
        Face = 1,
    }
}
