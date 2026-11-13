namespace LOP
{
    public static class CardRushCard
    {
        //  카드가 늘 돈다(빨 → 노 → 파, 한 장 CardDwellTicks). 첫 탭 = 잡기(카드가 멈춘다), 다시 탭 = 쓰기(4차 spec §3.2).
        //  잡은 카드는 LockHoldTicks 안에 안 쓰면 사라진다 — 쓴 것과 같은 쿨다운 뒤 새 첫 장부터 다시 돈다.

        /// <summary>카드가 도는 기준 틱 — 아직 안 썼으면 출발 틱, 썼(거나 사라졌)으면 마지막 + 쿨다운.</summary>
        public static long SpinBase(in CardRushRunnerState s, long gameplayStartTick)
            => s.SpinStartTick != 0 ? s.SpinStartTick : gameplayStartTick;

        /// <summary>도는 카드가 그 틱에 보일 색 — 잡았는지는 안 본다. 기준 틱 전이면 의미 없는 값이라 부르는 쪽이 거른다.</summary>
        public static CardColor SpinColor(in CardRushRunnerState s, long tick, long gameplayStartTick, int lane, CardRushTrack track)
        {
            long turns = (tick - SpinBase(s, gameplayStartTick)) / CardRushRules.CardDwellTicks;
            return (CardColor)(((int)track.FirstCard(lane, s.UseCount) + (int)(turns % CardColors.Count)) % CardColors.Count);
        }

        /// <summary>
        /// 그 틱에 머리 위에 있는 카드. 잡았으면 잡은 색(멈춘다), 아니면 도는 색. 출발 전·쿨다운 중이면 false(뒷면).
        /// 기절 중에도 돈다(못 쓸 뿐).
        /// </summary>
        public static bool CardAt(in CardRushRunnerState s, long tick, long gameplayStartTick, int lane, CardRushTrack track, out CardColor color)
        {
            color = default;
            if (track == null) return false;
            if (s.LockTick != 0)
            {
                color = SpinColor(s, s.LockTick, gameplayStartTick, lane, track);
                return true;
            }
            if (tick < SpinBase(s, gameplayStartTick)) return false;
            color = SpinColor(s, tick, gameplayStartTick, lane, track);
            return true;
        }

        /// <summary>잡든 쓰든 손을 쓸 수 있는 틱 — 출발 뒤, 완주 전, 기절 아님.</summary>
        public static bool CanAct(in CardRushRunnerState s, long tick, long gameplayStartTick)
            => tick > 0 && tick >= gameplayStartTick && s.FinishTick == 0 && tick >= s.StunEndTick;

        /// <summary>이 틱에 탭하면 도는 카드를 잡나.</summary>
        public static bool CanLock(in CardRushRunnerState s, long tick, long gameplayStartTick)
            => s.LockTick == 0 && CanAct(s, tick, gameplayStartTick) && tick >= SpinBase(s, gameplayStartTick);

        /// <summary>
        /// 이 틱에 탭하면 잡은 카드를 쓰나. 잡은 틱엔 못 쓴다(한 틱에 잡기 하나). 노랑·파랑은 땅에서만 — 공중 탭은 버리고 잡은 카드는 남는다.
        /// 못 쓰는 틱의 탭은 버린다(쌓지 않는다).
        /// </summary>
        public static bool CanUse(in CardRushRunnerState s, long tick, long gameplayStartTick, int lane, CardRushTrack track)
        {
            if (s.LockTick == 0 || tick <= s.LockTick || track == null || !CanAct(s, tick, gameplayStartTick)) return false;
            return s.JumpStartTick == 0 || SpinColor(s, s.LockTick, gameplayStartTick, lane, track) == CardColor.Red;
        }

        /// <summary>잡은 카드가 이 틱에 사라지나 — 잡고 LockHoldTicks가 지났다. 기절 중에도 시간은 흐른다.</summary>
        public static bool LockExpired(in CardRushRunnerState s, long tick)
            => s.LockTick != 0 && tick >= s.LockTick + CardRushRules.LockHoldTicks;
    }
}
