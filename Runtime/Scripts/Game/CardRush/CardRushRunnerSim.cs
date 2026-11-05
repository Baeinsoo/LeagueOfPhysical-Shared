namespace LOP
{
    /// <summary>
    /// 선수 한 명의 한 틱(spec §3.5) — 탭이 없어도 매 틱, 카드 쓰기 → 바리케이드 올라감 → 이동 → 막힘 → 굴러오는 것 → 구역 → 완주 순서.
    /// 그 선수 상태 + 코스 + 그 틱 탭만 본다(남과 얽히지 않는다). 정수만 써서 서버와 내 예측이 같은 틱에 같은 상태를 낸다.
    /// </summary>
    public static class CardRushRunnerSim
    {
        public static void Step(ref CardRushRunnerState s, long tick, bool tapped, long g, int lane, CardRushTrack track, in CardRushTuning t)
        {
            //  0은 "없음"이라 틱 0엔 아무것도 하지 않는다(실제 판의 출발 틱은 1보다 크다).
            if (track == null || tick <= 0 || tick < g || s.FinishTick != 0) return;

            if (tapped && CardRushCard.CanUse(s, tick, g) && CardRushCard.TryCardAt(s, tick, g, lane, track, out var card))
                UseCard(ref s, tick, card, track);
            LiftBarricade(ref s, tick, track, t);
            Move(ref s, tick);
            StopAtBarricade(ref s, tick, track);
            Rollers(ref s, tick, track, t);
            Zones(ref s, tick, track, t);
            if (s.PosMm >= track.TrackMm)
            {
                s.PosMm = track.TrackMm;
                s.FinishTick = tick;
            }
        }

        /// <summary>
        /// 지금 장애물의 앞면 자리. 바리케이드 = 그 자리, 굴러오는 것 = 굴러오는 중이면 지금 자리, 아직 안 풀렸으면 없음(멀어 손이 안 닿는다).
        /// </summary>
        public static bool TryFront(in CardRushRunnerState s, long tick, CardRushTrack track, out int front)
        {
            front = 0;
            if (track == null || s.ObstacleIndex >= track.Obstacles.Count) return false;
            var o = track.Obstacles[s.ObstacleIndex];
            if (o.Kind == CardRushObstacleKind.Barricade)
            {
                front = o.AtMm;
                return true;
            }
            if (s.ObstacleTick == 0) return false;
            front = RollerPos(o, s.ObstacleTick, tick);
            return true;
        }

        /// <summary>굴러오는 것의 자리 — 풀린 틱부터 틱마다 RollerMmPerTick씩 내 쪽으로.</summary>
        public static int RollerPos(in CardRushObstacle o, long revealTick, long tick)
            => (int)(o.AtMm - (long)CardRushRules.RollerMmPerTick * (tick - revealTick));

        private static void UseCard(ref CardRushRunnerState s, long tick, CardColor card, CardRushTrack track)
        {
            switch (card)
            {
                case CardColor.Red:
                    if (TryFront(s, tick, track, out int front)
                        && front - s.PosMm > -CardRushRules.SmashBackMm && front - s.PosMm <= CardRushRules.SmashReachMm)
                    {
                        Clear(ref s, tick, CardRushObstacleEvent.Smashed);
                    }
                    else
                    {
                        //  헛방 — 장애물 번호는 그대로, 그림·소리만 안다.
                        s.ObstacleEvent = (int)CardRushObstacleEvent.Whiff;
                        s.ObstacleEventTick = tick;
                    }
                    break;
                case CardColor.Yellow:
                    s.BlockEndTick = tick + CardRushRules.BlockTicks;
                    break;
                case CardColor.Blue:
                    s.DashStartTick = tick;
                    break;
            }
            s.UseCount += 1;
            s.SpinStartTick = tick + CardRushRules.CooldownTicks;
        }

        private static void LiftBarricade(ref CardRushRunnerState s, long tick, CardRushTrack track, in CardRushTuning t)
        {
            if (s.ObstacleIndex >= track.Obstacles.Count) return;
            if (track.Obstacles[s.ObstacleIndex].Kind != CardRushObstacleKind.Barricade) return;
            if (s.ObstacleTick != 0 && tick - s.ObstacleTick >= t.BarricadeWaitTicks)
                Clear(ref s, tick, CardRushObstacleEvent.Lifted);
        }

        private static void Move(ref CardRushRunnerState s, long tick)
        {
            if (s.DashStartTick != 0)
            {
                //  방패 중에도 대시는 미끄러진다(시안 그대로).
                int k = (int)(tick - s.DashStartTick + 1);
                s.PosMm += CardRushRules.DashOffset(k) - CardRushRules.DashOffset(k - 1);
                if (k >= CardRushRules.DashTicks) s.DashStartTick = 0;
            }
            else if (tick >= s.StunEndTick && tick >= s.BlockEndTick)
            {
                s.PosMm += CardRushRules.RunMmPerTick;
            }
        }

        private static void StopAtBarricade(ref CardRushRunnerState s, long tick, CardRushTrack track)
        {
            if (s.ObstacleIndex >= track.Obstacles.Count) return;
            var o = track.Obstacles[s.ObstacleIndex];
            if (o.Kind != CardRushObstacleKind.Barricade) return;
            int stop = o.AtMm - CardRushRules.BarricadeStopMm;
            if (s.PosMm > stop)
            {
                s.PosMm = stop;
                s.DashStartTick = 0;
                if (s.ObstacleTick == 0) s.ObstacleTick = tick;
            }
        }

        private static void Rollers(ref CardRushRunnerState s, long tick, CardRushTrack track, in CardRushTuning t)
        {
            if (s.ObstacleIndex >= track.Obstacles.Count) return;
            var o = track.Obstacles[s.ObstacleIndex];
            if (o.Kind != CardRushObstacleKind.Roller) return;
            if (s.ObstacleTick == 0 && s.PosMm >= o.AtMm - CardRushRules.RollerRevealMm) s.ObstacleTick = tick;
            if (s.ObstacleTick == 0) return;

            //  음수(이미 지나침)도 접촉이다 — 대시로 건너뛸 수 없다.
            if (RollerPos(o, s.ObstacleTick, tick) - s.PosMm > CardRushRules.RollerContactMm) return;
            if (tick < s.BlockEndTick)
            {
                Clear(ref s, tick, CardRushObstacleEvent.Blocked);
                return;
            }
            Clear(ref s, tick, CardRushObstacleEvent.Tumbled);
            Knock(ref s, tick, t.RollKnockMm, t.RollStunTicks);
        }

        private static void Zones(ref CardRushRunnerState s, long tick, CardRushTrack track, in CardRushTuning t)
        {
            if (s.ZoneIndex >= track.Zones.Count) return;
            var z = track.Zones[s.ZoneIndex];
            if (s.ZoneTick == 0 && s.PosMm >= z.TriggerMm) s.ZoneTick = tick;
            if (s.ZoneTick == 0 || tick < s.ZoneTick + z.WarnTicks) return;

            //  터지는 틱에 한 번만 본다. 방패는 그 틱에 켜져 있어야 한다.
            CardRushZoneEvent result;
            bool inside = s.PosMm >= z.FromMm - CardRushRules.ZoneMarginMm && s.PosMm <= z.ToMm + CardRushRules.ZoneMarginMm;
            if (!inside) result = CardRushZoneEvent.Dodged;
            else if (tick < s.BlockEndTick) result = CardRushZoneEvent.Held;
            else
            {
                result = CardRushZoneEvent.Hit;
                Knock(ref s, tick, t.ZoneKnockMm, t.ZoneStunTicks);
            }

            s.ZoneIndex += 1;
            s.ZoneTick = 0;
            s.ZoneEvent = (int)result;
            s.ZoneEventTick = tick;
            Tally(ref s, result == CardRushZoneEvent.Hit);
        }

        private static void Knock(ref CardRushRunnerState s, long tick, int knockMm, int stunTicks)
        {
            s.PosMm = System.Math.Max(0, s.PosMm - knockMm);
            s.StunEndTick = tick + stunTicks;
            s.DashStartTick = 0;
        }

        private static void Clear(ref CardRushRunnerState s, long tick, CardRushObstacleEvent kind)
        {
            s.ObstacleIndex += 1;
            s.ObstacleTick = 0;
            s.ObstacleEvent = (int)kind;
            s.ObstacleEventTick = tick;
            Tally(ref s, kind == CardRushObstacleEvent.Tumbled);
        }

        private static void Tally(ref CardRushRunnerState s, bool hit)
        {
            if (hit)
            {
                s.Hits += 1;
                s.CleanStreak = 0;
                return;
            }
            s.CleanStreak += 1;
            if (s.CleanStreak > s.BestCleanStreak) s.BestCleanStreak = s.CleanStreak;
        }
    }
}
