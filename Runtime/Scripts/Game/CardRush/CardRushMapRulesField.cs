using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 카드 러시 맵의 칸·레인 배치. 씬 마커(<see cref="CardRushMapRules"/>)가 맵 로드 때 스스로 값을 넣고, 사라질 때 거둔다
    /// (<see cref="FlappyMapRulesField"/>와 같은 통로). 마커가 없으면 <see cref="HasRules"/>가 거짓이라 월드는 아무것도 안 한다.
    /// </summary>
    public sealed class CardRushMapRulesField
    {
        public const int MinStepCount = 1;
        public const int MaxStepCount = 200;

        public bool HasRules { get; private set; }
        public CardRushTheme Theme { get; private set; }
        public int StepCount { get; private set; }
        public float StepSpacing { get; private set; }
        public float LaneSpacing { get; private set; }
        public Vector3 Origin { get; private set; }
        public Vector3 Forward { get; private set; } = Vector3.forward;

        public void Set(CardRushTheme theme, int stepCount, float stepSpacing, float laneSpacing,
                        Vector3 origin, Vector3 forward)
        {
            HasRules = true;
            Theme = theme;
            StepCount = stepCount;
            StepSpacing = stepSpacing;
            LaneSpacing = laneSpacing;
            Origin = origin;
            Forward = forward;
        }

        /// <summary>맵을 다시 로드하면 옛 마커가 사라진다 — 그때 거둬 "규칙 없음"으로 되돌린다.</summary>
        public void Clear()
        {
            HasRules = false;
            Theme = default;
            StepCount = 0;
            StepSpacing = 0f;
            LaneSpacing = 0f;
            Origin = Vector3.zero;
            Forward = Vector3.forward;
        }

        public bool Validate(out string error)
        {
            if (StepCount < MinStepCount || StepCount > MaxStepCount)
            {
                error = $"칸 수 {StepCount}가 {MinStepCount}~{MaxStepCount} 밖이다";
                return false;
            }
            error = null;
            return true;
        }

        public Vector3 LaneOrigin(int lane) => CardRushLanes.LaneOrigin(Origin, Forward, LaneSpacing, lane);
    }
}
