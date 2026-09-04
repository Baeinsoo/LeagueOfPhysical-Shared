using System.Numerics;
using LOP;
using NUnit.Framework;

public class LaserGeometryTests
{
    private static Laser Rotating(float startAngle, float angularSpeed)
        => new Laser(Vector3.Zero, length: 10f, radius: 0.5f,
                     startAngle: startAngle, angularSpeed: angularSpeed,
                     sweepHalfRange: 0f, period: 0, onTicks: 0, phase: 0);

    [Test]
    public void 고정_빔은_각도가_변하지_않는다()
    {
        Laser laser = Rotating(0.5f, 0f);

        Assert.AreEqual(0.5f, LaserGeometry.Angle(laser, 0f), 1e-5f);
        Assert.AreEqual(0.5f, LaserGeometry.Angle(laser, 100f), 1e-5f);
    }

    [Test]
    public void 회전_빔은_틱에_비례해_돈다()
    {
        Laser laser = Rotating(0f, 0.1f);

        Assert.AreEqual(1.0f, LaserGeometry.Angle(laser, 10f), 1e-5f);
    }

    // 삼각파의 네 경계. 접는 지점이 틀리면 왕복이 튄다.
    [Test]
    public void 삼각파는_네_경계를_지난다()
    {
        const float half = 2f;

        Assert.AreEqual(0f, LaserGeometry.Fold(0f, half), 1e-5f);
        Assert.AreEqual(half, LaserGeometry.Fold(half, half), 1e-5f);
        Assert.AreEqual(0f, LaserGeometry.Fold(2f * half, half), 1e-5f);
        Assert.AreEqual(-half, LaserGeometry.Fold(3f * half, half), 1e-5f);
        Assert.AreEqual(0f, LaserGeometry.Fold(4f * half, half), 1e-5f);
    }

    [Test]
    public void 삼각파는_음수_입력에도_같은_주기를_돈다()
    {
        const float half = 2f;

        Assert.AreEqual(LaserGeometry.Fold(1f, half), LaserGeometry.Fold(1f - 4f * half, half), 1e-5f);
    }

    [Test]
    public void 삼각파는_범위를_벗어나지_않는다()
    {
        const float half = 3f;

        for (int i = 0; i < 200; i++)
        {
            float folded = LaserGeometry.Fold(i * 0.37f, half);
            Assert.That(folded, Is.InRange(-half - 1e-4f, half + 1e-4f));
        }
    }

    [Test]
    public void 왕복_빔은_시작각을_중심으로_흔들린다()
    {
        var laser = new Laser(Vector3.Zero, 10f, 0.5f,
                              startAngle: 1f, angularSpeed: 0.5f, sweepHalfRange: 1f,
                              period: 0, onTicks: 0, phase: 0);

        Assert.AreEqual(1f, LaserGeometry.Angle(laser, 0f), 1e-5f);
        Assert.AreEqual(2f, LaserGeometry.Angle(laser, 2f), 1e-5f);   // 진행각 1.0 = half → 정점
    }

    [Test]
    public void 선분은_피벗에서_길이만큼_뻗는다()
    {
        Laser laser = Rotating(0f, 0f);

        LaserGeometry.SegmentAt(laser, 0f, out Vector3 a, out Vector3 b);

        Assert.AreEqual(Vector3.Zero, a);
        Assert.AreEqual(10f, b.X, 1e-4f);
        Assert.AreEqual(0f, b.Y, 1e-4f);
        Assert.AreEqual(0f, b.Z, 1e-4f);
    }

    [Test]
    public void 주기가_없으면_늘_켜져_있다()
    {
        Laser laser = Rotating(0f, 0f);

        Assert.IsTrue(LaserGeometry.Lit(laser, 0));
        Assert.IsTrue(LaserGeometry.Lit(laser, 12345));
    }

    [Test]
    public void 점멸은_주기_안에서_켜졌다_꺼진다()
    {
        var laser = new Laser(Vector3.Zero, 10f, 0.5f, 0f, 0f, 0f,
                              period: 10, onTicks: 4, phase: 0);

        Assert.IsTrue(LaserGeometry.Lit(laser, 0));
        Assert.IsTrue(LaserGeometry.Lit(laser, 3));
        Assert.IsFalse(LaserGeometry.Lit(laser, 4));
        Assert.IsFalse(LaserGeometry.Lit(laser, 9));
        Assert.IsTrue(LaserGeometry.Lit(laser, 10));
    }

    [Test]
    public void 위상이_점멸을_밀어_준다()
    {
        var laser = new Laser(Vector3.Zero, 10f, 0.5f, 0f, 0f, 0f,
                              period: 10, onTicks: 4, phase: 5);

        Assert.IsFalse(LaserGeometry.Lit(laser, 0));
        Assert.IsTrue(LaserGeometry.Lit(laser, 5));
    }
}
