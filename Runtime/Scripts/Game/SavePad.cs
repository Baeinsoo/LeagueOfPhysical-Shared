using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 놓는 세이브 발판. 보이는 판(렌더러 바운드)이 곧 발판이다 — 그 윗면에 내려앉으면 저장된다.
    /// <para><see cref="CheckpointMarker"/>와 같은 이유로 공용 패키지에 있다 — 서버도 같은 맵 씬을 읽는다.</para>
    /// </summary>
    [SceneInjectMonoBehaviour]
    public class SavePad : MonoBehaviour
    {
        /// <summary>맵 안에서 겹치지 않는 번호. 저장 상태에 이 값이 담긴다(되감기·표시).</summary>
        public int Id;

        /// <summary>화면에 보일 이름("섬", "테라스 1100").</summary>
        public string Label;

        private SavePadField field;
        private bool hasRegistered;

        [Inject]
        public void Construct(SavePadField field)
        {
            this.field = field;
            var renderer = GetComponentInChildren<Renderer>();
            field.Add(Id, renderer != null ? renderer.bounds : new Bounds(transform.position, Vector3.zero), Label);
            hasRegistered = true;
        }

        private void OnDestroy()
        {
            // 라운드마다 맵을 다시 로드한다 — 안 빼면 다음 판에 옛 발판이 남는다.
            if (hasRegistered && field != null)
            {
                field.Remove(Id);
            }
        }
    }
}
