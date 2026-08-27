using System;
using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 판치기의 무대. 판 경계와 동전 자리를 씬이 들고 있게 해 코드에 좌표가 박히지 않게 한다 —
    /// 스폰과 장외 복귀가 같은 값을 본다.
    ///
    /// 이 스크립트가 서버가 아니라 공용 패키지에 있는 이유: 판은 <b>맵 씬</b>에 놓이고, 맵 씬은
    /// 클·서가 같은 어드레서블 번들로 받는다. 서버에만 타입이 있으면 클라가 그 씬을 열 때
    /// 스크립트 없는 깨진 참조가 된다.
    /// </summary>
    public class PanchigiBoard : MonoBehaviour
    {
        [Serializable]
        public class Formation
        {
            public string name;
            public Transform[] slots;
        }

        [SerializeField] private Collider boardCollider;
        [SerializeField] private Formation[] formations;

        public Bounds Bounds => boardCollider != null ? boardCollider.bounds : default;

        private void Awake()
        {
            if (boardCollider == null)
            {
                boardCollider = GetComponent<Collider>();
            }
        }

        public bool TryGetSlots(string formation, out IReadOnlyList<Transform> slots)
        {
            if (formations != null)
            {
                foreach (Formation f in formations)
                {
                    if (f != null && f.name == formation && f.slots != null && f.slots.Length > 0)
                    {
                        slots = f.slots;
                        return true;
                    }
                }
            }

            slots = null;
            return false;
        }
    }
}
