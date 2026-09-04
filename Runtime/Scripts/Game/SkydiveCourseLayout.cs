using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 코스의 뼈대 좌표. <b>굽는 쪽(클라 에디터)과 판정하는 쪽(서버)이 같은 값을 봐야</b> 해서
    /// 공용 패키지에 있다 — 한쪽에만 두면 표를 고칠 때 조용히 어긋난다.
    /// </summary>
    public static class SkydiveCourseLayout
    {
        public const float SpawnY = 3000f;

        /// <summary>선반 고도. 위에서 아래 순서.</summary>
        public static readonly IReadOnlyList<float> ShelfYs =
            new[] { 2600f, 2200f, 1800f, 1400f, 1000f, 600f, 200f };

        /// <summary>
        /// 선반 고도 → 그 선반 위 부활 지점. 규칙으로 유도하지 않고 적어 두는 이유는 선반마다
        /// 구멍 위치가 달라, 규칙 한 줄로 만들면 표를 고칠 때 구멍 위에 세우게 되기 때문이다.
        /// 전부 구멍 중심에서 40m 떨어져 있고 판(±100) 안이며 기둥(±60)과도 겹치지 않는다.
        /// </summary>
        public static readonly IReadOnlyDictionary<float, Vector3> RespawnPoints =
            new Dictionary<float, Vector3>
            {
                { 2600f, new Vector3(0f, 2600f, 40f) },
                { 2200f, new Vector3(30f, 2200f, 40f) },
                { 1800f, new Vector3(30f, 1800f, -10f) },
                { 1400f, new Vector3(-25f, 1400f, -10f) },
                { 1000f, new Vector3(-25f, 1000f, 10f) },
                { 600f, new Vector3(30f, 600f, 15f) },
                { 200f, new Vector3(0f, 200f, -15f) },
            };
    }
}
