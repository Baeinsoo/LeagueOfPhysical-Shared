using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 놓는 추격자·수동 대시 스위치. 맵이 올라올 때 <see cref="FlappyMapRulesField"/>를
    /// 주입받아 스스로 값을 넣는다.
    ///
    /// <para><see cref="FlappyBoostPad"/>와 같은 이유로 <b>공용 패키지</b>에 있다: 맵 씬은 클라에서
    /// 만들고 서버가 읽는데, 스크립트가 한쪽에만 있으면 반대쪽에서 missing script가 되고 그 빈
    /// 컴포넌트가 씬 주입을 끊는다.</para>
    ///
    /// <para>이 마커가 없는 맵(지금의 라이브 맵)에서는 필드가 기본값(둘 다 꺼짐)으로 남는다 —
    /// 추격자는 아무도 잡지 않고, 대시는 다이브로 못 차고 부스트 패드로만 받는다.</para>
    /// </summary>
    // 이 표시가 없으면 유니티는 플레이 중이 아닐 때 Awake/OnDestroy 같은 생명주기 함수를
    // 아예 불러 주지 않는다 — 그러면 씬 편집 중에 마커를 지워도 등록 해제가 안 걸린다.
    [ExecuteAlways]
    [SceneInjectMonoBehaviour]
    public class FlappyMapRules : MonoBehaviour
    {
        /// <summary>추격자(뒤에서 오는 벽)가 잡는 판정을 켜나.</summary>
        public bool Chaser;

        /// <summary>다이브로 대시 게이지를 직접 채우나. 꺼져 있으면 부스트 패드로만 대시를 받는다.</summary>
        public bool ManualDash;

        private FlappyMapRulesField field;

        [Inject]
        public void Construct(FlappyMapRulesField field)
        {
            this.field = field;
            field.Set(Chaser, ManualDash);
        }

        private void OnDestroy()
        {
            // 라운드가 여러 판이면 맵을 다시 로드한다 — 안 거두면 지난 맵의 룰이 새 맵에 남는다.
            if (field != null)
            {
                field.Clear();
            }
        }
    }
}
