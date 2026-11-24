using System;
using System.Collections.Generic;
using System.Numerics;

namespace LOP.TriGrab
{
    /// <summary>시안(trigrab-ring3d-v9)의 stepCards·freeSpot. 카드가 판 위를 미끄러지고 서로·두더지·벽에 튕긴다.</summary>
    public static class TriGrabCardSim
    {
        const int Iterations = 3;
        // 시안의 "부딪힘 연출" 기준(시안 3 × 0.1).
        const float BumpSpeed = 0.3f;
        const float SpotEdgeMargin = 0.2f;
        const int SpotTries = 600;
        const int SpotStrictTries = 400;
        const float SpotGap = 0.3f;
        const float SpotLooseGap = 0.05f;

        /// <param name="onDisc">판 위에 있는 카드(Gone 아님, 끌려가는 중 아님).</param>
        public static void Step(IList<TriGrabCard> onDisc, IReadOnlyList<TriGrabMole> moles, long tick, float dt, Random rng)
        {
            float sp = TriGrabConfig.CardSpeed;
            for (int i = 0; i < onDisc.Count; i++)
            {
                var c = onDisc[i];
                c.Pos += c.Vel * dt;
                float v = c.Vel.Length();
                if (v < 1e-3f)
                {
                    // 거의 멈추면 같은 씨앗 난수로 다시 걷어찬다.
                    float a = (float)(rng.NextDouble() * Math.PI * 2);
                    c.Vel = new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * sp;
                }
                else
                {
                    float k = 1f + (sp / v - 1f) * Math.Min(1f, dt * 1.5f);
                    c.Vel *= k;
                }
            }

            float need = TriGrabConfig.CardBumpRadius * 2f;
            float moleNeed = TriGrabConfig.CardBumpRadius + TriGrabConfig.MoleRadius;
            float wall = TriGrabConfig.CardWallRadius;
            for (int it = 0; it < Iterations; it++)
            {
                for (int i = 0; i < onDisc.Count; i++)
                    for (int j = i + 1; j < onDisc.Count; j++)
                    {
                        var a = onDisc[i];
                        var b = onDisc[j];
                        var d2 = b.Pos - a.Pos;
                        float d = d2.Length();
                        if (d < 1e-3f) d = 1e-3f;
                        if (d >= need) continue;
                        var n = d2 / d;
                        float o = (need - d) / 2f;
                        a.Pos -= n * o;
                        b.Pos += n * o;
                        float rel = Vector2.Dot(b.Vel - a.Vel, n);
                        if (rel < 0f)
                        {
                            a.Vel += n * rel;
                            b.Vel -= n * rel;
                            if (-rel > BumpSpeed) { a.BumpTick = tick; b.BumpTick = tick; }
                        }
                    }

                for (int i = 0; i < onDisc.Count; i++)
                {
                    var c = onDisc[i];
                    for (int m = 0; m < moles.Count; m++)
                    {
                        var mole = moles[m];
                        // 맞아서 아직 솟아 있는 두더지도 카드를 튕긴다(시안과 같음).
                        if (mole.HeightAt(tick) < 0.5f) continue;
                        var dv = c.Pos - mole.Pos;
                        float d = dv.Length();
                        if (d < 1e-3f) d = 1e-3f;
                        if (d >= moleNeed) continue;
                        var n = dv / d;
                        c.Pos = mole.Pos + n * moleNeed;
                        float vn = Vector2.Dot(c.Vel, n);
                        if (vn < 0f) c.Vel -= 2f * vn * n;
                    }
                }

                for (int i = 0; i < onDisc.Count; i++)
                {
                    var c = onDisc[i];
                    float r = c.Pos.Length();
                    if (r <= wall) continue;
                    var n = c.Pos / r;
                    c.Pos = n * wall;
                    float vn = Vector2.Dot(c.Vel, n);
                    if (vn > 0f)
                    {
                        c.Vel -= 2f * vn * n;
                        if (vn > BumpSpeed) c.BumpTick = tick;
                    }
                }
            }
        }

        /// <summary>다른 카드와 겹치지 않는 자리를 찾는다. 앞쪽 시도는 넉넉한 간격, 모자라면 좁힌다.</summary>
        public static bool TryFreeSpot(IList<TriGrabCard> onDisc, Random rng, out Vector2 spot)
        {
            float baseGap = TriGrabConfig.CardBumpRadius * 2f;
            for (int k = 0; k < SpotTries; k++)
            {
                float a = (float)(rng.NextDouble() * Math.PI * 2);
                float r = (float)Math.Sqrt(rng.NextDouble()) * (TriGrabConfig.CardWallRadius - SpotEdgeMargin);
                var p = new Vector2((float)Math.Cos(a) * r, (float)Math.Sin(a) * r);
                float gap = baseGap + (k < SpotStrictTries ? SpotGap : SpotLooseGap);
                bool ok = true;
                for (int i = 0; i < onDisc.Count && ok; i++)
                    if (Vector2.Distance(onDisc[i].Pos, p) < gap) ok = false;
                if (ok) { spot = p; return true; }
            }
            spot = default;
            return false;
        }
    }
}
