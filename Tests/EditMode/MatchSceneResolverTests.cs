using System;
using NUnit.Framework;
using LOP;

public class MatchSceneResolverTests
{
    [Test]
    public void CurrentRoundIndex_라운드가_있으면_첫_라운드를_가리킨다()
    {
        Assert.AreEqual(0, MatchSceneResolver.CurrentRoundIndex(1));
        Assert.AreEqual(0, MatchSceneResolver.CurrentRoundIndex(3));
    }

    [Test]
    public void CurrentRoundIndex_라운드가_없으면_예외()
    {
        Assert.Throws<InvalidOperationException>(() => MatchSceneResolver.CurrentRoundIndex(0));
    }

    [Test]
    public void CurrentRoundIndex_음수는_없는_것과_같다()
    {
        Assert.Throws<InvalidOperationException>(() => MatchSceneResolver.CurrentRoundIndex(-1));
    }

    [Test]
    public void RequireScenePath_값이_있으면_그대로_돌려준다()
    {
        Assert.AreEqual(
            "Assets/Scenes/FlapWang.unity",
            MatchSceneResolver.RequireScenePath("TbGameMode", 1, "Assets/Scenes/FlapWang.unity"));
    }

    [Test]
    public void RequireScenePath_null이면_예외()
    {
        Assert.Throws<InvalidOperationException>(
            () => MatchSceneResolver.RequireScenePath("TbGameMode", 1, null));
    }

    [Test]
    public void RequireScenePath_공백뿐이면_예외()
    {
        Assert.Throws<InvalidOperationException>(
            () => MatchSceneResolver.RequireScenePath("TbGameMode", 1, "   "));
    }

    [Test]
    public void RequireScenePath_예외_메시지에_테이블과_id가_들어간다()
    {
        var e = Assert.Throws<InvalidOperationException>(
            () => MatchSceneResolver.RequireScenePath("TbGameMode", 42, null));

        StringAssert.Contains("TbGameMode", e.Message);
        StringAssert.Contains("42", e.Message);
    }
}
