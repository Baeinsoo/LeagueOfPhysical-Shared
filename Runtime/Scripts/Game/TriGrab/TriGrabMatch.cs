using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>판 상태 전체. 서버가 이 값으로 와이어를 만든다. 보이는 것이 바뀔 때만 Version이 오른다(갈고리가 뻗는 것은 식이라 안 올린다).</summary>
    public sealed class TriGrabMatchState
    {
        public TriGrabPhase Phase = TriGrabPhase.Waiting;
        public long PhaseEndTick;
        public int Round;
        public readonly int TotalRounds = TriGrabConfig.TotalRounds;
        /// <summary>이번 라운드 Live가 끝나는 틱(시간 상한). 그 전에 남은 사람이 충분히 줄면 먼저 끝난다.</summary>
        public long RoundLimitTick;
        /// <summary>이번 라운드에 정답을 낸 사람 수.</summary>
        public int RoundCorrectCount;
        public TriGrabRule Rule;
        public TriGrabRoundClock Clock;
        public readonly TriGrabSlot[] Slots = new TriGrabSlot[TriGrabConfig.Slots];
        public readonly List<TriGrabHook> Hooks = new List<TriGrabHook>();
        public readonly Dictionary<string, TriGrabPlayerScore> Players = new Dictionary<string, TriGrabPlayerScore>();
        /// <summary>등록한 순서. 와이어·점수판이 같은 순서를 쓴다.</summary>
        public readonly List<string> PlayerOrder = new List<string>();
        /// <summary>지금 판에 없는(연결이 끊긴) 사람. 서버만 채운다. 정답을 낼 수 없으니 라운드 끝 수에서 "끝난 쪽"으로 센다.</summary>
        public readonly HashSet<string> Inactive = new HashSet<string>();
        /// <summary>이번 라운드 1등.</summary>
        public string RoundWinnerId = "";
        public string MatchWinnerId = "";
        public long LastValidTick;
        public int Version;

        public TriGrabMatchState()
        {
            for (int i = 0; i < Slots.Length; i++) Slots[i] = new TriGrabSlot();
        }
    }

    /// <summary>삼색 그랩 판 규칙. 서버가 틱마다 부르고, 클라 에디터 시험도 같은 코드를 돌린다.</summary>
    public sealed class TriGrabMatch
    {
        readonly System.Random _rng;
        readonly Vector2[] _slotPos = new Vector2[TriGrabConfig.Slots];
        readonly bool[] _occupied = new bool[TriGrabConfig.Slots];
        int _nextCardId = 1;
        long _startTick;

        public TriGrabMatchState State { get; } = new TriGrabMatchState();

        public TriGrabMatch(int seed) { _rng = new System.Random(seed); }

        public void AddPlayer(string id)
        {
            if (State.Players.ContainsKey(id)) return;
            State.Players[id] = new TriGrabPlayerScore();
            State.PlayerOrder.Add(id);
            State.Version++;
        }

        /// <summary>서버가 연결 상태를 알린다. 봇은 늘 활성이다.</summary>
        public void SetActive(string id, bool active)
        {
            if (!State.Players.ContainsKey(id)) return;
            if (active) State.Inactive.Remove(id); else State.Inactive.Add(id);
        }

        public TriGrabHook HookOf(string id)
        {
            foreach (var h in State.Hooks) if (h.OwnerId == id) return h;
            return null;
        }

        public void Start(long tick)
        {
            _startTick = tick;
            StartRound(tick);
        }

        public bool TryFire(string id, Vector2 bodyPos, float yawDegrees, long tick)
        {
            var s = State;
            if (s.Phase != TriGrabPhase.Live) return false;
            if (!s.Players.TryGetValue(id, out var p)) return false;
            if (p.RoundPlace > 0 || s.Inactive.Contains(id)) return false;
            if (HookOf(id) != null || tick < p.ReloadUntilTick) return false;

            var hook = new TriGrabHook
            {
                OwnerId = id,
                FireTick = tick,
                Origin = bodyPos,
                Dir = TriGrabMath.DirFromYaw(yawDegrees),
                TurnTick = tick + TriGrabConfig.ReachTicks,
                TurnLength = TriGrabConfig.HookLength,
            };
            hook.EndTick = TriGrabHook.ComputeEndTick(hook.TurnTick, hook.TurnLength, false);
            PickTarget(hook);
            s.Hooks.Add(hook);
            // 일단 빗나간다고 보고 재장전을 잡는다. 카드를 잡으면 아래에서 고친다.
            p.ReloadUntilTick = hook.EndTick + TriGrabConfig.ReloadMissTicks;
            s.Version++;
            return true;
        }

        public void Tick(long t)
        {
            var s = State;
            if (s.Phase == TriGrabPhase.Waiting || s.Phase == TriGrabPhase.Over) return;

            if (t - _startTick >= TriGrabConfig.MatchCapTicks)
            {
                FinishMatch();
                return;
            }

            if (s.Phase == TriGrabPhase.Prep && t >= s.PhaseEndTick)
            {
                s.Phase = TriGrabPhase.Live;
                s.LastValidTick = t;
                s.Version++;
            }
            if (s.Phase == TriGrabPhase.Resolved && t >= s.PhaseEndTick)
            {
                if (s.Round >= s.TotalRounds) FinishMatch();
                else StartRound(t);
                return;
            }
            if (s.Phase == TriGrabPhase.Live && t >= s.RoundLimitTick) EndRound(t);

            if (s.Phase == TriGrabPhase.Live) TickLive(t);
            TickHooks(t);
            if (s.Phase == TriGrabPhase.Live && RoundDone()) EndRound(t);
            RemoveFinishedHooks(t);
        }

        void TickLive(long t)
        {
            var s = State;
            for (int i = 0; i < s.Slots.Length; i++)
            {
                var slot = s.Slots[i];
                if (!slot.Card.HasValue && t >= slot.EmptyUntilTick)
                {
                    slot.Card = MakeCard(_rng.NextDouble() < TriGrabConfig.ValidProbability, t, true);
                    slot.ValidSinceTick = -1;
                    s.Version++;
                }
            }

            bool anyValid = false;
            foreach (var slot in s.Slots)
            {
                if (!slot.Card.HasValue) continue;
                var c = slot.Card.Value;
                if (s.Rule.Matches(c, t))
                {
                    if (slot.ValidSinceTick < 0) slot.ValidSinceTick = t;
                    anyValid = true;
                }
                else slot.ValidSinceTick = -1;
            }

            if (anyValid) s.LastValidTick = t;
            else if (t - s.LastValidTick >= TriGrabConfig.ForceValidAfterTicks)
            {
                var filled = new List<int>();
                for (int i = 0; i < s.Slots.Length; i++) if (s.Slots[i].Card.HasValue) filled.Add(i);
                if (filled.Count > 0)
                {
                    int idx = filled[_rng.Next(filled.Count)];
                    s.Slots[idx].Card = MakeCard(true, t, false);
                    s.Slots[idx].ValidSinceTick = t;
                    s.LastValidTick = t;
                    s.Version++;
                }
            }
        }

        void TickHooks(long t)
        {
            var s = State;
            if (s.Hooks.Count == 0) return;

            // 같은 틱에 둘이 닿으면 누가 먼저인지가 공정하도록 순서를 섞는다.
            var order = new List<TriGrabHook>(s.Hooks);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            // 이번 틱에 닿은 갈고리는 모두 판정한다(같은 틱 두 번째 정답도 점수를 받는다). 라운드 끝은 그 뒤에 센다.
            if (s.Phase != TriGrabPhase.Live) return;
            foreach (var hook in order)
            {
                if (hook.Result != TriGrabHookResult.None || hook.Carried.HasValue) continue;
                if (t <= hook.FireTick || t > hook.TurnTick) continue;
                TryGrab(hook, t);
            }
        }

        // 쏜 순간의 회전판·카드로 갈고리를 앞으로 굴려, 처음 닿을 카드를 정해 둔다(그 사이 아무것도 빠지지 않는다고 본다).
        void PickTarget(TriGrabHook hook)
        {
            var s = State;
            for (long k = hook.FireTick + 1; k <= hook.TurnTick; k++)
            {
                int hit = HitAt(hook, k);
                if (hit < 0) continue;
                hook.TargetSlot = hit;
                hook.TargetCardId = s.Slots[hit].Card.Value.Id;
                return;
            }
        }

        int HitAt(TriGrabHook hook, long t)
        {
            var s = State;
            float baseAngle = s.Clock.BaseAt(t);
            for (int i = 0; i < s.Slots.Length; i++)
            {
                _slotPos[i] = TriGrabCarousel.SlotPosition(i, baseAngle);
                _occupied[i] = s.Slots[i].Card.HasValue;
            }
            return TriGrabMath.PickHit(hook.TipAt(t), hook.Origin, _slotPos, _occupied);
        }

        void TryGrab(TriGrabHook hook, long t)
        {
            var s = State;
            if (hook.TargetCardId == 0) return;   // 닿을 카드가 없었다 — 빗나감
            var target = s.Slots[hook.TargetSlot];
            if (!target.Card.HasValue || target.Card.Value.Id != hook.TargetCardId)
            {
                // 겨눈 카드를 남이 먼저 가져갔다(또는 바뀌었다). 옆 카드를 집지 않고 지금 자리에서 빈손으로 돌아온다.
                float len = hook.LengthAt(t);
                hook.TargetCardId = 0;
                hook.TurnTick = t;
                hook.TurnLength = len;
                hook.EndTick = TriGrabHook.ComputeEndTick(t, len, false);
                s.Players[hook.OwnerId].ReloadUntilTick = hook.EndTick + TriGrabConfig.ReloadMissTicks;
                s.Version++;
                return;
            }
            Vector2 cardPos = TriGrabCarousel.SlotPosition(hook.TargetSlot, s.Clock.BaseAt(t));
            if ((cardPos - hook.TipAt(t)).magnitude >= TriGrabConfig.HitRadius) return;
            int hit = hook.TargetSlot;

            var slot = s.Slots[hit];
            var card = slot.Card.Value;
            bool ok = s.Rule.Matches(card, t);
            long validSince = slot.ValidSinceTick;
            slot.Card = null;
            slot.EmptyUntilTick = t + TriGrabConfig.RefillTicks;
            slot.ValidSinceTick = -1;

            float lengthNow = hook.LengthAt(t);
            card.Ink = card.InkAt(t);
            card.Cycles = false;
            hook.Carried = card;
            hook.Result = ok ? TriGrabHookResult.Correct : TriGrabHookResult.Wrong;
            hook.TurnTick = t;
            hook.TurnLength = lengthNow;
            hook.EndTick = TriGrabHook.ComputeEndTick(t, lengthNow, true);

            var p = s.Players[hook.OwnerId];
            if (ok)
            {
                s.RoundCorrectCount++;
                p.RoundPlace = s.RoundCorrectCount;
                p.RoundPoints = TriGrabConfig.PlacePoints(p.RoundPlace);
                p.Score += p.RoundPoints;
                p.ReloadUntilTick = hook.EndTick;
                int rtMs = (int)((t - (validSince >= 0 ? validSince : t)) * 20);
                if (p.BestRtMs == 0 || rtMs < p.BestRtMs) p.BestRtMs = rtMs;
                if (p.RoundPlace == 1) s.RoundWinnerId = hook.OwnerId;
            }
            else
            {
                p.Score = Mathf.Max(0, p.Score - 1);
                p.Mistakes++;
                p.ReloadUntilTick = hook.EndTick + TriGrabConfig.ReloadWrongTicks;
            }
            s.Version++;
        }

        void RemoveFinishedHooks(long t)
        {
            int removed = State.Hooks.RemoveAll(h => h.EndTick < t);
            if (removed > 0) State.Version++;
        }

        /// <summary>
        /// 라운드가 끝났나. 아직 정답을 낼 수 있는 사람(정답 전이고 판에 있는 사람)이 max(1, 인원/4) 이하로 남으면 끝.
        /// 인원은 등록된 모두(나간 사람 포함) — 나간 사람은 정답을 낼 수 없으니 "끝난 쪽"으로 센다. 낼 수 있는 사람이 없어도 끝.
        /// </summary>
        bool RoundDone()
        {
            var s = State;
            int canStillScore = 0;
            foreach (var kv in s.Players)
                if (kv.Value.RoundPlace == 0 && !s.Inactive.Contains(kv.Key)) canStillScore++;
            if (canStillScore == 0) return true;
            // 아무도 안 끝났으면(1인 판의 시작 등) 정원만으로 끝내지 않는다.
            return canStillScore < s.Players.Count && canStillScore <= TriGrabConfig.RoundEndRemaining(s.Players.Count);
        }

        void EndRound(long t)
        {
            var s = State;
            s.Phase = TriGrabPhase.Resolved;
            s.PhaseEndTick = t + TriGrabConfig.ResolvedTicks;
            s.Clock.FreezeTick = t;
            s.Version++;
        }

        void FinishMatch()
        {
            var s = State;
            int best = -1;
            string winner = "";
            bool tie = false;
            foreach (var id in s.PlayerOrder)
            {
                int sc = s.Players[id].Score;
                if (sc > best) { best = sc; winner = id; tie = false; }
                else if (sc == best) tie = true;
            }
            s.MatchWinnerId = tie ? "" : winner;
            s.Phase = TriGrabPhase.Over;
            s.Version++;
        }

        void StartRound(long t)
        {
            var s = State;
            float prevBase = s.Clock.BaseAt(s.Clock.FreezeTick);
            s.Round++;

            TriGrabRule rule;
            do
            {
                rule = new TriGrabRule((TriGrabRuleType)_rng.Next(3), _rng.Next(3));
            } while (s.Round > 1 && rule.Type == s.Rule.Type && (rule.Type == TriGrabRuleType.Match || rule.Color == s.Rule.Color));
            s.Rule = rule;

            long liveStart = t + TriGrabConfig.PrepTicks;
            s.Clock = new TriGrabRoundClock
            {
                BaseAngle0 = prevBase,
                Omega = (TriGrabConfig.RotationBase + TriGrabConfig.RotationStep * (s.Round - 1)) * (s.Round % 2 == 1 ? 1f : -1f),
                LiveStartTick = liveStart,
                FreezeTick = long.MaxValue,
            };

            foreach (var slot in s.Slots)
            {
                slot.Card = MakeCard(_rng.NextDouble() < TriGrabConfig.ValidProbability, liveStart, true);
                slot.EmptyUntilTick = 0;
                slot.ValidSinceTick = -1;
            }
            s.Hooks.Clear();
            foreach (var p in s.Players.Values) { p.ReloadUntilTick = 0; p.RoundPlace = 0; p.RoundPoints = 0; }

            s.RoundWinnerId = "";
            s.RoundCorrectCount = 0;
            s.RoundLimitTick = liveStart + TriGrabConfig.RoundLimitTicks;
            s.Phase = TriGrabPhase.Prep;
            s.PhaseEndTick = liveStart;
            s.LastValidTick = liveStart;
            s.Version++;
        }

        // 원본 시안 makeCard와 같다. want이면 정답 카드, 아니면 오답 카드(헷갈리게 일부러 한 칸을 주문 색으로).
        TriGrabCard MakeCard(bool want, long t, bool allowCycle)
        {
            var s = State;
            var r = s.Rule;
            bool cyc = allowCycle && s.Round >= 2 && _rng.NextDouble() < TriGrabConfig.CycleProbability;
            for (int i = 0; i < 60; i++)
            {
                var c = new TriGrabCard
                {
                    Id = _nextCardId,
                    Word = _rng.Next(3),
                    Ink = _rng.Next(3),
                    Cycles = cyc,
                    PeriodTicks = TriGrabConfig.CyclePeriodTicks(s.Round),
                    T0Tick = t,
                };
                if (!want && r.Type != TriGrabRuleType.Match && _rng.NextDouble() < 0.5)
                {
                    if (r.Type == TriGrabRuleType.Ink) c.Word = r.Color; else c.Ink = r.Color;
                }
                if (cyc)
                {
                    if (r.Type != TriGrabRuleType.Word || (c.Word == r.Color) == want) { _nextCardId++; return c; }
                    continue;
                }
                if (r.Matches(c, t) == want) { _nextCardId++; return c; }
            }
            return new TriGrabCard { Id = _nextCardId++, T0Tick = t, PeriodTicks = TriGrabConfig.CyclePeriodTicks(s.Round) };
        }
    }
}
