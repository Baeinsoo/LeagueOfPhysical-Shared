namespace LOP
{
    public static class CardRushCard
    {
        //  평소엔 멈춘 뒷면. 탭 = 돌기 시작, 다시 탭(또는 RollMaxTicks 뒤 자동) = 잡기, 또 탭 = 쓰기(4.1차 spec).
        //  잡은 카드는 LockHoldTicks 안에 안 쓰면 사라진다 — 쓴 것과 같은 쿨다운 뒤 평소로 돌아간다.

        /// <summary>카드를 돌리기 시작할 수 있는 가장 이른 틱 — 아직 안 썼으면 출발 틱, 썼(거나 사라졌)으면 마지막 + 쿨다운. 색의 기준이 아니다.</summary>
        public static long SpinBase(in CardRushRunnerState s, long gameplayStartTick)
            => s.SpinStartTick != 0 ? s.SpinStartTick : gameplayStartTick;

        /// <summary>도는 카드가 그 틱에 보일 색 — 돌기 시작한 틱(RollStartTick)부터 한 장씩 무작위로 넘긴다(4.2차). 돌고 있지 않으면 의미 없는 값이라 부르는 쪽이 거른다.</summary>
        public static CardColor SpinColor(in CardRushRunnerState s, long tick, long gameplayStartTick, int lane, CardRushTrack track)
        {
            long turns = System.Math.Max(0, tick - s.RollStartTick) / CardRushRules.CardDwellTicks;
            return track.CardOf(lane, s.UseCount, turns);
        }

        /// <summary>
        /// 그 틱에 머리 위에 있는 카드. 잡았으면 잡은 색(멈춘다), 돌고 있으면 도는 색. 안 돌고 안 잡았으면(평소·쿨다운) false(뒷면).
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
            //  돌기 시작 전 틱엔 카드가 없다(뒷면) — 지난 틱을 물어도 첫 장이 미리 보이지 않게.
            if (s.RollStartTick == 0 || tick < s.RollStartTick) return false;
            color = SpinColor(s, tick, gameplayStartTick, lane, track);
            return true;
        }

        /// <summary>잡든 쓰든 손을 쓸 수 있는 틱 — 출발 뒤, 완주 전, 기절 아님.</summary>
        public static bool CanAct(in CardRushRunnerState s, long tick, long gameplayStartTick)
            => tick > 0 && tick >= gameplayStartTick && s.FinishTick == 0 && tick >= s.StunEndTick;

        /// <summary>이 틱에 탭하면 카드가 돌기 시작하나 — 평소(안 돌고 안 잡음)이고 쿨다운이 끝났다.</summary>
        public static bool CanStart(in CardRushRunnerState s, long tick, long gameplayStartTick)
            => s.RollStartTick == 0 && s.LockTick == 0 && CanAct(s, tick, gameplayStartTick) && tick >= SpinBase(s, gameplayStartTick);

        /// <summary>이 틱에 탭하면 도는 카드를 잡나 — 돌고 있고 시작한 틱이 아니다(한 틱에 하나).</summary>
        public static bool CanLock(in CardRushRunnerState s, long tick, long gameplayStartTick)
            => s.RollStartTick != 0 && s.LockTick == 0 && tick > s.RollStartTick && CanAct(s, tick, gameplayStartTick);

        /// <summary>도는 카드가 이 틱에 자동으로 잡히나 — 돌기 시작하고 RollMaxTicks가 지났다. 기절 중에도 시간은 흐른다.</summary>
        public static bool RollExpired(in CardRushRunnerState s, long tick)
            => s.RollStartTick != 0 && s.LockTick == 0 && tick >= s.RollStartTick + CardRushRules.RollMaxTicks;

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
