using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 카드 러시 맵 씬에 놓는 마커. 놓인 자리가 가운데 레인 사이의 출발선, <c>transform.forward</c>가 달리는 방향이다.
    /// 맵이 올라올 때 <see cref="CardRushMapRulesField"/>를 주입받아 스스로 값을 넣는다.
    ///
    /// <para><see cref="FlappyMapRules"/>와 같은 이유로 <b>공용 패키지</b>에 있다: 맵 씬은 클라에서 만들고 서버가 읽는데,
    /// 스크립트가 한쪽에만 있으면 반대쪽에서 missing script가 되고 그 빈 컴포넌트가 씬 주입을 끊는다.</para>
    ///
    /// <para>옛 씬에 남은 <c>stepCount</c>·<c>stepSpacing</c>·<c>sections</c> 직렬화 값은 유니티가 무시한다(씬을 다시 저장하면 빠진다).
    /// <see cref="theme"/>은 이제 그림을 고르지 않는다 — 저장 값 호환 때문에만 남긴다.</para>
    /// </summary>
    // 이 표시가 없으면 유니티는 플레이 중이 아닐 때 OnDestroy 같은 생명주기 함수를 불러 주지 않는다.
    [ExecuteAlways]
    [SceneInjectMonoBehaviour]
    public class CardRushMapRules : MonoBehaviour
    {
        public CardRushTheme theme = CardRushTheme.StoneBridge;

        /// <summary>레인 사이 거리(m). 늘 8레인 기준으로 놓는다.</summary>
        public float laneSpacing = 2.6f;

        private CardRushMapRulesField field;

        [Inject]
        public void Construct(CardRushMapRulesField field)
        {
            this.field = field;
            field.Set(theme, laneSpacing, transform.position, transform.forward);
            if (field.Validate(out var error))
            {
                Debug.Log($"[CardRushMapRules] {theme} 레인 {laneSpacing}");
            }
            else
            {
                //  월드는 검증을 통과한 규칙으로만 코스를 만든다 — 여기서 크게 알린다.
                Debug.LogError($"[CardRushMapRules] 규칙이 잘못돼 판이 돌지 않는다: {error}");
            }
        }

        private void OnDestroy()
        {
            // 라운드가 여러 판이면 맵을 다시 로드한다 — 안 거두면 지난 맵의 규칙이 새 맵에 남는다.
            if (field != null)
            {
                field.Clear();
            }
        }

        private void OnValidate()
        {
            var probe = new CardRushMapRulesField();
            probe.Set(theme, laneSpacing, transform.position, transform.forward);
            if (probe.Validate(out var error))
            {
                return;
            }
            Debug.LogError($"[CardRushMapRules] {name}: {error}", this);
        }

        //  에디터에서 배치를 보는 용도 — 레인 8줄과 10 m 눈금. 코스 길이는 마스터데이터라 최대 길이까지 그린다.
        private void OnDrawGizmos()
        {
            var forward = CardRushLanes.Flat(transform.forward);
            var right = Vector3.Cross(Vector3.up, forward);
            float halfWidth = laneSpacing * CardRushLanes.MaxLanes * 0.5f;
            Gizmos.color = Color.cyan;
            for (int lane = 0; lane < CardRushLanes.MaxLanes; lane++)
            {
                var start = CardRushLanes.LaneOrigin(transform.position, forward, laneSpacing, lane);
                Gizmos.DrawLine(start, CardRushLanes.PositionAt(start, forward, CardRushRules.MaxTrackMm));
            }
            Gizmos.color = Color.white;
            for (int mm = 0; mm <= CardRushRules.MaxTrackMm; mm += GizmoTickMm)
            {
                var at = CardRushLanes.PositionAt(transform.position, forward, mm);
                Gizmos.DrawLine(at - right * halfWidth, at + right * halfWidth);
            }
        }

        private const int GizmoTickMm = 10_000;
    }
}
