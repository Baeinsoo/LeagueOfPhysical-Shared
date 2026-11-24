using System;
using System.Collections.Generic;
using System.Numerics;

namespace LOP.TriGrab
{
    public sealed class TriGrabPlayerScore
    {
        public int Score;
        public int Correct;
        public int Wrong;
        public long StunUntilTick;
        public long CooldownUntilTick;
    }

    public enum TriGrabFxKind { Correct, Wrong, MoleBonk, Range }

    /// <summary>클라 연출용 한 번짜리 사건. 틱마다 비운다.</summary>
    public sealed class TriGrabFx
    {
        public TriGrabFxKind Kind;
        public Vector2 Pos;
        public string Owner;
        /// <summary>MoleBonk일 때만: 두더지 대사 번호.</summary>
        public int LineIndex;
    }

    public sealed class TriGrabMatchState
    {
        public TriGrabPhase Phase = TriGrabPhase.Waiting;
        public long PhaseEndTick;
        public int Round;
        public int TotalRounds = TriGrabConfig.TotalRounds;
        public TriGrabRule Rule;
        public long LiveStartTick;
        public int Quota;
        /// <summary>이번 라운드에 지금까지 만든 맞는 카드 수.</summary>
        public int OkSpawned;
        /// <summary>Gone 포함. 라운드마다 비운다.</summary>
        public List<TriGrabCard> Cards = new List<TriGrabCard>();
        public List<TriGrabHook> Hooks = new List<TriGrabHook>();
        public List<TriGrabMole> Moles = new List<TriGrabMole>();
        public Dictionary<string, TriGrabPlayerScore> Players = new Dictionary<string, TriGrabPlayerScore>();
        public List<string> PlayerOrder = new List<string>();
        public HashSet<string> Inactive = new HashSet<string>();
        public List<TriGrabFx> Fx = new List<TriGrabFx>();
        /// <summary>정체가 바뀔 때만 오른다(위치만 변한 건 제외). 클라가 목록을 다시 받을지 가르는 표.</summary>
        public int Version;
    }

    /// <summary>시안(trigrab-ring3d-v9)의 nextRound·refill·update·collect를 한 판 진행으로 묶은 것. 난수는 하나(seed)로만 쓴다.</summary>
    public sealed class TriGrabMatch
    {
        const int MoleFirstDelayTicks = 30;
        const int SampleTries = 800;

        public readonly TriGrabMatchState State = new TriGrabMatchState();
        readonly Random _rng;
        readonly TriGrabMoleSpawner _moles = new TriGrabMoleSpawner();
        int _nextCardId;
        long _nextRefillTick;
        int _ringCount, _ringIndex;
        float _ringRadius, _ringRotation;
        TriGrabRule? _previousRule;

        public TriGrabMatch(int seed) { _rng = new Random(seed); }

        public List<TriGrabFx> Fx => State.Fx;

        public void AddPlayer(string id)
        {
            if (State.Players.ContainsKey(id)) return;
            State.Players[id] = new TriGrabPlayerScore();
            State.PlayerOrder.Add(id);
        }

        public void SetActive(string id, bool active)
        {
            if (active)
            {
                State.Inactive.Remove(id);
                return;
            }
            State.Inactive.Add(id);
            DropHookOf(id);
        }

        // 나간 사람의 손은 지우고, 끌던 카드는 점수 없이 사라진다(라운드 끝과 같은 처리) — 몸이 없어도 손이 점수를 내지 않게.
        void DropHookOf(string id)
        {
            var h = HookOf(id);
            if (h == null) return;
            State.Hooks.Remove(h);
            if (h.CardId >= 0)
            {
                for (int i = 0; i < State.Cards.Count; i++)
                {
                    var c = State.Cards[i];
                    if (c.Id != h.CardId || c.CarriedBy != id) continue;
                    c.CarriedBy = null;
                    c.Gone = true;
                }
            }
            State.Version++;
        }

        public void Start(long tick)
        {
            State.Round = 0;
            NextRound(tick);
        }

        public TriGrabHook HookOf(string id)
        {
            for (int i = 0; i < State.Hooks.Count; i++)
                if (State.Hooks[i].Owner == id) return State.Hooks[i];
            return null;
        }

        public bool TryFire(string id, Vector2 bodyPos, float yawDegrees, long tick)
        {
            if (State.Phase != TriGrabPhase.Live) return false;
            if (!State.Players.TryGetValue(id, out var p)) return false;
            if (State.Inactive.Contains(id)) return false;
            if (tick < p.StunUntilTick || tick < p.CooldownUntilTick) return false;
            if (HookOf(id) != null) return false;
            State.Hooks.Add(TriGrabHook.Fire(id, bodyPos, yawDegrees, tick));
            State.Version++;
            return true;
        }

