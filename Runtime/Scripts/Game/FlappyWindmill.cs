using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 놓는 풍차(십자 날개) 표시. 맵이 올라올 때 <see cref="FlappyWindmillField"/>를
    /// 주입받아 스스로 등록한다.
    ///
    /// <para><see cref="WindVolume"/>·<see cref="SpawnPoint"/>와 같은 이유로 <b>공용 패키지</b>에
    /// 있다: 맵 씬은 클라에서 만들고 서버가 읽는데, 스크립트가 한쪽에만 있으면 반대쪽에서 missing
    /// script가 되고 그 빈 컴포넌트가 씬 주입을 끊는다. 이 풍차는 특히 그래야 한다 — 날개에 부딪혀
    /// 얼어붙는 판정(<c>FlappyStunSystem</c>)이 클·서 공유 시뮬에서 돌기 때문에, 한쪽만 날개를
    /// 돌리면 곧장 갈린다.</para>
    ///
    /// <para><b><c>Update()</c>가 없다.</b> 자세는 <see cref="FlappyWindmillField.PoseForTick"/>이
    /// 매 틱 <see cref="FlappyWindmillCurve"/>로 다시 계산해 <b>대입</b>한다. 프레임마다 조금씩
    /// 더해 가면 프레임레이트에 따라 위상이 갈리고 과거 틱의 각도를 물을 수 없다.</para>
    /// </summary>
    // 이 표시가 없으면 유니티는 플레이 중이 아닐 때 Awake/OnDestroy 같은 생명주기 함수를
    // 아예 불러 주지 않는다 — 그러면 씬 편집 중에 마커를 지워도 등록 해제가 안 걸린다.
    [ExecuteAlways]
    [SceneInjectMonoBehaviour]
    public class FlappyWindmill : MonoBehaviour
    {
        /// <summary>도 / 초. 음수면 반대 방향으로 돈다.</summary>
        public float RotSpeed = 55f;

        /// <summary>0틱일 때의 각도(도). 풍차마다 다르게 줘야 관문이 동시에 닫히지 않는다.</summary>
        public float StartAngle = 0f;

        private FlappyWindmillField field;

        [Inject]
        public void Construct(FlappyWindmillField field)
        {
            this.field = field;
            field.Add(this);
        }

        private void OnDestroy()
        {
            // 라운드가 여러 판이면 맵을 다시 로드한다 — 안 빼면 죽은 풍차가 목록에 남는다.
            if (field != null)
            {
                field.Remove(this);
            }
        }

        // 배치가 곧 코스 설계다. 날개가 한 바퀴 도는 동안 어디까지 쓸고 가는지 씬 뷰에서 보이게 한다.
        // (WindVolume.OnDrawGizmos와 같은 이유·같은 자리에 둔다.)
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
