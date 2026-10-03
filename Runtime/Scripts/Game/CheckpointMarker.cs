using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 놓는 체크포인트 표식. 위치가 부활 지점이고 그 높이가 체크포인트 고도다.
    /// <para><see cref="LaserVolume"/>과 같은 이유로 공용 패키지에 있다 — 서버도 같은 맵 씬을 읽는다.</para>
    /// </summary>
    [SceneInjectMonoBehaviour]
    public class CheckpointMarker : MonoBehaviour
    {
        private CheckpointField field;
        private Vector3 registered;
        private bool hasRegistered;

        [Inject]
        public void Construct(CheckpointField field)
        {
            this.field = field;
            registered = transform.position;
            hasRegistered = true;
            field.Add(registered.y, registered);
        }

        private void OnDestroy()
        {
            // 라운드마다 맵을 다시 로드한다 — 안 빼면 다음 판에 옛 표식이 남는다.
            if (hasRegistered && field != null)
            {
                field.Remove(registered.y, registered);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 2f);
        }
    }
}
