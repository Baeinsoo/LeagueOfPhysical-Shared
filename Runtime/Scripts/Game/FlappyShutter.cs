using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 놓는 주기 셔터 표시. 문(<see cref="Door"/>)이 칸 바닥까지 내려와 막았다가 천장 속으로 올라가 길을 다 연다.
    /// <see cref="FlappyPendulum"/>과 같은 이유로 공용 패키지에 있고, 맵이 올라올 때 <see cref="FlappyShutterField"/>에
    /// 스스로 등록한다. <b>Update()가 없다</b>: 문 높이는 필드가 매 틱 대입한다.
    /// </summary>
    [ExecuteAlways]
    [SceneInjectMonoBehaviour]
    public class FlappyShutter : MonoBehaviour
    {
        /// <summary>다 열렸을 때 문이 올라가는 거리(m). 닫힘이 0이다.</summary>
        public float Travel = 5.3f;
        /// <summary>한 번 닫혔다 열렸다 다시 닫히는 시간(초).</summary>
        public float Period = 2.5f;
        /// <summary>주기 중 다 열려 있는 몫.</summary>
        public float OpenShare = 0.4f;
        /// <summary>주기 중 올라가는(또 내려오는) 데 드는 몫. 각각 이만큼이다.</summary>
        public float MoveShare = 0.15f;
        /// <summary>0틱일 때 주기 위치(도). 셔터마다 달리 주면 동시에 닫히지 않는다.</summary>
        public float Phase = 0f;
        /// <summary>오르내리는 문. 닫힌 자리가 localPosition.y = 0이다.</summary>
        public Transform Door;

        private FlappyShutterField field;

        [Inject]
        public void Construct(FlappyShutterField field)
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
