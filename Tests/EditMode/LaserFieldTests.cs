using System.Numerics;
using LOP;
using NUnit.Framework;

public class LaserFieldTests
{
    private static Laser Any(float startAngle)
        => new Laser(Vector3.Zero, 10f, 0.5f, startAngle, 0f, 0f, 0, 0, 0);

    [Test]
    public void 비어_있는_판에는_레이저가_없다()
    {
        var field = new LaserField();

        Assert.AreEqual(0, field.All.Count);
    }

    [Test]
    public void 넣은_순서대로_들어간다()
    {
        var field = new LaserField();
        field.Add(Any(1f));
        field.Add(Any(2f));

        Assert.AreEqual(2, field.All.Count);
        Assert.AreEqual(1f, field.All[0].StartAngle, 1e-5f);
        Assert.AreEqual(2f, field.All[1].StartAngle, 1e-5f);
    }

    // 라운드가 여러 판이면 맵을 다시 로드한다. 안 비우면 레이저가 두 배가 된다.
    [Test]
    public void 비우면_다_사라진다()
    {
        var field = new LaserField();
        field.Add(Any(1f));
        field.Add(Any(2f));

        field.Clear();

        Assert.AreEqual(0, field.All.Count);
    }

    // WindVolume이 실제로 겪은 실패(라운드 재로드 시 등록 중복)를 막는 게 Remove다.
    // 값이 같은 레이저 둘 중 하나를 빼도 나머지 하나는 남는다는 것까지 확인한다.
    [Test]
    public void 값이_같아도_하나만_빠진다()
    {
        var field = new LaserField();
        var laser = Any(1f);
        field.Add(laser);
        field.Add(laser);

        var removed = field.Remove(laser);

        Assert.IsTrue(removed);
        Assert.AreEqual(1, field.All.Count);
    }
}
