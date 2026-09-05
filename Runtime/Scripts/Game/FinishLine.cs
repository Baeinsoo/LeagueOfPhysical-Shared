using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 찍어 두는 결승선. 게임 룰이 이 지점의 좌표를 읽어 "전원이 넘었나"를 판정한다.
    ///
    /// <see cref="SpawnPoint"/>와 같은 이유로 <b>공용 패키지</b>에 있다: 맵 씬은 클라에서 만들고
    /// 서버가 읽는데, 스크립트가 한쪽에만 있으면 반대쪽에서 missing script가 되고 그 빈 컴포넌트가
    /// 씬 주입을 끊는다. (Unreal의 ATriggerVolume 계열 골 마커에 대응 — 좌표를 코드가 아니라
    /// 맵이 정한다.)
    ///
    /// 맵이 올라올 때 <see cref="FinishLineBounds"/>를 주입받아 <b>스스로 등록한다</b>
    /// (<c>GameLifetimeScope</c>가 <c>sceneLoaded</c>를 듣고 <c>InjectSceneObjects</c>를 부른다).
    /// 그래야 시뮬이 첫 틱에 씬을 훑지 않는다.
    ///
    /// 어느 축을 읽을지는 마커가 아니라 <b>게임이 정한다</b> — 마커는 형상만 내줄 뿐이다.
    /// </summary>
    [SceneInjectMonoBehaviour]
    public class FinishLine : MonoBehaviour
    {
        private FinishLineBounds line;

        [Inject]
        public void Construct(FinishLineBounds line)
        {
            this.line = line;
            Bounds shape = Shape();
            line.Register(shape);
            //  [진단용 임시] 맵 마커가 실제로 주입되는지. 안 찍히면 씬 주입이 안 걸린 것이다.
            Debug.Log($"[Finish] 결승선 등록 min={shape.min} max={shape.max}");
        }

        //  보이는 판이 곧 결승선이다. 렌더러가 없으면(마커만 찍어 둔 맵) 두께 0인 선으로 쓴다.
        private Bounds Shape()
        {
            var renderer = GetComponentInChildren<Renderer>();
            return renderer != null ? renderer.bounds : new Bounds(transform.position, Vector3.zero);
        }

        private void OnDestroy()
        {
            // 라운드가 여러 판이면 맵을 다시 로드한다 — 안 거두면 옛 선이 남는다.
            line?.Unregister();
        }
    }
}
