namespace LOP
{
    /// <summary>이 판이 개인전인지 팀전인지. 팀 수 0 = 개인전, 2 이상 = 팀전.</summary>
    public sealed class KnockbackMatchMode
    {
        public static readonly KnockbackMatchMode Solo = new KnockbackMatchMode(0);

        public int TeamCount { get; }
        public bool IsTeam => TeamCount >= 2;

        public KnockbackMatchMode(int teamCount)
        {
            TeamCount = teamCount;
        }
    }
}
