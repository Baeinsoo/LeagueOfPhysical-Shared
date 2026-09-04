using LOP;
using NUnit.Framework;
using UnityEngine;

public class WindVolumeVisualizerTests
{
    [Test]
    public void 밀린_화살표는_끝에_닿으면_반대쪽에서_다시_들어온다()
    {
        // 높이 100 볼륨에서 위쪽 끝(+50) 근처 화살표를 20 더 밀면 아래쪽(-30)에서 나온다.
        float wrapped = WindVolumeVisualizer.WrapAlong(along: 40f, offset: 20f, extent: 100f);

        Assert.AreEqual(-40f, wrapped, 0.001f);
    }

    [Test]
    public void 끝에_닿기_전에는_그냥_밀린다()
    {
        float moved = WindVolumeVisualizer.WrapAlong(along: -10f, offset: 20f, extent: 100f);

        Assert.AreEqual(10f, moved, 0.001f);
    }

    [Test]
    public void 여러_바퀴를_돌아도_볼륨_안에_남는다()
    {
        float wrapped = WindVolumeVisualizer.WrapAlong(along: 0f, offset: 1234.5f, extent: 100f);

        Assert.That(wrapped, Is.InRange(-50f, 50f));
    }

    [Test]
    public void 세로_바람은_원기둥_높이만큼_흐른다()
    {
        float extent = WindVolumeVisualizer.FlowExtent(
            new Vector3(10f, 0f, 0f), Vector3.up, radius: 25f, height: 120f);

        Assert.AreEqual(120f, extent, 0.001f);
    }

    [Test]
    public void 가운데를_지나는_가로_바람은_지름만큼_흐른다()
    {
        float extent = WindVolumeVisualizer.FlowExtent(
            Vector3.zero, Vector3.forward, radius: 150f, height: 400f);

        Assert.AreEqual(300f, extent, 0.001f);
    }

    // 지름으로 감으면 가장자리 화살표가 되감길 때 원기둥 밖으로 튀어나온다. 그 자리에서
    // 실제로 지나갈 수 있는 현(chord) 길이여야 한다.
    [Test]
    public void 가장자리를_지나는_가로_바람은_짧게_흐른다()
    {
        // 바람이 +Z, 화살표가 x=±r이면 그 자리의 현 길이는 0이다.
        float atEdge = WindVolumeVisualizer.FlowExtent(
            new Vector3(150f, 0f, 0f), Vector3.forward, radius: 150f, height: 400f);
        // x=90이면 현의 절반이 sqrt(150²-90²)=120 → 전체 240.
        float inside = WindVolumeVisualizer.FlowExtent(
            new Vector3(90f, 0f, 0f), Vector3.forward, radius: 150f, height: 400f);

        Assert.AreEqual(0f, atEdge, 0.001f);
        Assert.AreEqual(240f, inside, 0.001f);
    }
}
