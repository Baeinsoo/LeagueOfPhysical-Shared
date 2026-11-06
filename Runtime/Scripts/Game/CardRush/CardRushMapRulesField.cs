using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 카드 러시 맵의 레인 배치. 씬 마커(<see cref="CardRushMapRules"/>)가 맵 로드 때 스스로 값을 넣고, 사라질 때 거둔다
    /// (<see cref="FlappyMapRulesField"/>와 같은 통로). 마커가 없으면 <see cref="HasRules"/>가 거짓이라 월드는 아무것도 안 한다.
    /// 코스 길이는 맵이 아니라 마스터데이터(<see cref="CardRushTuning"/>)가 정한다.
    /// </summary>
    public sealed class CardRushMapRulesField
    {
        public bool HasRules { get; private set; }
        public CardRushTheme Theme { get; private set; }
        public float LaneSpacing { get; private set; }
        public Vector3 Origin { get; private set; }
        public Vector3 Forward { get; private set; } = Vector3.forward;

        public void Set(CardRushTheme theme, float laneSpacing, Vector3 origin, Vector3 forward)
        {
            HasRules = true;
            Theme = theme;
            LaneSpacing = laneSpacing;
            Origin = origin;
            Forward = forward;
        }

        /// <summary>맵을 다시 로드하면 옛 마커가 사라진다 — 그때 거둬 "규칙 없음"으로 되돌린다.</summary>
        public void Clear()
        {
            HasRules = false;
            Theme = default;
            LaneSpacing = 0f;
            Origin = Vector3.zero;
            Forward = Vector3.forward;
        }

        public bool Validate(out string error)
        {
            //  간격이 0이면 레인이 한 줄에 겹치고 레인 번호(첫 장 씨앗)를 못 구한다.
            if (!(LaneSpacing > 0f))
            {
                error = $"레인 간격 {LaneSpacing}이 0보다 커야 한다";
                return false;
            }
            error = null;
            return true;
        }

        public Vector3 LaneOrigin(int lane) => CardRushLanes.LaneOrigin(Origin, Forward, LaneSpacing, lane);
    }
}
