using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 놓는 낙석 표시. 바위(<see cref="Rock"/>)가 천장에서 곧게 떨어져 통로를 가로막았다가 바닥 밑으로 빠진다.
    /// <see cref="FlappyLift"/>와 같은 이유로 공용 패키지에 있고, 맵이 올라올 때 <see cref="FlappyRockfallField"/>에
    /// 스스로 등록한다. <b>Update()가 없다</b>: 바위 자리는 필드가 매 틱 대입한다.
    /// 예고(경고) 상태는 없다 — 떨어지지 않는 동안은 천장 속(판정 밖)에 가만히 숨어 있다(10-13 사용자).
    /// </summary>
    [ExecuteAlways]
    [SceneInjectMonoBehaviour]
    public class FlappyRockfall : MonoBehaviour
    {
        /// <summary>한 번 떨어지고 다시 떨어질 때까지(초).</summary>
        public float Period = 2f;
        /// <summary>떨어지는 빠르기(m/s). 곧게 일정하게 떨어진다.</summary>
        public float Speed = 9f;
        /// <summary>바위 윗면이 천장에서 내려가는 거리(m) — 통로 높이 + 바위 크기면 바닥 밑까지 다 빠진다.
        /// Speed·Period는 Drop보다 여유 있게 커야 한다 — 다 떨어진 뒤 다음 주기가 오기 전까지 숨을 시간이 있어야 한다.</summary>
        public float Drop = 15.76f;
        /// <summary>바위 한 변(m).</summary>
        public float Size = 1.2f;
        /// <summary>숨어 있을 때 바위 가운데가 천장선 위로 올라가 있는 높이(m) — 천장 바위 속이라 판정에 안 닿는다.</summary>
        public float HiddenRise = 5.6f;
        /// <summary>0틱일 때 주기 위치(도). 줄마다 달리 주면 같이 안 떨어진다.</summary>
        public float Phase = 0f;
        /// <summary>떨어지는 바위. 마커 원점이 천장선이다.</summary>
        public Transform Rock;

        private FlappyRockfallField field;

        [Inject]
        public void Construct(FlappyRockfallField field)
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
