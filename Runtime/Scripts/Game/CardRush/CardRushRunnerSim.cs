namespace LOP
{
    /// <summary>
    /// 선수 한 명의 한 틱(4차 spec §3.3) — 탭이 없어도 매 틱, 잡은 카드 사라짐 → 탭(잡기/쓰기) → 바리케이드 올라감 → 이동 → 투사체
    /// → 막힘 → 구덩이 → 굴러오는 것 → 구역 → 완주 순서.
    /// 그 선수 상태 + 코스 + 그 틱 탭만 본다(남과 얽히지 않는다). 정수만 써서 서버와 내 예측이 같은 틱에 같은 상태를 낸다.
    /// </summary>
    public static class CardRushRunnerSim
    {
        public static void Step(ref CardRushRunnerState s, long tick, bool tapped, long g, int lane, CardRushTrack track, in CardRushTuning t)
        {
            //  0은 "없음"이라 틱 0엔 아무것도 하지 않는다(실제 판의 출발 틱은 1보다 크다).
            if (track == null || tick <= 0 || tick < g || s.FinishTick != 0) return;

            if (CardRushCard.LockExpired(s, tick))
            {
                s.ExpireTick = tick;
                ReleaseCard(ref s, tick);
            }
            if (tapped)
            {
                if (CardRushCard.CanLock(s, tick, g))
                {
                    s.LockTick = tick;
                }
                else if (CardRushCard.CanUse(s, tick, g, lane, track))
                {
                    UseCard(ref s, tick, CardRushCard.SpinColor(s, s.LockTick, g, lane, track));
                    ReleaseCard(ref s, tick);
                }
            }
            LiftBarricade(ref s, tick, track, t);
            Move(ref s, tick);
            Shot(ref s, tick, track);
            StopAtBarricade(ref s, tick, track);
            Pit(ref s, tick, track, t);
            Rollers(ref s, tick, track, t);
            Zones(ref s, tick, track, t);
            if (s.PosMm >= track.TrackMm)
            {
                s.PosMm = track.TrackMm;
                s.FinishTick = tick;
                //  완주 뒤엔 Step이 멈춰 투사체를 끌 데가 없다 — 날던 것은 여기서 지운다.
                s.ShotTick = 0;
                s.ShotFromMm = 0;
            }
        }

        /// <summary>
        /// 지금 장애물의 앞면 자리 — 투사체가 맞히는 곳. 바리케이드 = 그 자리, 굴러오는 것 = 굴러오는 중이면 지금 자리·아직 안 풀렸으면 그 자리
        /// (서 있는 것도 맞힌다), 구덩이 = 없음(투사체가 위로 지나간다).
        /// </summary>
        public static bool TryFront(in CardRushRunnerState s, long tick, CardRushTrack track, out int front)
        {
            front = 0;
            if (track == null || s.ObstacleIndex >= track.Obstacles.Count) return false;
            var o = track.Obstacles[s.ObstacleIndex];
            switch (o.Kind)
            {
                case CardRushObstacleKind.Barricade:
                    front = o.AtMm;
                    return true;
                case CardRushObstacleKind.Roller:
                    front = s.ObstacleTick == 0 ? o.AtMm : RollerPos(o, s.ObstacleTick, tick);
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>굴러오는 것의 자리 — 풀린 틱부터 틱마다 RollerMmPerTick씩 내 쪽으로.</summary>
        public static int RollerPos(in CardRushObstacle o, long revealTick, long tick)
            => (int)(o.AtMm - (long)CardRushRules.RollerMmPerTick * (tick - revealTick));

        /// <summary>날고 있는 투사체의 자리(mm) — 던진 틱엔 출발 자리, 틱마다 ShotMmPerTick씩 앞으로. 날고 있지 않으면(ShotTick 0) 의미 없다.</summary>
        public static int ShotPos(in CardRushRunnerState s, long tick)
            => (int)(s.ShotFromMm + (long)CardRushRules.ShotMmPerTick * (tick - s.ShotTick));

        public static bool IsAirborne(in CardRushRunnerState s) => s.JumpStartTick != 0;

        //  쓴 뒤·사라진 뒤 같은 쿨다운, 같은 다음 첫 장 규칙.
        private static void ReleaseCard(ref CardRushRunnerState s, long tick)
        {
            s.LockTick = 0;
            s.UseCount += 1;
            s.SpinStartTick = tick + CardRushRules.CooldownTicks;
        }

        private static void UseCard(ref CardRushRunnerState s, long tick, CardColor card)
        {
            switch (card)
            {
                case CardColor.Red:
                    //  날고 있던 투사체가 있으면 새것으로 바꾼다.
                    s.ShotTick = tick;
                    s.ShotFromMm = s.PosMm + CardRushRules.ShotStartAheadMm;
                    break;
                case CardColor.Yellow:
                    s.JumpStartTick = tick;
                    s.DashStartTick = 0;
                    break;
                case CardColor.Blue:
                    s.DashStartTick = tick;
                    break;
            }
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
            if (s.JumpStartTick != 0)
            {
                int k = (int)(tick - s.JumpStartTick + 1);
                s.PosMm += CardRushRules.JumpMmPerTick;
                //  마지막 틱에 내려앉는다 — 같은 틱의 구덩이 판정은 땅으로 본다.
                if (k >= CardRushRules.JumpTicks) s.JumpStartTick = 0;
            }
            else if (s.DashStartTick != 0)
            {
                int k = (int)(tick - s.DashStartTick + 1);
                s.PosMm += CardRushRules.DashOffset(k) - CardRushRules.DashOffset(k - 1);
                if (k >= CardRushRules.DashTicks) s.DashStartTick = 0;
            }
            else if (tick >= s.StunEndTick)
            {
                s.PosMm += CardRushRules.RunMmPerTick;
            }
        }

        private static void Shot(ref CardRushRunnerState s, long tick, CardRushTrack track)
        {
            if (s.ShotTick == 0) return;
            int sp = ShotPos(s, tick);
            //  "앞면 ≤ 투사체"로 본다 — 한 틱에 300 mm를 가도 지나친 장애물을 놓치지 않는다.
            if (TryFront(s, tick, track, out int front) && front <= sp)
            {
                s.ShotTick = 0;
                Clear(ref s, tick, CardRushObstacleEvent.Smashed);
            }
            else if (sp - s.ShotFromMm >= CardRushRules.ShotRangeMm)
            {
                s.ShotTick = 0;
                s.WhiffTick = tick;
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
                //  바리케이드는 못 넘는다 — 부딪혀 내려앉는다.
                s.JumpStartTick = 0;
                if (s.ObstacleTick == 0) s.ObstacleTick = tick;
            }
        }

        private static void Pit(ref CardRushRunnerState s, long tick, CardRushTrack track, in CardRushTuning t)
        {
            if (s.ObstacleIndex >= track.Obstacles.Count) return;
            var o = track.Obstacles[s.ObstacleIndex];
            if (o.Kind != CardRushObstacleKind.Pit) return;
            int far = o.AtMm + o.WidthMm;
            if (s.JumpStartTick == 0 && s.PosMm >= o.AtMm && s.PosMm <= far)
            {
                //  빠짐 — 번호는 그대로라 다시 넘어야 한다.
                s.ObstacleEvent = (int)CardRushObstacleEvent.Fell;
                s.ObstacleEventTick = tick;
                s.PosMm = System.Math.Max(0, o.AtMm - CardRushRules.PitBackMm);
                s.StunEndTick = tick + t.PitStunTicks;
                s.DashStartTick = 0;
                Tally(ref s, hit: true);
            }
            else if (s.PosMm > far)
            {
                Clear(ref s, tick, CardRushObstacleEvent.Jumped);
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
            if (s.JumpStartTick != 0)
            {
                Clear(ref s, tick, CardRushObstacleEvent.Jumped);
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

            //  터지는 틱에 한 번만 본다.
            bool inside = s.PosMm >= z.FromMm - CardRushRules.ZoneMarginMm && s.PosMm <= z.ToMm + CardRushRules.ZoneMarginMm;
            var result = inside ? CardRushZoneEvent.Hit : CardRushZoneEvent.Dodged;
            if (inside) Knock(ref s, tick, t.ZoneKnockMm, t.ZoneStunTicks);

            s.ZoneIndex += 1;
            s.ZoneTick = 0;
            s.ZoneEvent = (int)result;
            s.ZoneEventTick = tick;
            Tally(ref s, inside);
        }

        private static void Knock(ref CardRushRunnerState s, long tick, int knockMm, int stunTicks)
        {
            s.PosMm = System.Math.Max(0, s.PosMm - knockMm);
            s.StunEndTick = tick + stunTicks;
            s.DashStartTick = 0;
            //  밀려나면 내려앉는다 — 공중에서 구역에 맞아도 기절한 채 앞으로 날아가지 않게.
            s.JumpStartTick = 0;
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
