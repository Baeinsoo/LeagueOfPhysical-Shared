using System.Collections.Generic;
using NUnit.Framework;

namespace LOP.Tests
{
    public class KnockbackTeamsTests
    {
        static Dictionary<string, int> Teams() => new Dictionary<string, int>
        {
            ["a"] = 1, ["b"] = 2, ["c"] = 1, ["d"] = 2,
        };

        [Test]
        public void 자리는_번갈아_두_팀으로_나뉜다()
        {
            Assert.AreEqual(1, KnockbackTeams.TeamForSeat(0, 2));
            Assert.AreEqual(2, KnockbackTeams.TeamForSeat(1, 2));
            Assert.AreEqual(1, KnockbackTeams.TeamForSeat(2, 2));
            Assert.AreEqual(2, KnockbackTeams.TeamForSeat(7, 2));
        }

        [Test]
        public void 개인전은_팀이_0이다()
        {
            Assert.AreEqual(0, KnockbackTeams.TeamForSeat(0, 0));
            Assert.AreEqual(0, KnockbackTeams.TeamForSeat(5, 0));
        }

        [Test]
        public void 개인전은_아무도_같은_팀이_아니다()
        {
            Assert.IsFalse(KnockbackTeams.SameTeam(0, 0));
        }

        [Test]
        public void 같은_팀만_같은_팀이다()
        {
            Assert.IsTrue(KnockbackTeams.SameTeam(1, 1));
            Assert.IsFalse(KnockbackTeams.SameTeam(1, 2));
        }

        [Test]
        public void 한_팀만_남으면_끝이다()
        {
            Assert.IsTrue(KnockbackTeams.TeamMatchOver(Teams(), new[] { "a", "c" }));
            Assert.IsTrue(KnockbackTeams.TeamMatchOver(Teams(), new[] { "b" }));
        }

        [Test]
        public void 두_팀이_남으면_안_끝난다()
        {
            Assert.IsFalse(KnockbackTeams.TeamMatchOver(Teams(), new[] { "a", "b" }));
        }

        [Test]
        public void 아무도_안_남으면_끝이다()
        {
            Assert.IsTrue(KnockbackTeams.TeamMatchOver(Teams(), new string[0]));
        }
    }
}
