using System.Collections.Generic;

namespace LOP
{
    /// <summary>넉백 팀전의 팀 규칙. 팀 번호 0 = 팀 없음(개인전), 팀전은 1과 2.</summary>
    public static class KnockbackTeams
    {
        /// <summary>자리를 번갈아 두 팀으로 나눈다. 개인전(팀 수 0)이면 0.</summary>
        public static int TeamForSeat(int seat, int teamCount)
        {
            return teamCount < 2 ? 0 : seat % 2 + 1;
        }

        /// <summary>팀 0은 "팀 없음"이라 서로 같은 팀이 아니다.</summary>
        public static bool SameTeam(int a, int b) => a != 0 && a == b;

        /// <summary>살아 있는 사람들의 팀이 하나 이하면 끝.</summary>
        public static bool TeamMatchOver(IReadOnlyDictionary<string, int> teamOf, IReadOnlyCollection<string> alive)
        {
            var seen = new HashSet<int>();
            foreach (var id in alive)
            {
                seen.Add(teamOf.TryGetValue(id, out var team) ? team : 0);
            }
            return seen.Count <= 1;
        }
    }
}
