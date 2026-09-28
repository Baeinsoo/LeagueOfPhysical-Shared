using System.Collections.Generic;
using LOP;
using NUnit.Framework;
using UnityEngine;

public class DodgeHazardsTests
{
    //  마스터데이터 기본값과 같다(TbDodgeConfig).
    static readonly DodgeConfig C = new DodgeConfig(3, 1.5f, 0.16f, 0.5f, 9f, 6, 2f, 1.8f, 0,
                                                   1.2f, 5f, 0.22f, 2f, 0.25f, 0.7f, 0.45f, 6f, 1.35f, 0.55f);

    static List<DodgeShape> ShapesAt(DodgePattern p, long tick)
    {
        var list = new List<DodgeShape>();
        DodgeHazards.Shapes(p, tick, C, list);
        return list;
    }

    [Test]
    public void 시작_전에는_아무_도형도_없다()
    {
        var bomb = new DodgePattern(1, DodgePatternKind.Bomb, 100, 0, 0f, 0f, 2f, 0f);
        Assert.AreEqual(0, ShapesAt(bomb, 99).Count);
        Assert.AreEqual(1, ShapesAt(bomb, 100).Count);
    }

    [Test]
    public void 같은_값이면_클서_어디서든_같은_도형이다()
    {
        var rain = new DodgePattern(1, DodgePatternKind.BulletRain, 0, 12345UL, 0f, 8f, 4f, 0.15f);
        var a = ShapesAt(rain, 40);
        var b = ShapesAt(rain, 40);
        Assert.Greater(a.Count, 0);
        Assert.AreEqual(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++)
        {
            Assert.AreEqual(a[i].X0, b[i].X0);
            Assert.AreEqual(a[i].Z0, b[i].Z0);
        }
    }

    [Test]
    public void 예고_중에는_맞지_않는다()
    {
        var bomb = new DodgePattern(1, DodgePatternKind.Bomb, 0, 0, 0f, 0f, 2f, 0f);
        var here = new Vector2(0f, 0f);
        Assert.IsFalse(DodgeHazards.Hits(bomb, C.WarnTicks - 1, here, here, C));
        var s = ShapesAt(bomb, C.WarnTicks - 1)[0];
        Assert.IsFalse(s.Active);
        Assert.Greater(s.Progress, 0.9f);
    }

    [Test]
    public void 터지는_동안_안에_있으면_맞고_밖이면_안_맞는다()
    {
        var bomb = new DodgePattern(1, DodgePatternKind.Bomb, 0, 0, 0f, 0f, 2f, 0f);
        long t = C.WarnTicks;
        Assert.IsTrue(DodgeHazards.Hits(bomb, t, new Vector2(1.5f, 0f), new Vector2(1.5f, 0f), C));
        Assert.IsFalse(DodgeHazards.Hits(bomb, t, new Vector2(2.5f, 0f), new Vector2(2.5f, 0f), C));
        Assert.IsFalse(DodgeHazards.Hits(bomb, t + C.BombActiveTicks, Vector2.zero, Vector2.zero, C));
    }

    [Test]
    public void 탄은_변에서_출발해_안쪽으로_날아온다()
    {
        //  북쪽 벽(side 0)에서 남쪽으로. 틈이 없는 벽 탄막으로 본다.
        var wall = new DodgePattern(1, DodgePatternKind.BulletWall, 0, 0, 0f, 100f, 0f, 1f);
        var at0 = ShapesAt(wall, 0);
        var at50 = ShapesAt(wall, 50);
        Assert.Greater(at0.Count, 10);
        Assert.AreEqual(C.EdgeDistance, at0[0].Z0, 1e-4f);
        Assert.AreEqual(C.EdgeDistance - C.BulletSpeed, at50[0].Z0, 1e-3f);   // 50틱 = 1초
    }

    [Test]
    public void 벽_탄막의_틈에는_탄이_없다()
    {
        var wall = new DodgePattern(1, DodgePatternKind.BulletWall, 0, 0, 0f, 0f, 3f, 1f);
        foreach (var s in ShapesAt(wall, 0))
        {
            Assert.IsFalse(Mathf.Abs(s.X0) < 1.5f, $"틈 안에 탄이 있다: x={s.X0}");
        }
    }

