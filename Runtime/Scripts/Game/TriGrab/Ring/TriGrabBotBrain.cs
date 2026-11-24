using System;
using System.Numerics;

namespace LOP.TriGrab
{
    /// <summary>
    /// 봇 한 마리의 두뇌(시안 botThink·predict·rayFirst, 실력 "보통").
    /// 목표·기다림·옆걸음 방향을 직접 들고 있으니 봇마다 하나씩 만든다.
    /// </summary>
    public sealed class TriGrabBotBrain
    {
        const float ReactSeconds = 1.0f, ReactJitter = 0.35f;
        const float NextSeconds = 0.65f, NextJitter = 0.3f;
        const float TrapChance = 0.18f;
        const float AimErrorDegrees = 4f;
        const float RangeFactor = 0.9f;
        const float LaneMargin = 0.5f;
        const float StrafeFlipSeconds = 1.2f;
        const float RayStep = 0.2f;
        const float RayMargin = 0.5f;
        const float MoleLaneMargin = 0.25f;
        const float MoleMinHeight = 0.2f;
        const float AroundAngle = 0.9f;

        readonly Random _rng;
        int _wait = -1;
        bool _first;
        TriGrabCard _target;
        int _side;
        int _strafeTicks;
        int _round = -1;

        public TriGrabBotBrain(int seed) { _rng = new Random(seed); }

        public void Think(string id, Vector2 pos, long tick, TriGrabMatchState s, out Vector2 move, out float? fireYawDegrees)
        {
            move = Vector2.Zero;
            fireYawDegrees = null;
            if (s.Phase != TriGrabPhase.Live) return;

            if (s.Round != _round)
            {
                _round = s.Round;
                _target = null;
                _first = false;
                _wait = -1;
            }
            if (_wait < 0) _wait = Ticks(ReactSeconds + (float)_rng.NextDouble() * ReactJitter);
            if (_wait > 0) { _wait--; return; }

            if (_target == null || !Takeable(_target, s, id, tick))
            {
                PickTarget(pos, tick, s, id);
                return;
            }

            var c = _target;
            float d = Vector2.Distance(pos, c.Pos);
            if (d > TriGrabConfig.HookRange * RangeFactor)
            {
                move = WalkAround(pos, c.Pos);
                return;
            }

            if (!CanFire(id, tick, s)) return;

            var q = Predict(c, pos);
            var toQ = q - pos;
            float qd = toQ.Length();
            if (qd < 1e-4f) return;
            var u = toQ / qd;

            if (MoleInTheWay(pos, u, qd, tick, s)) return;

            var blocker = RayFirst(pos, u, tick, s);
            if (blocker != null && blocker != c)
            {
                if (_side == 0) _side = _rng.Next(2) == 0 ? 1 : -1;
                _strafeTicks++;
                if (_strafeTicks > Ticks(StrafeFlipSeconds)) { _side = -_side; _strafeTicks = 0; }
                move = Tangent(pos) * _side;
                return;
            }

            float jitter = ((float)_rng.NextDouble() - 0.5f) * 2f * AimErrorDegrees;
            fireYawDegrees = TriGrabMath.YawFromDir(u) + jitter;
        }

        void PickTarget(Vector2 pos, long tick, TriGrabMatchState s, string id)
        {
            bool wantTrap = _rng.NextDouble() < TrapChance;
            TriGrabCard best = null, bestTrap = null;
            float bestD = float.MaxValue, bestTrapD = float.MaxValue;
            for (int i = 0; i < s.Cards.Count; i++)
            {
                var c = s.Cards[i];
                if (!Takeable(c, s, id, tick)) continue;
                float d = Vector2.Distance(pos, c.Pos);
                if (c.Ok) { if (d < bestD) { bestD = d; best = c; } }
                else if (TriGrabCards.IsTrap(c.Word, c.Ink, c.Fill, s.Rule) && d < bestTrapD) { bestTrapD = d; bestTrap = c; }
            }
            var pick = wantTrap && bestTrap != null ? bestTrap : best;
            if (pick == null) return;
            _target = pick;
            if (_first) _wait = Ticks(NextSeconds + (float)_rng.NextDouble() * NextJitter);
            _first = true;
        }

        // 이미 사라졌거나 누가 끌고 가거나 아직 덜 솟은 카드는 노리지 않는다.
        static bool Takeable(TriGrabCard c, TriGrabMatchState s, string id, long tick)
        {
            return !c.Gone && c.CarriedBy == null && TriGrabCards.PopK(c, tick) >= 0.5f && s.Cards.Contains(c);
        }

