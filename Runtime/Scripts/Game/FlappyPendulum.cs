using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 놓는 철골 진자 표시. 맵이 올라올 때 <see cref="FlappyPendulumField"/>를 주입받아 스스로 등록한다.
    /// <see cref="FlappyWindmill"/>과 같은 이유로 공용 패키지에 있다 — 서버도 같은 씬을 읽으므로 한쪽에만 있으면
    /// missing script가 되고 씬 주입이 끊긴다. <b>Update()가 없다</b>: 자세는 필드가 매 틱 대입한다.
    /// </summary>
    [ExecuteAlways]
    [SceneInjectMonoBehaviour]
    public class FlappyPendulum : MonoBehaviour
    {
        /// <summary>좌우 최대 각도(도).</summary>
        public float Amplitude = 55f;
        /// <summary>한 번 왕복하는 시간(초).</summary>
        public float Period = 2.5f;
        /// <summary>0틱일 때 사인의 위상(도). 진자마다 달리 주면 동시에 닫히지 않는다.</summary>
        public float Phase = 0f;

        private FlappyPendulumField field;

        [Inject]
        public void Construct(FlappyPendulumField field)
        {
            this.field = field;
            field.Add(this);
        }

        private void OnDestroy()
        {
            if (field != null)
            {
                field.Remove(this);
            }
        }

        //  쓸고 지나가는 범위가 곧 코스 설계라 씬 뷰에서 보이게 한다(풍차와 같은 자리).
        private void OnDrawGizmos()
        {
            float reach = 0f;
            for (int i = 0; i < transform.childCount; i++)
            {
                reach = Mathf.Max(reach, transform.GetChild(i).localPosition.magnitude);
            }
            if (reach <= 0f)
            {
                return;
            }
            Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, reach);
        }
    }
}
