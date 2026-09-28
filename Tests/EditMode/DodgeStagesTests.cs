using LOP;
using NUnit.Framework;

public class DodgeStagesTests
{
    static readonly DodgeConfig C = new DodgeConfig(3, 1.5f, 0.16f, 0.5f, 9f, 6, 2f, 1.8f, 0,
                                                   1.2f, 5f, 0.22f, 2f, 0.25f, 0.7f, 0.45f, 6f, 1.35f, 0.55f,
                                                   0.5f, 0.8f, 1.5f, 0.02f);

    // 10초(500틱)짜리 둘: 폭탄 → 레이저
    static DodgeStageTable Two() => new DodgeStageTable(new[]
    {
        new DodgeStage("폭탄", 10f, new[] { DodgePatternKind.Bomb }, 1f, 0.5f, 1.6f),
        new DodgeStage("레이저", 10f, new[] { DodgePatternKind.Laser }, 1.2f, 0.5f, 1.5f),
    });

    [Test]
    public void 시작_전에는_스테이지가_없다()
    {
        var at = Two().At(1000, long.MaxValue, C);
        Assert.IsFalse(at.Started);
        Assert.AreEqual(-1, at.Index);
        Assert.IsFalse(Two().At(99, 100, C).Started);
    }

    [Test]
    public void 경기_시작_틱에_첫_스테이지가_열린다()
    {
        var at = Two().At(100, 100, C);
        Assert.AreEqual(0, at.Index);
        Assert.AreEqual(100, at.StartTick);
        Assert.AreEqual(600, at.EndTick);
        Assert.AreEqual(0f, at.Progress, 1e-6f);
        CollectionAssert.AreEqual(new[] { DodgePatternKind.Bomb }, at.Kinds);
    }

    [Test]
    public void 경계_틱은_다음_스테이지다()
    {
        Assert.AreEqual(0, Two().At(599, 100, C).Index);
        Assert.AreEqual(1, Two().At(600, 100, C).Index);
        Assert.AreEqual(600, Two().At(600, 100, C).StartTick);
    }

    [Test]
    public void 스테이지_안에서_세기가_오른다()
    {
        var t = Two();
        var early = t.At(100, 100, C);
        var late = t.At(599, 100, C);
        Assert.AreEqual(1f, early.Intensity, 1e-5f);
        Assert.Greater(late.Intensity, early.Intensity);
        Assert.AreEqual(1f * (1f + (499f / 500f) * 0.5f), late.Intensity, 1e-4f);
    }

    [Test]
    public void 마지막_스테이지가_끝나면_서든데스이고_모든_종류를_쓴다()
    {
        var at = Two().At(1100, 100, C);
        Assert.IsTrue(at.SuddenDeath);
        Assert.AreEqual(2, at.Index);
        Assert.AreEqual(1100, at.StartTick);
        Assert.AreEqual(long.MaxValue, at.EndTick);
        Assert.AreEqual(7, at.Kinds.Length);
        Assert.AreEqual(C.SuddenDeathBase, at.Intensity, 1e-5f);
        Assert.AreEqual(C.PatternIntervalTicks, at.IntervalTicks);
    }

    [Test]
    public void 서든데스의_세기는_멈추지_않고_오른다()
    {
        var t = Two();
        float prev = 0f;
        for (long tick = 1100; tick < 1100 + 50 * 600; tick += 50 * 30)   // 10분 동안 30초마다
        {
            float now = t.At(tick, 100, C).Intensity;
            Assert.Greater(now, prev);
            prev = now;
        }
        Assert.AreEqual(C.SuddenDeathBase * (1f + 60f * C.SuddenDeathGrowth), t.At(1100 + 50 * 60, 100, C).Intensity, 1e-3f);
    }

    [Test]
    public void 표가_비면_곧바로_서든데스다()
    {
        var at = new DodgeStageTable(new DodgeStage[0]).At(100, 100, C);
        Assert.IsTrue(at.SuddenDeath);
        Assert.AreEqual(0, at.Index);
    }

    [Test]
    public void 종류가_빈_스테이지는_전부를_쓴다()
    {
        var t = new DodgeStageTable(new[] { new DodgeStage("빈", 10f, new DodgePatternKind[0], 1f, 0f, 1.8f) });
        Assert.AreEqual(7, t.At(0, 0, C).Kinds.Length);
    }
}
