using LOP;
using NUnit.Framework;
using UnityEngine;

public class DodgeRefereeTests
{
    static readonly DodgeConfig C = new DodgeConfig(3, 1.5f, 0.16f, 0.5f, 9f, 6, 2f, 1.8f, 0,
                                                   1.2f, 5f, 0.22f, 2f, 0.25f, 0.7f, 0.45f, 6f, 1.35f, 0.55f);
    // 10초 탄막 → 10초 수박 → (끝나면 서든데스: 전부 = 탄막 포함)
    static readonly DodgeStageTable T = new DodgeStageTable(new[]
    {
        new DodgeStage("슬리퍼", 10f, new[] { DodgePatternKind.Ring, DodgePatternKind.Spiral }, 1f, 0f, 1.8f),
        new DodgeStage("수박", 10f, new[] { DodgePatternKind.Bomb }, 1f, 0f, 1.6f),
    });
    static int Walk => DodgeConfig.Ticks(DodgeReferee.WalkSeconds);
    static DodgeReferee.Pose At(long tick) => DodgeReferee.PoseAt(tick, 0, T, C);

    [Test]
    public void 경기_전에는_바깥에_있다() => Assert.AreEqual(DodgeReferee.Gate, DodgeReferee.PoseAt(5, 100, T, C).Position);

    [Test]
    public void 탄막_스테이지가_열리면_뛰어_들어와_가운데에_선다()
    {
        Assert.AreEqual(DodgeReferee.Gate, At(0).Position);
        var mid = At(Walk / 2).Position;
        Assert.Less(mid.magnitude, DodgeReferee.Gate.magnitude);
        Assert.Greater(At(Walk / 2).Velocity.magnitude, 0f);
        Assert.AreEqual(DodgeReferee.Spot, At(Walk).Position);
        Assert.AreEqual(Vector2.zero, At(Walk + 10).Velocity);
    }

    [Test]
    public void 탄막_스테이지가_끝나면_나간다()
    {
        Assert.AreEqual(DodgeReferee.Spot, At(499).Position);   // 10초 = 500틱
        Assert.Greater((At(500 + Walk / 2).Position - DodgeReferee.Spot).magnitude, 0.1f);
        Assert.AreEqual(DodgeReferee.Gate, At(500 + Walk).Position);
        Assert.AreEqual(DodgeReferee.Gate, At(700).Position);
    }

    [Test]
    public void 서든데스에는_다시_들어온다()
    {
        Assert.AreEqual(DodgeReferee.Spot, At(1000 + Walk).Position);
    }

    [Test]
    public void 탄막_종류가_있는_스테이지에서만_던진다()
    {
        Assert.IsTrue(DodgeReferee.Throws(T.At(0, 0, C)));
        Assert.IsFalse(DodgeReferee.Throws(T.At(600, 0, C)));
        Assert.IsTrue(DodgeReferee.Throws(T.At(1100, 0, C)));
    }
}
