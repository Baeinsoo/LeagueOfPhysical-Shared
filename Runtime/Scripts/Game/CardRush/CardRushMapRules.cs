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
    /// <para>옛 씬에 남은 <c>sections</c> 직렬화 값은 유니티가 무시한다 — 씬을 다시 굽지 않아도 된다.</para>
    /// </summary>
    // 이 표시가 없으면 유니티는 플레이 중이 아닐 때 OnDestroy 같은 생명주기 함수를 불러 주지 않는다.
    [ExecuteAlways]
    [SceneInjectMonoBehaviour]
    public class CardRushMapRules : MonoBehaviour
    {
        public CardRushTheme theme = CardRushTheme.StoneBridge;

        /// <summary>결승까지의 칸 수(1~200).</summary>
        public int stepCount = 40;

        /// <summary>칸 사이 거리(m).</summary>
        public float stepSpacing = 2.2f;

        /// <summary>레인 사이 거리(m). 늘 8레인 기준으로 놓는다.</summary>
        public float laneSpacing = 2.6f;

        private CardRushMapRulesField field;

        [Inject]
        public void Construct(CardRushMapRulesField field)
        {
            this.field = field;
            field.Set(theme, stepCount, stepSpacing, laneSpacing, transform.position, transform.forward);
            if (field.Validate(out var error))
            {
                Debug.Log($"[CardRushMapRules] {theme} 칸 {stepCount} 간격 {stepSpacing} 레인 {laneSpacing}");
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
            probe.Set(theme, stepCount, stepSpacing, laneSpacing, transform.position, transform.forward);
            if (probe.Validate(out var error))
            {
                return;
            }
            Debug.LogError($"[CardRushMapRules] {name}: {error}", this);
        }

        //  에디터에서 배치를 보는 용도 — 레인 8개 선과 칸 점.
        private void OnDrawGizmos()
        {
            var forward = CardRushLanes.Flat(transform.forward);
            int steps = Mathf.Clamp(stepCount, 0, CardRushMapRulesField.MaxStepCount);
            for (int lane = 0; lane < CardRushLanes.MaxLanes; lane++)
            {
                var start = CardRushLanes.LaneOrigin(transform.position, forward, laneSpacing, lane);
                var end = CardRushLanes.PositionAt(start, forward, stepSpacing, steps);
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(start, end);
                Gizmos.color = Color.white;
                for (int s = 0; s <= steps; s++)
                {
                    Gizmos.DrawSphere(CardRushLanes.PositionAt(start, forward, stepSpacing, s), 0.12f);
                }
            }
        }
    }
}
