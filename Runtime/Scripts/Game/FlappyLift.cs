using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 놓는 승강기 표시. 관문의 위·아래 파이프를 자식 <see cref="Cage"/>로 묶어 위아래로 오르내린다(광산 승강기·흔들리는 계단).
    /// <see cref="FlappyShutter"/>와 같은 이유로 공용 패키지에 있고, 맵이 올라올 때 <see cref="FlappyLiftField"/>에 스스로 등록한다.
    /// <b>Update()가 없다</b>: 케이지 높이는 필드가 매 틱 대입한다.
    /// </summary>
    [ExecuteAlways]
    [SceneInjectMonoBehaviour]
    public class FlappyLift : MonoBehaviour
    {
        /// <summary>가운데에서 위·아래로 가는 거리(m).</summary>
        public float Amplitude = 1.8f;
        /// <summary>한 번 오르내리는 시간(초).</summary>
        public float Period = 3.5f;
        /// <summary>0틱일 때 사인의 위상(도). 관문마다 달리 주면 같이 안 움직인다.</summary>
        public float Phase = 0f;
        /// <summary>오르내리는 파이프 묶음. 구운 자리(가운데)가 localPosition.y = 0이다.</summary>
        public Transform Cage;

        private FlappyLiftField field;

        [Inject]
        public void Construct(FlappyLiftField field)
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
    }
}
