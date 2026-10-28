namespace LOP
{
    /// <summary>
    /// 연속 날갯짓 세기·끊기·고비 보상(spec 2026-10-27-flappy-combo-dash-reward). 클·서가 같은 구체 클래스를 돌려 결과가 갈리지 않는다.
    /// <para>어느 날갯짓을 셀지는 월드가 정한다 — 이동 커널이 그 날갯짓을 실제로 쓰는 틱에만 <see cref="Flap"/>을 부른다
    /// (대시 중·결승 뒤·기절 중·출발 전은 안 부른다). 여기서 대시·결승을 다시 묻지 않는 것은 시스템끼리 고리를 안 만들려는 것이다.</para>
    /// </summary>
    public class FlappyFlapComboSystem
    {
        private readonly FlappyDashSystem dash;
        private readonly FlappyMapRulesField rules;

        /// <param name="rules"><c>null</c>은 규칙 없이 만든 시험·도구용이라 기본 고비로 센다. 실제 판은 DI가 늘 필드를 넣는다.</param>
        public FlappyFlapComboSystem(FlappyDashSystem dash, FlappyMapRulesField rules = null)
        {
            this.dash = dash;
            this.rules = rules;
        }

        public FlappyFlapComboRules Rules => rules?.FlapCombo ?? FlappyFlapComboRules.Default;

        /// <summary>시뮬이 이번 틱 날갯짓을 받았다 — 연속 +1, 그 수가 고비면 게이지(버튼 없는 맵·가득이면 대시 쪽이 거른다).</summary>
        public void Flap(GameFramework.World.Entity entity)
        {
            var combo = entity.Get<FlappyFlapCombo>();
            if (combo == null)
            {
                return;
            }
            combo.Count++;
            float gain = Rules.Gain(combo.Count);
            if (gain > 0f)
            {
                dash.GrantCharge(entity, gain);
            }
        }

        /// <summary>새로 기절에 들어갔다 — 연속이 끊긴다. 무적 중에 닿은 것은 기절이 아니라 부르지 않는다.</summary>
        public void Break(GameFramework.World.Entity entity)
        {
            var combo = entity.Get<FlappyFlapCombo>();
            if (combo != null)
            {
                combo.Count = 0;
            }
        }
    }
}
