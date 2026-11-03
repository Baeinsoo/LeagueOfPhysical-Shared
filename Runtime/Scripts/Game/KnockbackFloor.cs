using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 넉백 생존전의 바닥. 콜라이더 없이 "그 점 아래에 칸이 있나"를 계산으로 답한다.
    /// 클라와 서버가 같은 식을 쓰므로 같은 답이 나온다.
    /// </summary>
    public sealed class KnockbackFloor
    {
        // 칸 좌표 -> 칸 번호(tiles 안의 순서). 칸 번호는 클·서가 같은 식으로 만들어 같다.
        readonly Dictionary<(int x, int z), int> indexOf = new Dictionary<(int x, int z), int>();
        readonly List<(int x, int z)> tiles = new List<(int x, int z)>();

        public float TileSize { get; }
        public IReadOnlyList<(int x, int z)> Tiles => tiles;

        /// <summary>true면 위험 지대 방식: 원이 줄어도 칸은 남고, 원 밖 칸이 위험 지대가 된다.</summary>
        public bool HazardEdge { get; }

        readonly float arenaRadius;
        readonly long suddenDeathTicks;
        readonly long crackWarningTicks;

        /// <summary>무너지는 틱이 없는 칸의 값.</summary>
        public const long NoCollapse = long.MaxValue;

        // 서버가 알린 "이 칸이 무너지는 틱". 칸 번호로 찾는다. 서버 사실이라 되감기 저장 대상이 아니다(틱의 함수라 되감아도 그 틱 그대로).
        long[] collapseTicks;

        /// <param name="suddenDeathTicks">위험 지대 방식에서 마지막 원이 다 줄어든 뒤, 바깥부터 전부 무너지기까지 걸리는 틱. 0이면 서든데스 없음.</param>
        /// <param name="crackWarningTicks">금이 간 칸이 무너지기 전에 깜박이는 틱. 0이면 이 규칙으론 깜박이지 않는다.</param>
        public KnockbackFloor(float arenaRadius, float tileSize, float holeRadius, bool hazardEdge = false, long suddenDeathTicks = 0, long crackWarningTicks = 0)
        {
            this.crackWarningTicks = crackWarningTicks;
            HazardEdge = hazardEdge;
            this.arenaRadius = arenaRadius;
            this.suddenDeathTicks = suddenDeathTicks;
            TileSize = tileSize;
            ZoneStartRadius = arenaRadius + tileSize * Mathf.Sqrt(2f) * 0.5f;
            int range = Mathf.CeilToInt(arenaRadius / tileSize);
            // z 먼저, 그 안에서 x 순으로 훑어 순서를 고정한다.
            for (int z = -range; z <= range; z++)
            {
                for (int x = -range; x <= range; x++)
                {
                    float cx = (x + 0.5f) * tileSize;
                    float cz = (z + 0.5f) * tileSize;
                    float d = Mathf.Sqrt(cx * cx + cz * cz);
                    if (d <= arenaRadius && d >= holeRadius)
                    {
                        indexOf.Add((x, z), tiles.Count);
                        tiles.Add((x, z));
                    }
                }
            }
            collapseTicks = new long[tiles.Count];
            ClearCollapses();
        }

        public bool HasTile(int x, int z) => indexOf.ContainsKey((x, z));

        /// <summary>칸 좌표의 칸 번호. 원판에 없으면 -1.</summary>
        public int TileIndex(int x, int z) => indexOf.TryGetValue((x, z), out int i) ? i : -1;

        /// <summary>그 점 아래 칸의 번호. 원판 밖(구멍 포함)이면 -1.</summary>
        public int TileIndexAt(float x, float z)
        {
            var (tx, tz) = TileAt(x, z, TileSize);
            return TileIndex(tx, tz);
        }

        /// <summary>
        /// 칸이 tick부터 무너진다는 서버 사실을 넣는다. 이미 더 이른 틱이 있으면 그대로 둔다(더 이른 무너짐이 이긴다).
        /// </summary>
        public void SetCollapseTick(int tileIndex, long tick)
        {
            if (tileIndex < 0 || tileIndex >= collapseTicks.Length)
                throw new System.ArgumentOutOfRangeException(nameof(tileIndex), tileIndex, "칸 번호가 원판 밖이다");
            if (tick < collapseTicks[tileIndex]) collapseTicks[tileIndex] = tick;
        }

        /// <summary>그 칸이 무너지는 틱. 없으면 NoCollapse.</summary>
        public long CollapseTickOf(int tileIndex)
            => tileIndex >= 0 && tileIndex < collapseTicks.Length ? collapseTicks[tileIndex] : NoCollapse;

        /// <summary>서버가 알린 무너짐을 모두 지운다(새 상태를 처음부터 다시 넣을 때).</summary>
        public void ClearCollapses()
        {
            for (int i = 0; i < collapseTicks.Length; i++) collapseTicks[i] = NoCollapse;
        }

        public static (int x, int z) TileAt(float x, float z, float tileSize)
            => (Mathf.FloorToInt(x / tileSize), Mathf.FloorToInt(z / tileSize));

        // 서버가 공개한 원 단계. 호출자가 나중에 목록을 바꿔도 바닥이 몰래 바뀌지 않게 복사해 둔다.
        ZoneStage[] stages = System.Array.Empty<ZoneStage>();

        /// <summary>
        /// 첫 단계 전의 원 반지름. 원판에 든 모든 칸의 가장 먼 꼭짓점이 이 안에 들어
        /// 단계가 없을 땐 원판 그대로다.
        /// </summary>
        public float ZoneStartRadius { get; }

        public void SetZoneStages(IReadOnlyList<ZoneStage> newStages)
        {
            var copy = new ZoneStage[newStages.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = newStages[i];
            stages = copy;
        }

        // 칸이 있고, 칸의 가장 먼 꼭짓점까지 그 틱의 원 안이어야 바닥이다(칸이 한 조각이라도 원 밖이면 무너진다).
        public bool IsSolid(float x, float z, long tick)
        {
            var (tx, tz) = TileAt(x, z, TileSize);
            return IsTileSolid(tx, tz, tick);
        }

        /// <summary>칸 번호(미터 아님)로 묻는다. 월드 좌표는 IsSolid(float, float, tick)를 쓴다.</summary>
        public bool IsTileSolid(int tx, int tz, long tick)
        {
            if (!indexOf.TryGetValue((tx, tz), out int index)) return false;
            if (tick >= collapseTicks[index]) return false;
            if (HazardEdge) return !SuddenDeathCollapsed(tx, tz, tick);
            return TileInsideCircle(tx, tz, tick);
        }

        // 위험 지대 방식의 판 끝내기: 마지막 원이 다 줄어든 틱(E) 뒤로 원판 중심 기준 반지름이 D틱에 걸쳐 0까지 줄고, 그 밖으로 걸친 칸은 무너진다.
        bool SuddenDeathCollapsed(int tx, int tz, long tick)
        {
            if (suddenDeathTicks <= 0 || stages.Length == 0) return false;
            int last = 0;
            for (int i = 1; i < stages.Length; i++) { if (stages[i].index > stages[last].index) last = i; }
            long end = stages[last].shrinkEndTick;
            if (tick <= end) return false;
            float radius = arenaRadius * (1f - (float)(tick - end) / suddenDeathTicks);
            if (radius <= 0f) return true;
            float fx = Mathf.Max(Mathf.Abs(tx * TileSize), Mathf.Abs((tx + 1) * TileSize));
            float fz = Mathf.Max(Mathf.Abs(tz * TileSize), Mathf.Abs((tz + 1) * TileSize));
            return fx * fx + fz * fz > radius * radius;
        }

        // 칸의 가장 먼 꼭짓점까지 그 틱의 원 안이면 true. 반지름 0이면 어떤 칸도 안에 없다.
        bool TileInsideCircle(int tx, int tz, long tick)
        {
            var circle = MountedArcheryZone.CircleAt(stages, tick, Vector2.zero, ZoneStartRadius);
            if (circle.radius <= 0f) return false;
            float fx = Mathf.Max(Mathf.Abs(tx * TileSize - circle.center.x), Mathf.Abs((tx + 1) * TileSize - circle.center.x));
            float fz = Mathf.Max(Mathf.Abs(tz * TileSize - circle.center.y), Mathf.Abs((tz + 1) * TileSize - circle.center.y));
            return fx * fx + fz * fz <= circle.radius * circle.radius;
        }

        /// <summary>
        /// 위험 지대 방식에서, 그 점 아래 칸이 있고 칸의 한 조각이라도 그 틱의 원 밖이면 true.
        /// 낭떠러지 방식은 늘 false.
        /// </summary>
        public bool InHazard(float x, float z, long tick)
        {
            var (tx, tz) = TileAt(x, z, TileSize);
            return IsTileHazard(tx, tz, tick);
        }

        /// <summary>칸 번호로 묻는 InHazard.</summary>
        public bool IsTileHazard(int tx, int tz, long tick)
            => HazardEdge && IsTileSolid(tx, tz, tick) && !TileInsideCircle(tx, tz, tick);

        /// <summary>x, z는 칸 번호. 지금은 바닥인데 warningTicks 뒤엔 무너져 있는 칸(위험 지대 방식은 위험 지대가 되거나 서든데스로 무너지는 칸) — 깜박이는 그림용.</summary>
        public bool IsWarning(int x, int z, long tick, long warningTicks)
            => IsCrackWarning(x, z, tick) || IsRuleWarning(x, z, tick, warningTicks);

        // 금이 간 칸이 무너지기 전 crackWarningTicks 동안.
        bool IsCrackWarning(int x, int z, long tick)
        {
            if (crackWarningTicks <= 0 || !indexOf.TryGetValue((x, z), out int index)) return false;
            long at = collapseTicks[index];
            return at != NoCollapse && tick < at && tick >= at - crackWarningTicks && IsTileSolid(x, z, tick);
        }

        bool IsRuleWarning(int x, int z, long tick, long warningTicks)
            => HazardEdge
                ? IsTileSolid(x, z, tick)
                    && ((!IsTileHazard(x, z, tick) && IsTileHazard(x, z, tick + warningTicks)) || !IsTileSolid(x, z, tick + warningTicks))
                : IsTileSolid(x, z, tick) && !IsTileSolid(x, z, tick + warningTicks);

        public Vector3 TileCenter(int x, int z)
            => new Vector3((x + 0.5f) * TileSize, 0f, (z + 0.5f) * TileSize);
    }
}