        public void Tick(long tick, Func<string, Vector2> bodyPosOf)
        {
            State.Fx.Clear();
            switch (State.Phase)
            {
                case TriGrabPhase.Prep:
                    if (tick >= State.PhaseEndTick) StartLive(tick);
                    break;
                case TriGrabPhase.Live:
                    TickLive(tick, bodyPosOf);
                    break;
                case TriGrabPhase.Resolved:
                    if (tick >= State.PhaseEndTick) NextRound(tick);
                    break;
            }
        }

        void SetPhase(TriGrabPhase phase, long endTick)
        {
            State.Phase = phase;
            State.PhaseEndTick = endTick;
            State.Version++;
        }

        void NextRound(long tick)
        {
            State.Round++;
            State.Hooks.Clear();
            State.Cards.Clear();
            State.Moles.Clear();
            if (State.Round > State.TotalRounds)
            {
                State.Round = State.TotalRounds;
                SetPhase(TriGrabPhase.Over, tick);
                return;
            }
            State.Rule = TriGrabCards.PickRule(_rng, _previousRule);
            _previousRule = State.Rule;
            SetPhase(TriGrabPhase.Prep, tick + TriGrabConfig.AskTicks);
        }

        void StartLive(long tick)
        {
            State.LiveStartTick = tick;
            State.Cards.Clear();
            State.Moles.Clear();
            _moles.NextTick = tick + MoleFirstDelayTicks;
            State.Quota = TriGrabConfig.QuotaFor(State.PlayerOrder.Count);
            State.OkSpawned = 0;
            _nextRefillTick = tick;
            SetPhase(TriGrabPhase.Live, tick + TriGrabConfig.LiveLimitTicks);
            // 첫 묶음은 가운데 작은 고리에 같은 간격으로 놓는다(겹치지 않게 고리는 필요하면 조금 넓힌다).
            // 보충 규칙이 CardsOnDisc에서 멈추므로 첫 묶음도 그 수다.
            int batch = TriGrabConfig.CardsOnDisc;
            float need = TriGrabConfig.CardBumpRadius * 2f + 0.02f;
            _ringRadius = Math.Max(TriGrabConfig.CardSpawnRadius, need / (2f * (float)Math.Sin(Math.PI / batch)));
            _ringRotation = (float)(_rng.NextDouble() * Math.PI * 2);
            _ringCount = batch;
            _ringIndex = 0;
            for (int i = 0; i < batch && Refill(tick); i++) { }
            _ringCount = 0;
        }

        List<TriGrabCard> OnDisc()
        {
            var list = new List<TriGrabCard>();
            var cards = State.Cards;
            for (int i = 0; i < cards.Count; i++)
                if (!cards[i].Gone && cards[i].CarriedBy == null) list.Add(cards[i]);
            return list;
        }

        int VisibleOk() => State.PlayerOrder.Count <= 5 ? 2 : 3;

        bool Refill(long tick)
        {
            var rule = State.Rule;
            var d = OnDisc();
            int ok = 0, trap = 0;
            for (int i = 0; i < d.Count; i++)
            {
                if (d[i].Ok) ok++;
                else if (TriGrabCards.IsTrap(d[i].Word, d[i].Ink, d[i].Fill, rule)) trap++;
            }
            if (ok < VisibleOk() && State.OkSpawned < State.Quota)
            {
                if (AddCard(d, tick, true, false)) State.OkSpawned++;
                return true;
            }
            if (d.Count < TriGrabConfig.CardsOnDisc)
                return AddCard(d, tick, false, trap < 3);
            return false;
        }

        // wantOk면 맞는 카드, 아니면 틀린 카드(trapOnly면 그중 헷갈리는 것).
        bool AddCard(List<TriGrabCard> onDisc, long tick, bool wantOk, bool trapOnly)
        {
            var rule = State.Rule;
            (int word, int ink, int fill) c = default;
            bool found = false;
            for (int k = 0; k < SampleTries && !found; k++)
            {
                c = TriGrabCards.Make(rule, _rng);
                bool isOk = TriGrabCards.IsOk(c.word, c.ink, rule);
                if (isOk != wantOk) continue;
                if (trapOnly && !TriGrabCards.IsTrap(c.word, c.ink, c.fill, rule)) continue;
                found = true;
            }
            if (!found) return false;
            Vector2 spot;
            if (_ringCount > 0)
            {
                float a0 = _ringRotation + _ringIndex * (float)(Math.PI * 2 / _ringCount);
                _ringIndex++;
                spot = new Vector2((float)Math.Cos(a0), (float)Math.Sin(a0)) * _ringRadius;
            }
            else if (!TriGrabCardSim.TryCenterSpot(onDisc, _rng, out spot)) return false;
            var dir = TriGrabCardSim.OutwardDirection(spot, _rng);
            var card = new TriGrabCard
            {
                Id = _nextCardId++,
                Word = c.word,
                Ink = c.ink,
                Fill = c.fill,
                Ok = wantOk,
                BornTick = tick,
                Pos = spot,
                Vel = dir * TriGrabConfig.CardBurstSpeed,
            };
            State.Cards.Add(card);
            onDisc.Add(card);
            State.Version++;
            return true;
        }

