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
        readonly HashSet<(int x, int z)> set = new HashSet<(int x, int z)>();
        readonly List<(int x, int z)> tiles = new List<(int x, int z)>();

        public float TileSize { get; }
        public IReadOnlyList<(int x, int z)> Tiles => tiles;

        public KnockbackFloor(float arenaRadius, float tileSize, float holeRadius)
        {
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
                        set.Add((x, z));
                        tiles.Add((x, z));
                    }
                }
            }
        }

        public bool HasTile(int x, int z) => set.Contains((x, z));

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
            if (!HasTile(tx, tz)) return false;
            var circle = MountedArcheryZone.CircleAt(stages, tick, Vector2.zero, ZoneStartRadius);
            if (circle.radius <= 0f) return false;
            float fx = Mathf.Max(Mathf.Abs(tx * TileSize - circle.center.x), Mathf.Abs((tx + 1) * TileSize - circle.center.x));
            float fz = Mathf.Max(Mathf.Abs(tz * TileSize - circle.center.y), Mathf.Abs((tz + 1) * TileSize - circle.center.y));
            return fx * fx + fz * fz <= circle.radius * circle.radius;
        }

        /// <summary>x, z는 칸 번호. 지금은 바닥인데 warningTicks 뒤엔 무너져 있는 칸 — 깜박이는 그림용.</summary>
        public bool IsWarning(int x, int z, long tick, long warningTicks)
            => IsTileSolid(x, z, tick) && !IsTileSolid(x, z, tick + warningTicks);

        public Vector3 TileCenter(int x, int z)
            => new Vector3((x + 0.5f) * TileSize, 0f, (z + 0.5f) * TileSize);
    }
}
