using System.Collections.Generic;
using GameFramework.Rng;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 위험 도형 식. (패턴, 시작 틱, 틱) → 그 틱의 도형. 상태가 없어 클·서가 각자 계산해도 같은 답이 나온다.
    ///
    /// 패턴 값(P0..P3) 표:
    /// BulletRain  Seed=탄마다 난수 | 변 | 탄 수 | 탄 사이 틱 | 흩어짐(rad)
    /// BulletWall  | 변 | 틈 중심 | 틈 폭 | 탄 간격(m)
    /// BulletAimed | 출발 x | 출발 z | 목표 x | 목표 z   (3발씩 3번)
    /// Bomb        | x | z | 반지름
    /// Laser       | x0 | z0 | x1 | z1
    /// Rock        | 변 | 변 따라 위치 | 각 비틀기(rad)
    /// Tiles       Seed=켜질 칸 비트마스크(칸 i = (i % N, i / N))
    /// 변: 0=북(+z에서 남쪽으로) 1=동 2=남 3=서.
    /// </summary>
    public static class DodgeHazards
    {
        private const int AimedWaves = 3;
        private const int AimedWaveGapTicks = 17;
        private const int AimedFan = 3;
        private const float AimedFanStep = 0.13f;
        private const float AimedSpeedScale = 1.3f;

        [System.ThreadStatic] private static List<DodgeShape> scratch;

        public static Vector2 EdgePoint(int side, float along, float edge)
        {
            switch (side & 3)
            {
                case 0: return new Vector2(along, edge);
                case 1: return new Vector2(edge, along);
                case 2: return new Vector2(along, -edge);
                default: return new Vector2(-edge, along);
            }
        }

        public static Vector2 Inward(int side)
        {
            switch (side & 3)
            {
                case 0: return new Vector2(0f, -1f);
                case 1: return new Vector2(-1f, 0f);
                case 2: return new Vector2(0f, 1f);
                default: return new Vector2(1f, 0f);
            }
        }

        private static Vector2 Rotate(Vector2 v, float radians)
        {
            float c = Mathf.Cos(radians), s = Mathf.Sin(radians);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        // 경기장을 가로질러 반대편 밖까지 가는 데 걸리는 틱. 대각선도 넉넉히 덮게 1.5배.
        private static long TravelTicks(float speed, in DodgeConfig c)
            => (long)Mathf.Ceil((c.EdgeDistance * 2f * 1.5f + 2f) / Mathf.Max(speed, 0.01f) * DodgeConfig.TicksPerSecond);

        /// <summary>이 패턴의 예고 길이. 옛 목록처럼 값이 없으면(0) 설정 기본값 — 0틱 예고는 "켜진 채 나타남"이다.</summary>
        public static int Warn(in DodgePattern p, in DodgeConfig c) => p.WarnTicks > 0 ? p.WarnTicks : c.WarnTicks;

        public static long LifetimeTicks(in DodgePattern p, in DodgeConfig c)
        {
            switch (p.Kind)
            {
                case DodgePatternKind.BulletRain:
                    return (long)Mathf.Max(0f, p.P1 - 1f) * (long)Mathf.Max(0f, p.P2) + TravelTicks(c.BulletSpeed, c);
                case DodgePatternKind.BulletWall:
                    return TravelTicks(c.BulletSpeed, c);
                case DodgePatternKind.BulletAimed:
                    return (AimedWaves - 1) * AimedWaveGapTicks + TravelTicks(c.BulletSpeed * AimedSpeedScale, c);
                case DodgePatternKind.Bomb:
                    return Warn(p, c) + c.BombActiveTicks - 1;
                case DodgePatternKind.Laser:
                    return Warn(p, c) + c.LaserOnTicks - 1;
                case DodgePatternKind.Rock:
                    return Warn(p, c) + TravelTicks(c.RockSpeed, c);
                case DodgePatternKind.Tiles:
                    return Warn(p, c) + c.TileOnTicks - 1;
                default:
                    return 0;
            }
        }

        public static bool IsOver(in DodgePattern p, long tick, in DodgeConfig c) => tick - p.StartTick > LifetimeTicks(p, c);

        public static void Shapes(in DodgePattern p, long tick, in DodgeConfig c, List<DodgeShape> into)
        {
            long age = tick - p.StartTick;
            if (age < 0 || age > LifetimeTicks(p, c))
            {
                return;
            }

            int first = into.Count;
            switch (p.Kind)
            {
                case DodgePatternKind.BulletRain: Rain(p, age, c, into); break;
                case DodgePatternKind.BulletWall: Wall(p, age, c, into); break;
                case DodgePatternKind.BulletAimed: Aimed(p, age, c, into); break;
                case DodgePatternKind.Bomb: Bomb(p, age, c, into); break;
                case DodgePatternKind.Laser: Laser(p, age, c, into); break;
                case DodgePatternKind.Rock: Rock(p, age, c, into); break;
                case DodgePatternKind.Tiles: Tiles(p, age, c, into); break;
            }

            // 그림이 물건을 고르게 종류를 싣는다 — 판정(Hits)은 이 값을 보지 않는다.
            for (int i = first; i < into.Count; i++)
            {
                var s = into[i];
                s.Kind = p.Kind;
                into[i] = s;
            }
        }

        /// <summary>몸이 이번 틱에 from→to로 움직였을 때 켜진 도형에 닿았나. 예고 중인 도형은 판정이 없다.</summary>
        public static bool Hits(in DodgePattern p, long tick, Vector2 from, Vector2 to, in DodgeConfig c)
        {
            var list = scratch ??= new List<DodgeShape>();
            list.Clear();
            Shapes(p, tick, c, list);
            foreach (var s in list)
            {
                if (!s.Active)
                {
                    continue;
                }
                switch (s.Type)
                {
                    case DodgeShapeType.Circle:
                        if (DodgeGeometry.ClosestApproach(from, to, new Vector2(s.X1, s.Z1), new Vector2(s.X0, s.Z0))
                            <= c.HitRadius + s.Radius) return true;
                        break;
                    case DodgeShapeType.Segment:
                        if (DodgeGeometry.SegmentDistance(from, to, new Vector2(s.X0, s.Z0), new Vector2(s.X1, s.Z1))
                            <= c.HitRadius + s.Radius) return true;
                        break;
                    case DodgeShapeType.Rect:
                        // 바닥은 발밑 칸으로 본다 — 판정 반지름을 더하면 칸 경계에 선 사람이 억울하다.
                        if (to.x >= s.X0 && to.x < s.X1 && to.y >= s.Z0 && to.y < s.Z1) return true;
                        break;
                }
            }
            return false;
        }

        // 탄 하나: origin에서 dir로 age 틱 날아간 자리와 한 틱 전 자리. 경기장 밖 멀리 나간 탄은 안 넣는다.
        private static void Bullet(Vector2 origin, Vector2 dir, float speed, long age, in DodgeConfig c, List<DodgeShape> into)
        {
            float step = speed / DodgeConfig.TicksPerSecond;
            Vector2 now = origin + dir * (step * age);
            Vector2 prev = origin + dir * (step * Mathf.Max(0, age - 1));
            float limit = c.EdgeDistance + 1f;
            if (Mathf.Abs(now.x) > limit || Mathf.Abs(now.y) > limit)
            {
                return;
            }
            into.Add(new DodgeShape
            {
                Type = DodgeShapeType.Circle, Active = true, Progress = 1f,
                X0 = now.x, Z0 = now.y, X1 = prev.x, Z1 = prev.y, Radius = c.BulletRadius,
            });
        }

        private static void Rain(in DodgePattern p, long age, in DodgeConfig c, List<DodgeShape> into)
        {
            int side = (int)p.P0, count = (int)p.P1;
            long spacing = (long)Mathf.Max(0f, p.P2);
            float spread = p.P3, h = c.ArenaHalf;
            for (int i = 0; i < count; i++)
            {
                long bulletAge = age - i * spacing;
                if (bulletAge < 0)
                {
                    break;
                }
                // 뽑는 순서(위치 → 각)가 계약이다. 바꾸면 클·서 탄이 어긋난다.
                var rng = new DeterministicRandom(Hashing.Combine(p.Seed, (ulong)i));
                float along = rng.Range(-h, h);
                float angle = spread > 0f ? rng.Range(-spread, spread) : 0f;
                Bullet(EdgePoint(side, along, c.EdgeDistance), Rotate(Inward(side), angle), c.BulletSpeed, bulletAge, c, into);
            }
        }

        private static void Wall(in DodgePattern p, long age, in DodgeConfig c, List<DodgeShape> into)
        {
            int side = (int)p.P0;
            float gapCenter = p.P1, gapHalf = p.P2 * 0.5f, spacing = Mathf.Max(0.3f, p.P3), h = c.ArenaHalf;
            for (float along = -h; along <= h + 1e-4f; along += spacing)
            {
                if (Mathf.Abs(along - gapCenter) < gapHalf)
                {
                    continue;
                }
                Bullet(EdgePoint(side, along, c.EdgeDistance), Inward(side), c.BulletSpeed, age, c, into);
            }
        }

        private static void Aimed(in DodgePattern p, long age, in DodgeConfig c, List<DodgeShape> into)
        {
            var origin = new Vector2(p.P0, p.P1);
            Vector2 toTarget = new Vector2(p.P2, p.P3) - origin;
            Vector2 dir = toTarget.sqrMagnitude > 1e-6f ? toTarget.normalized : Vector2.down;
            for (int wave = 0; wave < AimedWaves; wave++)
            {
                long waveAge = age - wave * AimedWaveGapTicks;
                if (waveAge < 0)
                {
                    break;
                }
                for (int k = 0; k < AimedFan; k++)
                {
                    float angle = (k - (AimedFan - 1) * 0.5f) * AimedFanStep;
                    Bullet(origin, Rotate(dir, angle), c.BulletSpeed * AimedSpeedScale, waveAge, c, into);
                }
            }
        }

        private static void Bomb(in DodgePattern p, long age, in DodgeConfig c, List<DodgeShape> into)
        {
            bool active = age >= Warn(p, c);
            into.Add(new DodgeShape
            {
                Type = DodgeShapeType.Circle, Active = active,
                Progress = active ? 1f : (float)age / Warn(p, c),
                X0 = p.P0, Z0 = p.P1, X1 = p.P0, Z1 = p.P1, Radius = p.P2,
            });
        }

        private static void Laser(in DodgePattern p, long age, in DodgeConfig c, List<DodgeShape> into)
        {
            bool active = age >= Warn(p, c);
            into.Add(new DodgeShape
            {
                Type = DodgeShapeType.Segment, Active = active,
                Progress = active ? 1f : (float)age / Warn(p, c),
                X0 = p.P0, Z0 = p.P1, X1 = p.P2, Z1 = p.P3, Radius = c.LaserWidth * 0.5f,
            });
        }

        private static void Rock(in DodgePattern p, long age, in DodgeConfig c, List<DodgeShape> into)
        {
            int side = (int)p.P0;
            Vector2 entry = EdgePoint(side, p.P1, c.EdgeDistance);
            if (age < Warn(p, c))
            {
                // 예고: 벽 바로 안쪽, 굴러 들어올 자리에 원을 띄운다.
                Vector2 mark = entry + Inward(side) * 1.2f;
                into.Add(new DodgeShape
                {
                    Type = DodgeShapeType.Circle, Active = false, Progress = (float)age / Warn(p, c),
                    X0 = mark.x, Z0 = mark.y, X1 = mark.x, Z1 = mark.y, Radius = c.RockRadius,
                });
                return;
            }
            Vector2 dir = Rotate(Inward(side), p.P2);
            Vector2 origin = entry - dir * c.RockRadius;
            long rollAge = age - Warn(p, c);
            float step = c.RockSpeed / DodgeConfig.TicksPerSecond;
            Vector2 now = origin + dir * (step * rollAge);
            Vector2 prev = origin + dir * (step * Mathf.Max(0, rollAge - 1));
            into.Add(new DodgeShape
            {
                Type = DodgeShapeType.Circle, Active = true, Progress = 1f,
                X0 = now.x, Z0 = now.y, X1 = prev.x, Z1 = prev.y, Radius = c.RockRadius,
            });
        }

        private static void Tiles(in DodgePattern p, long age, in DodgeConfig c, List<DodgeShape> into)
        {
            int n = Mathf.Max(1, c.TileCount);
            float size = c.ArenaHalf * 2f / n;
            bool active = age >= Warn(p, c);
            float progress = active ? 1f : (float)age / Warn(p, c);
            for (int i = 0; i < n * n && i < 64; i++)
            {
                if ((p.Seed & (1UL << i)) == 0)
                {
                    continue;
                }
                float x0 = -c.ArenaHalf + (i % n) * size, z0 = -c.ArenaHalf + (i / n) * size;
                into.Add(new DodgeShape
                {
                    Type = DodgeShapeType.Rect, Active = active, Progress = progress,
                    X0 = x0, Z0 = z0, X1 = x0 + size, Z1 = z0 + size,
                });
            }
        }
    }
}