        void TickLive(long tick, Func<string, Vector2> bodyPosOf)
        {
            float dt = TriGrabConfig.TickSeconds;

            int molesBefore = State.Moles.Count;
            int idBefore = _moles.NextId;
            _moles.Step(State.Moles, OnDisc(), tick, _rng);
            int spawned = _moles.NextId - idBefore;
            if (spawned > 0 || State.Moles.Count < molesBefore) State.Version++;

            if (tick >= _nextRefillTick && OnDisc().Count < TriGrabConfig.CardsOnDisc + 1 && Refill(tick))
                _nextRefillTick = tick + TriGrabConfig.RefillTicks;

            TriGrabCardSim.Step(OnDisc(), State.Moles, tick, dt, _rng);

            StepHooks(tick, dt, bodyPosOf);

            bool quotaDone = State.OkSpawned >= State.Quota && !AnyOkLeft();
            if (quotaDone || tick >= State.PhaseEndTick) EndLive(tick);
        }

        bool AnyOkLeft()
        {
            for (int i = 0; i < State.Cards.Count; i++)
                if (State.Cards[i].Ok && !State.Cards[i].Gone) return true;
            return false;
        }

        void StepHooks(long tick, float dt, Func<string, Vector2> bodyPosOf)
        {
            var byId = new Dictionary<int, TriGrabCard>();
            for (int i = 0; i < State.Cards.Count; i++) byId[State.Cards[i].Id] = State.Cards[i];

            var hooks = State.Hooks;
            for (int i = hooks.Count - 1; i >= 0; i--)
            {
                var h = hooks[i];
                if (h.State == TriGrabHookState.Out)
                {
                    TriGrabHook.StepOut(h, OnDisc(), State.Moles, tick, dt);
                    if (h.State == TriGrabHookState.Back)
                    {
                        State.Version++;
                        if (h.End == TriGrabHookEnd.Mole)
                        {
                            var mole = State.Moles.Find(m => m.Id == h.MoleId);
                            State.Fx.Add(new TriGrabFx { Kind = TriGrabFxKind.MoleBonk, Pos = mole != null ? mole.Pos : h.Tip, Owner = h.Owner, LineIndex = mole != null ? mole.BonkLineIndex : 0 });
                        }
                        else if (h.End == TriGrabHookEnd.Range)
                            State.Fx.Add(new TriGrabFx { Kind = TriGrabFxKind.Range, Pos = h.Tip, Owner = h.Owner });
                    }
                }
                else if (h.State == TriGrabHookState.Back)
                {
                    var body = bodyPosOf(h.Owner);
                    if (TriGrabHook.StepBack(h, body, byId, dt))
                        Finish(h, body, tick, byId);
                }
                if (h.State == TriGrabHookState.Done) hooks.RemoveAt(i);
            }
        }

        void Finish(TriGrabHook h, Vector2 body, long tick, Dictionary<int, TriGrabCard> byId)
        {
            var p = State.Players[h.Owner];
            p.CooldownUntilTick = tick + TriGrabConfig.HookCooldownTicks;
            State.Version++;
            if (h.CardId < 0 || !byId.TryGetValue(h.CardId, out var card)) return;
            card.Gone = true;
            card.CarriedBy = null;
            if (card.Ok)
            {
                p.Score++;
                p.Correct++;
                State.Fx.Add(new TriGrabFx { Kind = TriGrabFxKind.Correct, Pos = body, Owner = h.Owner });
            }
            else
            {
                p.Score--;
                p.Wrong++;
                p.StunUntilTick = tick + TriGrabConfig.StunTicks;
                State.Fx.Add(new TriGrabFx { Kind = TriGrabFxKind.Wrong, Pos = body, Owner = h.Owner });
            }
        }

        // 라운드가 끝나면 남은 손과 끌려가던 카드는 점수 없이 사라진다.
        void EndLive(long tick)
        {
            State.Hooks.Clear();
            for (int i = 0; i < State.Cards.Count; i++)
                if (State.Cards[i].CarriedBy != null)
                {
                    State.Cards[i].CarriedBy = null;
                    State.Cards[i].Gone = true;
                }
            SetPhase(TriGrabPhase.Resolved, tick + TriGrabConfig.ResolvedTicks);
        }

        /// <summary>점수 높은 순. 같은 점수는 같은 등수(3,3,1 → 1,1,3).</summary>
        public IReadOnlyList<(string id, int place)> Placements()
        {
            var list = new List<(string id, int place)>();
            foreach (var id in State.PlayerOrder)
            {
                int s = State.Players[id].Score, better = 0;
                foreach (var o in State.PlayerOrder)
                    if (State.Players[o].Score > s) better++;
                list.Add((id, better + 1));
            }
            list.Sort((a, b) => a.place.CompareTo(b.place));
            return list;
        }
    }
}
