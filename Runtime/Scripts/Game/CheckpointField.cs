using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 체크포인트(부활 지점) 모음. 맵 씬의 <see cref="CheckpointMarker"/>가 맵이 뜰 때 자기를 넣는다 —
    /// 맵마다 체크포인트가 다르므로 코드 표 한 벌로는 맵을 두 장 둘 수 없다.
    /// <para>비어 있으면 옛 코드 표(<see cref="SkydiveCourseLayout"/>)를 준다 — 표식이 실리기 전의 더미 맵 원격 에셋도 안 깨진다.
    /// 맨 위 체크포인트가 스폰 고도다.</para>
    /// </summary>
    public class CheckpointField
    {
        private readonly Dictionary<float, Vector3> points = new Dictionary<float, Vector3>();
        private readonly List<float> ys = new List<float>();

        public int Count => points.Count;

        public void Add(float y, Vector3 point)
        {
            if (points.ContainsKey(y) == false)
            {
                ys.Add(y);
                ys.Sort((a, b) => b.CompareTo(a));   // 높은 것부터 — 코스가 위에서 아래로 간다
            }
            points[y] = point;
        }

        /// <summary>넣을 때와 같은 값이어야 뺀다 — 같은 고도에 다른 표식이 있으면 그것까지 지우지 않게.</summary>
        public bool Remove(float y, Vector3 point)
        {
            if (points.TryGetValue(y, out Vector3 current) == false || current != point)
            {
                return false;
            }
            points.Remove(y);
            ys.Remove(y);
            return true;
        }

        public IReadOnlyList<float> ShelfYs => Count == 0 ? SkydiveCourseLayout.ShelfYs : ys;

        public float SpawnY => Count == 0 ? SkydiveCourseLayout.SpawnY : ys[0];

        public IReadOnlyDictionary<float, Vector3> RespawnPoints => Count == 0 ? SkydiveCourseLayout.RespawnPoints : points;
    }
}
