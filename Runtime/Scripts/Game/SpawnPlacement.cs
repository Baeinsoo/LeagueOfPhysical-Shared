using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LOP
{
    /// <summary>맵에 찍힌 시작 지점 마커를 배정 순서대로 세운다.</summary>
    public static class SpawnPlacement
    {
        /// <summary>
        /// <see cref="SpawnPoint.Order"/> 오름차순으로 자리를 세운다. Order가 같으면 오브젝트 이름으로
        /// 가른다 — 그러지 않으면 찾아온 순서가 그대로 남아 실행할 때마다 자리가 바뀔 수 있다.
        /// </summary>
        public static List<Vector3> Arrange(IEnumerable<SpawnPoint> points)
        {
            if (points == null)
            {
                return new List<Vector3>();
            }

            return points
                .Where(point => point != null)
                .OrderBy(point => point.Order)
                .ThenBy(point => point.name, System.StringComparer.Ordinal)
                .Select(point => point.transform.position)
                .ToList();
        }
    }
}
