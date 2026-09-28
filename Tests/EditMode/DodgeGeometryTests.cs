using LOP;
using NUnit.Framework;
using UnityEngine;

public class DodgeGeometryTests
{
    [Test]
    public void 마주_보고_엇갈리면_가장_가까운_거리는_0이다()
    {
        // A는 왼→오, B는 오→왼. 틱 중간에 한 점에서 만난다 — 끝점만 비교하면 2m 떨어져 보인다.
        float d = DodgeGeometry.ClosestApproach(
            new Vector2(-1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-1, 0));
        Assert.AreEqual(0f, d, 1e-5f);
    }

    [Test]
    public void 나란히_가면_간격이_그대로다()
    {
        float d = DodgeGeometry.ClosestApproach(
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 2), new Vector2(1, 2));
        Assert.AreEqual(2f, d, 1e-5f);
    }

    [Test]
    public void 가만히_있는_두_점은_그냥_거리다()
    {
        float d = DodgeGeometry.ClosestApproach(
            new Vector2(0, 0), new Vector2(0, 0), new Vector2(3, 4), new Vector2(3, 4));
        Assert.AreEqual(5f, d, 1e-5f);
    }

    [Test]
    public void 교차하는_선분은_거리가_0이다()
    {
        float d = DodgeGeometry.SegmentDistance(
            new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, -1), new Vector2(0, 1));
        Assert.AreEqual(0f, d, 1e-5f);
    }

    [Test]
    public void 떨어진_선분은_가장_가까운_끝까지의_거리다()
    {
        float d = DodgeGeometry.SegmentDistance(
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(3, -1), new Vector2(3, 1));
        Assert.AreEqual(2f, d, 1e-5f);
    }

    [Test]
    public void 초를_틱으로_바꾸면_50Hz_반올림이고_최소_1틱이다()
    {
        Assert.AreEqual(25, DodgeConfig.Ticks(0.5f));
        Assert.AreEqual(1, DodgeConfig.Ticks(0f));
        Assert.AreEqual(60, DodgeConfig.Ticks(1.2f));
    }
}
