using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 세이브 발판 모음. 맵 씬의 <see cref="SavePad"/>가 맵이 뜰 때 자기를 넣는다(<see cref="CheckpointField"/>와 같은 통로).
    /// 비어 있으면 그 맵은 발판이 없는 맵 — 부활은 옛 자동 체크포인트 규칙을 쓴다.
    /// </summary>
    public class SavePadField
    {
        //  발에서 발판 윗면까지 이만큼 안이면 "발판 위에 서 있다". 접지 판정의 피부 두께보다 넉넉하게.
        private const float StandTolerance = 0.6f;

        private readonly Dictionary<int, (Bounds top, string label)> pads = new Dictionary<int, (Bounds, string)>();
        //  id 순으로 돈다 — 겹친 발판이 있어도 클·서가 같은 답을 내게(사전 순회 순서에 기대지 않는다).
        private readonly List<int> ids = new List<int>();

        public int Count => pads.Count;

        public void Add(int id, Bounds top, string label)
        {
            if (pads.ContainsKey(id) == false)
            {
                ids.Add(id);
                ids.Sort();
            }
            pads[id] = (top, label);
        }

        public bool Remove(int id)
        {
            ids.Remove(id);
            return pads.Remove(id);
        }

        /// <summary>발(몸 바닥 가운데)이 어느 발판 윗면 위에 있나.</summary>
        public bool TryFind(Vector3 feet, out int id)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                Bounds b = pads[ids[i]].top;
                if (feet.x >= b.min.x && feet.x <= b.max.x && feet.z >= b.min.z && feet.z <= b.max.z &&
                    Mathf.Abs(feet.y - b.max.y) <= StandTolerance)
                {
                    id = ids[i];
                    return true;
                }
            }
            id = SkydiveSave.None;
            return false;
        }

        /// <summary>부활 자리 = 발판 윗면 가운데.</summary>
        public bool TryGetRespawn(int id, out Vector3 point)
        {
            if (pads.TryGetValue(id, out var pad))
            {
                point = new Vector3(pad.top.center.x, pad.top.max.y, pad.top.center.z);
                return true;
            }
            point = default;
            return false;
        }

        public bool TryGetLabel(int id, out string label)
        {
            if (pads.TryGetValue(id, out var pad))
            {
                label = pad.label;
                return true;
            }
            label = null;
            return false;
        }
    }
}
