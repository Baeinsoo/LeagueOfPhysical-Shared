using LOP;
using NUnit.Framework;

public class SkydiveCheckpointsTests
{
    private static readonly float[] Shelves =
        { 2600f, 2200f, 1800f, 1400f, 1000f, 600f, 200f };

    private const float SpawnY = 3000f;

    [Test]
    public void 지나온_선반_중_가장_낮은_것으로_돌아간다()
    {
        Assert.AreEqual(1800f, SkydiveCheckpoints.LastPassedShelfY(1500f, Shelves, SpawnY), 1e-4f);
    }

    [Test]
    public void 첫_선반_위에서_죽으면_스폰_고도다()
    {
        Assert.AreEqual(SpawnY, SkydiveCheckpoints.LastPassedShelfY(2800f, Shelves, SpawnY), 1e-4f);
    }

    [Test]
    public void 마지막_선반_아래에서_죽으면_마지막_선반이다()
    {
        Assert.AreEqual(200f, SkydiveCheckpoints.LastPassedShelfY(50f, Shelves, SpawnY), 1e-4f);
    }

    // 선반 고도에 정확히 있을 때. 그 선반을 "지났다"고 보면 제자리 부활이 되어 다시 그 레이저에
    // 걸린다 — 바로 위 선반으로 보낸다.
    [Test]
    public void 선반_고도에_정확히_있으면_그_위_선반으로_간다()
    {
        Assert.AreEqual(1800f, SkydiveCheckpoints.LastPassedShelfY(1400f, Shelves, SpawnY), 1e-4f);
    }

    [Test]
    public void 표의_순서가_뒤섞여_있어도_답이_같다()
    {
        var shuffled = new[] { 600f, 2600f, 200f, 1800f, 1000f, 2200f, 1400f };

        Assert.AreEqual(1800f, SkydiveCheckpoints.LastPassedShelfY(1500f, shuffled, SpawnY), 1e-4f);
    }

    [Test]
    public void 선반이_하나도_없으면_스폰_고도다()
    {
        Assert.AreEqual(SpawnY, SkydiveCheckpoints.LastPassedShelfY(1500f, new float[0], SpawnY), 1e-4f);
    }
}