        static bool CanFire(string id, long tick, TriGrabMatchState s)
        {
            if (s.Players.TryGetValue(id, out var p) && (tick < p.StunUntilTick || tick < p.CooldownUntilTick)) return false;
            for (int i = 0; i < s.Hooks.Count; i++)
                if (s.Hooks[i].Owner == id) return false;
            return true;
        }

        // 카드가 미끄러지므로 손이 닿을 때쯤 카드가 있을 자리를 노린다(벽 반사는 무시).
        static Vector2 Predict(TriGrabCard c, Vector2 pos)
        {
            var q = c.Pos;
            for (int k = 0; k < 2; k++)
            {
                float time = Math.Min(Vector2.Distance(q, pos), TriGrabConfig.HookRange) / TriGrabConfig.HookOutSpeed;
                q = c.Pos + c.Vel * time;
            }
            float r = q.Length();
            if (r > TriGrabConfig.CardWallRadius) q *= TriGrabConfig.CardWallRadius / r;
            return q;
        }

        static Vector2 Tangent(Vector2 pos)
        {
            float pa = (float)Math.Atan2(pos.Y, pos.X);
            return new Vector2(-(float)Math.Sin(pa), (float)Math.Cos(pa));
        }

        static int Ticks(float seconds) => (int)Math.Round(seconds / TriGrabConfig.TickSeconds);

        // 카드 쪽 각도가 크게 다르면 둘레길을 따라 돌고, 가까워지면 둘레길 안쪽 가장자리의 카드 앞으로 곧장 간다.
        static Vector2 WalkAround(Vector2 pos, Vector2 cardPos)
        {
            float lane = TriGrabConfig.LaneMin + LaneMargin;
            float ca = (float)Math.Atan2(cardPos.Y, cardPos.X), pa = (float)Math.Atan2(pos.Y, pos.X);
            float df = WrapAngle(ca - pa);
            if (Math.Abs(df) > AroundAngle)
            {
                float sign = df >= 0f ? 1f : -1f;
                float rad = Math.Max(-1f, Math.Min(1f, (lane - pos.Length()) / 0.8f));
                var radial = new Vector2((float)Math.Cos(pa), (float)Math.Sin(pa));
                return Limit(Tangent(pos) * sign + radial * rad);
            }
            var goal = new Vector2((float)Math.Cos(ca), (float)Math.Sin(ca)) * lane;
            return Limit((goal - pos) / 0.5f);
        }

        static Vector2 Limit(Vector2 v)
        {
            float l = v.Length();
            return l > 1f ? v / l : v;
        }

        static float WrapAngle(float a)
        {
            const float TwoPi = (float)(Math.PI * 2.0);
            while (a > Math.PI) a -= TwoPi;
            while (a < -Math.PI) a += TwoPi;
            return a;
        }

        // 서 있는 두더지가 손 길(몸→노린 자리) 위에 있으면 기다린다.
        static bool MoleInTheWay(Vector2 pos, Vector2 u, float qd, long tick, TriGrabMatchState s)
        {
            for (int i = 0; i < s.Moles.Count; i++)
            {
                var m = s.Moles[i];
                var rel = m.Pos - pos;
                float along = Vector2.Dot(rel, u);
                float side = Math.Abs(rel.X * u.Y - rel.Y * u.X);
                if (along > 0f && along < qd && side < TriGrabConfig.MoleRadius + MoleLaneMargin && m.HeightAt(tick) > MoleMinHeight)
                    return true;
            }
            return false;
        }

        // 지금 자리 그대로 이 방향으로 뻗으면 처음 닿을 카드.
        static TriGrabCard RayFirst(Vector2 pos, Vector2 u, long tick, TriGrabMatchState s)
        {
            float reach = TriGrabConfig.CoinRadius + RayMargin;
            for (float l = 0f; l <= TriGrabConfig.HookRange; l += RayStep)
            {
                var tip = pos + u * l;
                TriGrabCard best = null;
                float bestD = float.MaxValue;
                for (int i = 0; i < s.Cards.Count; i++)
                {
                    var c = s.Cards[i];
                    if (c.Gone || c.CarriedBy != null || TriGrabCards.PopK(c, tick) < 0.5f) continue;
                    float d = Vector2.Distance(c.Pos, tip);
                    if (d < reach && d < bestD) { bestD = d; best = c; }
                }
                if (best != null) return best;
            }
            return null;
        }
    }
}
