using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 놓는 빛 알갱이 무더기. 맵이 올라올 때 <see cref="FlappyMoteField"/>를 주입받아 스스로 등록한다.
    ///
    /// <para><see cref="FlappyBoostPad"/>와 같은 이유로 <b>공용 패키지</b>에 있다: 맵 씬은 클라에서 만들고
    /// 서버가 읽는데, 스크립트가 한쪽에만 있으면 반대쪽에서 missing script가 되고 그 빈 컴포넌트가 씬 주입을 끊는다.</para>
    ///
    /// <para><b>알갱이 자리는 구워서 들고 있다.</b> 실행 중에 난수로 만들지 않는다 — 클·서가 같은 씬 데이터만
    /// 읽으면 같은 자리가 된다. 난수는 맵을 만드는 에디터 도구가 한 번만 쓴다.</para>
    /// </summary>
    // 이 표시가 없으면 유니티는 플레이 중이 아닐 때 Awake/OnDestroy 같은 생명주기 함수를
    // 아예 불러 주지 않는다 — 그러면 씬 편집 중에 마커를 지워도 등록 해제가 안 걸린다.
    [ExecuteAlways]
    [SceneInjectMonoBehaviour]
    public class FlappyMoteCluster : MonoBehaviour
    {
        /// <summary>알갱이 자리 — 이 마커 위치에서 잰 (x, y). z는 쓰지 않는다(코스는 x-y 평면 한 장).</summary>
        public Vector2[] Points = System.Array.Empty<Vector2>();

        /// <summary>금빛 큰 무더기(벽에 바짝 몰린 것)인가. 그림만 다르다.</summary>
        public bool Big;

        /// <summary>붙은 관문 틈의 반 높이(m). 마커 자리가 틈 중심이다. 그림(빛 띠)만 쓴다 — 0이면 띠를 안 그린다.</summary>
        public float GapHalf;

        private FlappyMoteField field;
        private FlappyMoteSet set;

        [Inject]
        public void Construct(FlappyMoteField field)
        {
            this.field = field;
            Vector3 origin = transform.position;
            var xs = new float[Points.Length];
            var ys = new float[Points.Length];
            for (int i = 0; i < Points.Length; i++)
            {
                xs[i] = origin.x + Points[i].x;
                ys[i] = origin.y + Points[i].y;
            }
            set = new FlappyMoteSet(xs, ys, Big, origin.x, origin.y, GapHalf);
            field.Add(set);
        }

        private void OnDestroy()
        {
            // 라운드가 여러 판이면 맵을 다시 로드한다 — 안 빼면 죽은 무더기가 목록에 남는다.
            if (field != null && set != null)
            {
                field.Remove(set);
            }
        }

        // 배치가 곧 코스 설계다. 어디에 얼마나 있는지 씬 뷰에서 보이게 한다.
        private void OnDrawGizmos()
        {
            Gizmos.color = Big ? new Color(1f, 0.84f, 0.3f, 0.9f) : new Color(0.37f, 0.89f, 1f, 0.9f);
            Vector3 origin = transform.position;
            for (int i = 0; i < Points.Length; i++)
            {
                Gizmos.DrawSphere(origin + new Vector3(Points[i].x, Points[i].y, 0f), 0.06f);
            }
        }
    }
}
