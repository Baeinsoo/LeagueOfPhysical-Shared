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

        // tick은 칸이 무너지는 기능(다음 슬라이스)을 위해 미리 받아 둔다.
        public bool IsSolid(float x, float z, long tick)
        {
            var (tx, tz) = TileAt(x, z, TileSize);
            return HasTile(tx, tz);
        }

        public Vector3 TileCenter(int x, int z)
            => new Vector3((x + 0.5f) * TileSize, 0f, (z + 0.5f) * TileSize);
    }
}
