namespace LOP
{
    public static class CardRushLanes
    {
        public const int MaxLanes = 8;

        public static UnityEngine.Vector3 Flat(UnityEngine.Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude < 1e-8f ? UnityEngine.Vector3.forward : v.normalized;
        }

        /// <summary>레인 i의 출발점. 늘 8레인 기준이라 인원이 달라도 모양이 같다.</summary>
        public static UnityEngine.Vector3 LaneOrigin(UnityEngine.Vector3 marker, UnityEngine.Vector3 forward, float laneSpacing, int lane)
        {
            var right = UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, Flat(forward));
            return marker + right * ((lane - (MaxLanes - 1) * 0.5f) * laneSpacing);
        }

        /// <summary>레인 출발점에서 앞으로 posMm 간 자리.</summary>
        public static UnityEngine.Vector3 PositionAt(UnityEngine.Vector3 laneOrigin, UnityEngine.Vector3 forward, int posMm)
            => laneOrigin + Flat(forward) * (posMm / 1000f);

        /// <summary>
        /// 자리 → 레인 번호(0~7). 옆 방향 거리만 본다 — 앞뒤(얼마나 갔나)·높이는 버린다. 레인 번호는 첫 장 씨앗이라
        /// 서버(스폰 자리)와 클라(받은 자리 — 판 도중이면 앞으로 간 자리)가 이 한 식으로 같은 값을 낸다.
        /// 규칙이 없으면 0.
        /// </summary>
        public static int LaneIndexOf(CardRushMapRulesField rules, UnityEngine.Vector3 position)
        {
            if (rules == null || rules.HasRules == false || rules.LaneSpacing <= 0f) return 0;
            var right = UnityEngine.Vector3.Cross(UnityEngine.Vector3.up, Flat(rules.Forward));
            float offset = UnityEngine.Vector3.Dot(position - rules.Origin, right) / rules.LaneSpacing;
            return UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(offset + (MaxLanes - 1) * 0.5f), 0, MaxLanes - 1);
        }
    }
}
