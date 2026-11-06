namespace LOP
{
    public static class CardRushCard
    {
        //  카드가 늘 돈다(빨 → 노 → 파, 한 장 CardDwellTicks). 탭 = 지금 보이는 카드를 바로 쓴다(spec §3.3).

        /// <summary>카드가 도는 기준 틱 — 아직 안 썼으면 출발 틱, 썼으면 마지막 사용 + 쿨다운.</summary>
        public static long SpinBase(in CardRushRunnerState s, long gameplayStartTick)
            => s.SpinStartTick != 0 ? s.SpinStartTick : gameplayStartTick;

        /// <summary>그 틱에 보이는 카드. 출발 전·쿨다운 중이면 false(뒷면). 기절 중에도 돈다(못 쓸 뿐).</summary>
        public static bool TryCardAt(in CardRushRunnerState s, long tick, long gameplayStartTick, int lane, CardRushTrack track, out CardColor color)
        {
            color = default;
            long spinBase = SpinBase(s, gameplayStartTick);
            if (track == null || tick < spinBase) return false;
            long turns = (tick - spinBase) / CardRushRules.CardDwellTicks;
            color = (CardColor)(((int)track.FirstCard(lane, s.UseCount) + (int)(turns % CardColors.Count)) % CardColors.Count);
            return true;
        }

        /// <summary>이 틱에 탭하면 카드를 쓰나. 못 쓰는 틱의 탭은 버린다(쌓지 않는다).</summary>
        public static bool CanUse(in CardRushRunnerState s, long tick, long gameplayStartTick)
            => tick > 0 && tick >= gameplayStartTick && s.FinishTick == 0
               && tick >= SpinBase(s, gameplayStartTick) && tick >= s.StunEndTick;
    }
}
