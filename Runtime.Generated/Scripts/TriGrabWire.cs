namespace LOP.TriGrab
{
    /// <summary>판 상태 ↔ 와이어 메시지. 클라 쪽 역변환은 클라가 한다.</summary>
    public static class TriGrabWire
    {
        /// <param name="tick">연출 사건에 찍을 지금 틱.</param>
        public static TriGrabRingStateToC ToWire(TriGrabMatch m, long tick = 0)
        {
            var s = m.State;
            var msg = new TriGrabRingStateToC
            {
                Version = s.Version,
                Phase = (int)s.Phase,
                PhaseEndTick = s.PhaseEndTick,
                Round = s.Round,
                TotalRounds = s.TotalRounds,
                RuleType = (int)s.Rule.Type,
                RuleValue = s.Rule.Value,
                RuleLure = s.Rule.Lure,
                LiveStartTick = s.LiveStartTick,
                Quota = s.Quota,
                OkSpawned = s.OkSpawned,
            };

            foreach (var c in s.Cards)
            {
                msg.Cards.Add(new TriGrabRingCardWire
                {
                    Id = c.Id, Word = c.Word, Ink = c.Ink, Fill = c.Fill, Ok = c.Ok,
                    BornTick = c.BornTick, CarriedBy = c.CarriedBy ?? "", Gone = c.Gone,
                });
            }

            foreach (var h in s.Hooks)
            {
                msg.Hooks.Add(new TriGrabRingHookWire
                {
                    OwnerId = h.Owner, FireTick = h.FireTick,
                    OriginX = h.Origin.X, OriginZ = h.Origin.Y,
                    DirX = h.Dir.X, DirZ = h.Dir.Y,
                    State = (int)h.State, End = (int)h.End, CardId = h.CardId,
                    TurnTick = h.TurnTick, TurnLength = h.TurnLength,
                    TipX = h.Tip.X, TipZ = h.Tip.Y,
                });
            }

            foreach (var mole in s.Moles)
            {
                msg.Moles.Add(new TriGrabRingMoleWire
                {
                    Id = mole.Id, X = mole.Pos.X, Z = mole.Pos.Y,
                    SpawnTick = mole.SpawnTick, StandTicks = mole.StandTicks, BonkTick = mole.BonkTick,
                    Say = mole.SayIndex, BonkLine = mole.BonkLineIndex,
                });
            }

            var places = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var (id, place) in m.Placements()) places[id] = place;
            foreach (var id in s.PlayerOrder)
            {
                var p = s.Players[id];
                msg.Players.Add(new TriGrabRingPlayerWire
                {
                    EntityId = id, Score = p.Score, Correct = p.Correct, Wrong = p.Wrong,
                    StunUntilTick = p.StunUntilTick, CooldownUntilTick = p.CooldownUntilTick,
                    Place = places[id],
                });
            }

            foreach (var f in s.Fx)
            {
                msg.Fx.Add(new TriGrabRingFxWire
                {
                    Kind = (int)f.Kind, X = f.Pos.X, Z = f.Pos.Y,
                    OwnerId = f.Owner ?? "", Line = f.LineIndex, Tick = tick,
                });
            }
            return msg;
        }

        /// <summary>원판 위 카드와 끌려가는 카드의 자리. 사라진(Gone) 카드는 뺀다.</summary>
        public static TriGrabCardsToC CardsToWire(TriGrabMatchState s, long tick)
        {
            var msg = new TriGrabCardsToC { Tick = tick };
            foreach (var c in s.Cards)
            {
                if (c.Gone) continue;
                msg.Ids.Add(c.Id);
                msg.Xs.Add(c.Pos.X);
                msg.Zs.Add(c.Pos.Y);
            }
            return msg;
        }
    }
}
