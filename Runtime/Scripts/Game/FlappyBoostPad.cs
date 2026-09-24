using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 놓는 부스트 패드 표시. 맵이 올라올 때 <see cref="FlappyBoostPadField"/>를 주입받아
    /// 스스로 등록한다.
    ///
    /// <para><see cref="WindVolume"/>·<see cref="FlappyWindmill"/>과 같은 이유로 <b>공용 패키지</b>에
    /// 있다: 맵 씬은 클라에서 만들고 서버가 읽는데, 스크립트가 한쪽에만 있으면 반대쪽에서 missing
    /// script가 되고 그 빈 컴포넌트가 씬 주입을 끊는다. 이 패드는 특히 그래야 한다 — 부스트가
    /// 전진 속도를 두 배로 만들므로 한쪽만 밟으면 그 자리에서 갈린다.</para>
    ///
    /// <para><b>콜라이더를 쓰지 않는다.</b> 판정은 필드가 사각형 포함 여부를 산술로 한다 —
    /// 트리거로 하면 롤백 재생에서 물리를 안 돌려 아예 답이 없다.</para>
    /// </summary>
    // 이 표시가 없으면 유니티는 플레이 중이 아닐 때 Awake/OnDestroy 같은 생명주기 함수를
    // 아예 불러 주지 않는다 — 그러면 씬 편집 중에 마커를 지워도 등록 해제가 안 걸린다.
    [ExecuteAlways]
    [SceneInjectMonoBehaviour]
    public class FlappyBoostPad : MonoBehaviour
    {
        /// <summary>가로 길이(m). 이 길이를 지나는 동안 계속 다시 밟히므로, 길면 끝에서 나갈 때 남는 시간이 같다.</summary>
        public float Width = 6f;

        /// <summary>세로 길이(m). 틈 위나 아래 한쪽에만 놓아야 "위험한 쪽에 상이 있다"가 성립한다.</summary>
        public float Height = 4f;

        /// <summary>밟으면 몇 초 동안 대시 상태가 되나.</summary>
        public float Duration = 0.6f;

        private FlappyBoostPadField field;
        private FlappyBoostRect rect;

        [Inject]
        public void Construct(FlappyBoostPadField field)
        {
            this.field = field;
            Vector3 center = transform.position;
            rect = new FlappyBoostRect(center.x - Width * 0.5f, center.x + Width * 0.5f,
                                       center.y - Height * 0.5f, center.y + Height * 0.5f, Duration);
            field.Add(rect);
        }

        private void OnDestroy()
        {
            // 라운드가 여러 판이면 맵을 다시 로드한다 — 안 빼면 죽은 패드가 목록에 남는다.
            if (field != null && rect != null)
            {
                field.Remove(rect);
            }
        }

        // 배치가 곧 코스 설계다. 어디서 얼마나 빨라지는지 씬 뷰에서 보이게 한다.
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.5f);
            Gizmos.DrawWireCube(transform.position, new Vector3(Width, Height, 1f));
        }
    }
}