    [Test]
    public void 마주_달려와_한_틱에_엇갈려도_맞는다()
    {
        //  탄은 남쪽으로 한 틱에 약 0.13m, 몸은 같은 틱에 북쪽으로 크게 움직여 탄을 지나친다.
        //  틱 시작·끝 위치끼리는 둘 다 판정 거리보다 멀다 — 중간에 엇갈린 것만 잡혀야 한다.
        var aimed = new DodgePattern(1, DodgePatternKind.BulletAimed, 0, 0, 0f, 9.5f, 0f, -9.5f);
        long tick = 0;
        DodgeShape center = default;
        for (; tick < 200; tick++)
        {
            var list = ShapesAt(aimed, tick);
            if (list.Count == 0 || list[0].Z0 >= 1f) continue;
            // 부채꼴 세 발 중 가운데(목표로 곧게 오는) 탄을 고른다.
            center = list[0];
            foreach (var s in list) if (Mathf.Abs(s.X0) < Mathf.Abs(center.X0)) center = s;
            break;
        }
        var from = new Vector2(center.X0, center.Z0 - 0.6f);
        var to = new Vector2(center.X0, center.Z1 + 0.6f);
        float reach = C.HitRadius + C.BulletRadius;
        Assert.Greater(Vector2.Distance(from, new Vector2(center.X1, center.Z1)), reach);
        Assert.Greater(Vector2.Distance(to, new Vector2(center.X0, center.Z0)), reach);
        Assert.IsTrue(DodgeHazards.Hits(aimed, tick, from, to, C));
    }

    [Test]
    public void 레이저는_켜진_동안_선_위에서만_맞는다()
    {
        var laser = new DodgePattern(1, DodgePatternKind.Laser, 0, 0, -9f, 0f, 9f, 0f);
        long on = C.WarnTicks;
        Assert.IsFalse(DodgeHazards.Hits(laser, on - 1, Vector2.zero, Vector2.zero, C));
        Assert.IsTrue(DodgeHazards.Hits(laser, on, new Vector2(3f, 0.3f), new Vector2(3f, 0.3f), C));
        Assert.IsFalse(DodgeHazards.Hits(laser, on, new Vector2(3f, 1.5f), new Vector2(3f, 1.5f), C));
    }

    [Test]
    public void 바닥은_켜진_칸_안에서만_맞는다()
    {
        //  칸 0 = 왼쪽 아래(-9..-6, -9..-6)만 켠다.
        var tiles = new DodgePattern(1, DodgePatternKind.Tiles, 0, 1UL, 0f, 0f, 0f, 0f);
        long on = C.WarnTicks;
        Assert.IsTrue(DodgeHazards.Hits(tiles, on, new Vector2(-7.5f, -7.5f), new Vector2(-7.5f, -7.5f), C));
        Assert.IsFalse(DodgeHazards.Hits(tiles, on, new Vector2(-4.5f, -7.5f), new Vector2(-4.5f, -7.5f), C));
        Assert.AreEqual(1, ShapesAt(tiles, on).Count);
    }

    [Test]
    public void 바위는_예고_뒤에_변에서_굴러_들어온다()
    {
        var rock = new DodgePattern(1, DodgePatternKind.Rock, 0, 0, 3f, 0f, 0f, 0f);   // 서쪽 → 동쪽
        var warn = ShapesAt(rock, 0);
        Assert.AreEqual(1, warn.Count);
        Assert.IsFalse(warn[0].Active);
        var rolling = ShapesAt(rock, C.WarnTicks + 50);
        Assert.IsTrue(rolling[0].Active);
        Assert.Greater(rolling[0].X0, -C.EdgeDistance);
    }

    [Test]
    public void 수명이_지나면_끝났다고_한다()
    {
        var bomb = new DodgePattern(1, DodgePatternKind.Bomb, 10, 0, 0f, 0f, 2f, 0f);
        long life = DodgeHazards.LifetimeTicks(bomb, C);
        Assert.IsFalse(DodgeHazards.IsOver(bomb, 10 + life, C));
        Assert.IsTrue(DodgeHazards.IsOver(bomb, 10 + life + 1, C));
        Assert.AreEqual(0, ShapesAt(bomb, 10 + life + 1).Count);
    }
}
