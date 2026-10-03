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

    [Test]
    public void 예고를_짧게_잡은_폭탄은_그만큼_일찍_켜진다()
    {
        var quick = new DodgePattern(1, DodgePatternKind.Bomb, 0, 0, 0f, 0f, 2f, 0f, warnTicks: 20);
        Assert.IsFalse(ShapesAt(quick, 19)[0].Active);
        Assert.IsTrue(ShapesAt(quick, 20)[0].Active);
        Assert.IsTrue(DodgeHazards.Hits(quick, 20, Vector2.zero, Vector2.zero, C));
    }

    [Test]
    public void 예고가_0이면_설정_기본값을_쓴다()
    {
        var old = new DodgePattern(1, DodgePatternKind.Laser, 0, 0, -9f, 0f, 9f, 0f);
        Assert.AreEqual(C.WarnTicks, DodgeHazards.Warn(old, C));
        Assert.IsFalse(ShapesAt(old, C.WarnTicks - 1)[0].Active);
        Assert.IsTrue(ShapesAt(old, C.WarnTicks)[0].Active);
    }

    [Test]
    public void 수명도_패턴의_예고를_따른다()
    {
        var quick = new DodgePattern(1, DodgePatternKind.Tiles, 0, 1UL, 0f, 0f, 0f, 0f, warnTicks: 20);
        Assert.AreEqual(20 + C.TileOnTicks - 1, DodgeHazards.LifetimeTicks(quick, C));
    }

    [Test]
    public void 도형은_자기_패턴_종류를_안다()
    {
        var all = new[]
        {
            new DodgePattern(1, DodgePatternKind.BulletRain, 0, 1UL, 0f, 8f, 4f, 0.15f),
            new DodgePattern(2, DodgePatternKind.BulletWall, 0, 0, 0f, 0f, 3f, 0.9f),
            new DodgePattern(3, DodgePatternKind.BulletAimed, 0, 0, -10f, 0f, 0f, 0f),
            new DodgePattern(4, DodgePatternKind.Bomb, 0, 0, 0f, 0f, 2f, 0f),
            new DodgePattern(5, DodgePatternKind.Laser, 0, 0, -9f, 0f, 9f, 0f),
            new DodgePattern(6, DodgePatternKind.Rock, 0, 0, 3f, 0f, 0f, 0f),
            new DodgePattern(7, DodgePatternKind.Tiles, 0, 1UL, 0f, 0f, 0f, 0f),
        };
        foreach (var p in all)
        {
            foreach (long t in new long[] { 5, C.WarnTicks + 5 })
            {
                foreach (var s in ShapesAt(p, t)) Assert.AreEqual(p.Kind, s.Kind, $"{p.Kind} @ {t}");
            }
        }
    }

    // 장독 그림이 예고 자리에서 켜지는 순간 뒤로 튀지 않게 — 굴러 나올 첫 자리와 방향을 한 식에서 꺼낸다(검토 Important 2).
    [Test]
    public void 바위의_출발점과_방향은_첫_굴림과_같다()
    {
        var rock = new DodgePattern(1, DodgePatternKind.Rock, 0, 0, 1f, 2.5f, 0.3f, 0f);
        var first = ShapesAt(rock, C.WarnTicks)[0];
        var next = ShapesAt(rock, C.WarnTicks + 1)[0];
        Vector2 start = DodgeHazards.RockStart(rock, C);
        Assert.AreEqual(first.X0, start.x, 1e-4f);
        Assert.AreEqual(first.Z0, start.y, 1e-4f);
        Vector2 dir = DodgeHazards.RockDirection(rock);
        Vector2 moved = new Vector2(next.X0 - first.X0, next.Z0 - first.Z0).normalized;
        Assert.AreEqual(dir.x, moved.x, 1e-4f);
        Assert.AreEqual(dir.y, moved.y, 1e-4f);
    }

    // 검사기가 같은 식을 쓰도록 도형 하나 판정을 밖으로 뺐다 — Hits와 결과가 같아야 한다.
    [Test]
    public void 도형_하나_판정은_Hits와_같다()
    {
        var c = C;
        var p = new DodgePattern(1, DodgePatternKind.Bomb, 0, 0, 0f, 0f, 2f, 0f);
        long tick = c.WarnTicks + 1;
        var shapes = new System.Collections.Generic.List<DodgeShape>();
        DodgeHazards.Shapes(p, tick, c, shapes);
        foreach (var at in new[] { Vector2.zero, new Vector2(1.9f, 0f), new Vector2(2.5f, 0f) })
        {
            bool any = false;
            foreach (var s in shapes) any |= DodgeHazards.ShapeHits(s, at, at, c);
            Assert.AreEqual(DodgeHazards.Hits(p, tick, at, at, c), any, at.ToString());
        }
    }

    // ── 탄막(가운데 투척기) ──

    static float Angle(DodgeShape s, Vector2 o) => Mathf.Atan2(s.Z0 - o.y, s.X0 - o.x);

    // 링: 가운데에서 N개가 같은 각도로 동시에 퍼진다. 겹(wave)이 시간차로 이어지고 겹마다 반 칸 엇갈린다.
    [Test]
    public void 링은_한_겹에_N개가_같은_간격으로_퍼진다()
    {
        var ring = new DodgePattern(1, DodgePatternKind.Ring, 0, 0, 0f, 0f, 24f, 0f);
        var first = ShapesAt(ring, 10);
        Assert.AreEqual(24, first.Count);
        float r = new Vector2(first[0].X0, first[0].Z0).magnitude;
        Assert.AreEqual(C.BulletSpeed / DodgeConfig.TicksPerSecond * 10, r, 1e-3f);
        float gap = Vector2.Distance(new Vector2(first[0].X0, first[0].Z0), new Vector2(first[1].X0, first[1].Z0));
        Assert.AreEqual(2f * r * Mathf.Sin(Mathf.PI / 24f), gap, 1e-3f);   // 멀수록 틈이 넓다
    }

    [Test]
    public void 링_다음_겹은_반_칸_엇갈린다()
    {
        var ring = new DodgePattern(1, DodgePatternKind.Ring, 0, 0, 0f, 0f, 24f, 0f);
        long t = DodgeHazards.RingWaveGapTicks + 5;
        var all = ShapesAt(ring, t);
        Assert.AreEqual(48, all.Count);
        float a0 = Angle(all[0], Vector2.zero), b0 = Angle(all[24], Vector2.zero);
        Assert.AreEqual(Mathf.PI / 24f, Mathf.DeltaAngle(a0 * Mathf.Rad2Deg, b0 * Mathf.Rad2Deg) * Mathf.Deg2Rad, 1e-3f);
    }

    // 나선 한 갈래의 이웃 탄은 몸이 빠질 만큼 떨어져 있다 — 촘촘하면 갈래가 벽이 되고, 그 벽이 사람보다 빨리 돌아 못 피한다.
    [Test]
    public void 나선_갈래는_사이로_빠질_수_있다()
    {
        float radial = C.BulletSpeed / DodgeConfig.TicksPerSecond * DodgeHazards.SpiralEmitTicks;
        Assert.Greater(radial, 2f * (C.BulletRadius + C.HitRadius));
    }

    // 나선: 갈래마다 일정 틱마다 한 발씩, 쏘는 각이 돌아간다. 쏜 탄은 곧게 날아간다.
    [Test]
    public void 나선은_쏠_때마다_각이_돌아간다()
    {
        float w = 0.05f;
        var spiral = new DodgePattern(1, DodgePatternKind.Spiral, 0, 0, 0f, 0f, 2f, w);
        var shapes = ShapesAt(spiral, DodgeHazards.SpiralEmitTicks * 2 + 1);   // 0·E·2E에 쐈다
        Assert.AreEqual(2 * 3, shapes.Count);
        float first = Angle(shapes[0], Vector2.zero), second = Angle(shapes[2], Vector2.zero);
        Assert.AreEqual(w * DodgeHazards.SpiralEmitTicks, Mathf.DeltaAngle(first * Mathf.Rad2Deg, second * Mathf.Rad2Deg) * Mathf.Deg2Rad, 1e-3f);
    }

    [Test]
    public void 탄막은_다_날아간_뒤에_끝난다()
    {
        var ring = new DodgePattern(1, DodgePatternKind.Ring, 0, 0, 0f, 0f, 24f, 0f);
        var spiral = new DodgePattern(2, DodgePatternKind.Spiral, 0, 0, 0f, 0f, 2f, 0.05f);
        Assert.IsFalse(DodgeHazards.IsOver(ring, DodgeHazards.LifetimeTicks(ring, C), C));
        Assert.AreEqual(0, ShapesAt(ring, DodgeHazards.LifetimeTicks(ring, C)).FindAll(s => Mathf.Abs(s.X0) < C.ArenaHalf && Mathf.Abs(s.Z0) < C.ArenaHalf).Count);
        Assert.Greater(DodgeHazards.LifetimeTicks(spiral, C), DodgeHazards.SpiralEmitTicks * (DodgeHazards.SpiralShots - 1));
    }
}
