using NUnit.Framework;
using LOP;

public class KnockbackMatchModeTests
{
    [Test]
    public void ZeroTeams_IsNotTeam()
    {
        Assert.IsFalse(new KnockbackMatchMode(0).IsTeam);
    }

    [Test]
    public void TwoTeams_IsTeam()
    {
        Assert.IsTrue(new KnockbackMatchMode(2).IsTeam);
    }

    [Test]
    public void Solo_HasZeroTeams()
    {
        Assert.AreEqual(0, KnockbackMatchMode.Solo.TeamCount);
        Assert.IsFalse(KnockbackMatchMode.Solo.IsTeam);
    }
}
