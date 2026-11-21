using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 삼색 그랩 봇 두뇌. 정답이 일정 시간 떠 있어야 "안다", 도는 카드는 정답 색이 돌아올 때를 노려 앞질러 겨눈다.
    /// 사람 손 빠르기에 맞췄다(3차): 기본 1.1~1.6초에 어려운 주문(두 축·뒤집기·속임 색)일수록 더 늦고 더 자주 헛손질한다.
    /// 한 인스턴스로 봇 여러 명을 돌려도 된다(봇마다 반응 속도를 따로 갖는다).
    /// </summary>
    public sealed class TriGrabBotBrain
    {
        sealed class BotMind
        {
            public float SkillMs;
            public float RtTicks;
            public float Home;
        }

        readonly System.Random _rng;
        readonly float _minRtMs, _maxRtMs;
        readonly Dictionary<string, BotMind> _minds = new Dictionary<string, BotMind>();
        readonly Vector2[] _pos = new Vector2[TriGrabConfig.Slots];

        public TriGrabBotBrain(int seed, float minRtMs = 1100f, float maxRtMs = 1600f)
        {
            _rng = new System.Random(seed);
            _minRtMs = minRtMs;
            _maxRtMs = maxRtMs;
        }

        float Gauss() => (float)((_rng.NextDouble() + _rng.NextDouble() + _rng.NextDouble() - 1.5) * 2.0);

        float SampleRtTicks(BotMind m) => Mathf.Max(500f, m.SkillMs + Gauss() * 200f) / 20f;

        /// <summary>주문이 어려울수록 알아채는 데 더 걸리는 시간(ms).</summary>
        public static float RuleDelayMs(in TriGrabRule r)
        {
            float ms = 0f;
            if (r.IsCompound) ms += 450f;
            if (r.Negate) ms += 250f;
            if (r.DecoyInk >= 0) ms += 200f;
            if (r.Type == TriGrabRuleType.Match || r.Type == TriGrabRuleType.ShapeMatch) ms += 150f;
            return ms;
        }

        /// <summary>쏘기로 한 순간 정답 대신 가까운 오답을 쏠 확률(헛손질). 주문이 어려울수록 높다.</summary>
        public static double SlipProbability(in TriGrabRule r)
        {
            double p = 0.05;
            if (r.IsCompound) p += 0.07;
            if (r.Negate) p += 0.05;
            if (r.DecoyInk >= 0) p += 0.05;
            return p;
        }

        BotMind MindOf(string id, Vector2 pos)
        {
            if (_minds.TryGetValue(id, out var m)) return m;
            m = new BotMind
            {
                SkillMs = _minRtMs + (float)_rng.NextDouble() * (_maxRtMs - _minRtMs),
                Home = Mathf.Atan2(pos.y, pos.x),
            };
            m.RtTicks = SampleRtTicks(m);
            _minds[id] = m;
            return m;
        }

        // 갈고리가 닿는 데 걸리는 틱과 그때의 카드 자리를 네 번 되풀이해 찾는다.
        (float tt, Vector2 pos) Intercept(Vector2 p, int slot, float baseNow, float omega)
        {
            float hs = TriGrabConfig.HookSpeedPerTick;
            Vector2 pos = TriGrabCarousel.SlotPosition(slot, baseNow);
            float tt = (pos - p).magnitude / hs;
            for (int k = 0; k < 4; k++)
            {
                pos = TriGrabCarousel.SlotPosition(slot, baseNow + omega * TriGrabConfig.TickSeconds * tt);
                tt = Mathf.Max(0f, (pos - p).magnitude - TriGrabConfig.BodyRadius) / hs;
            }
            return (tt, pos);
        }

        // 겨누는 길 위에 다른 카드가 있으면 그게 먼저 잡힌다.
        bool Blocked(TriGrabMatchState s, Vector2 p, int slot, Vector2 pos, float tt, float baseNow)
        {
            Vector2 d = pos - p;
            float L = d.magnitude;
            Vector2 u = d / L;
            for (int j = 0; j < TriGrabConfig.Slots; j++)
            {
                if (j == slot || !s.Slots[j].Card.HasValue) continue;
                Vector2 q = TriGrabCarousel.SlotPosition(j, baseNow + s.Clock.Omega * TriGrabConfig.TickSeconds * tt * 0.5f);
                float along = (q.x - p.x) * u.x + (q.y - p.y) * u.y;
                if (along <= 0f || along >= L) continue;
                if (Mathf.Abs((q.x - p.x) * u.y - (q.y - p.y) * u.x) < TriGrabConfig.HitRadius * 0.9f) return true;
            }
            return false;
        }

        /// <summary>매 틱 한 번. 갈고리가 나가 있으면 가만히 서고 쏘지 않는다. 이번 라운드에 정답을 냈으면 쏘지 않는다.</summary>
        public void Think(string botId, Vector2 pos, long tick, TriGrabMatchState s, out Vector2 move, out float? fireYawDegrees)
        {
            move = Vector2.zero;
            fireYawDegrees = null;
            var mind = MindOf(botId, pos);

            if (s.Phase != TriGrabPhase.Live) return;
            foreach (var h in s.Hooks) if (h.OwnerId == botId) return;

            s.Players.TryGetValue(botId, out var me);
            // 이번 라운드에 정답을 냈으면 더 쏘지 않고, 아직 못 낸 사람 길을 막지 않게 고리 바깥쪽 집 자리로 물러난다.
            bool done = me != null && me.RoundPlace > 0;
            if (done)
            {
                WalkTo(pos, HomePoint(mind, TriGrabConfig.BotRingMax - 0.6f), out move);
                return;
            }
            bool reloading = me != null && tick < me.ReloadUntilTick;
            float rt = mind.RtTicks + RuleDelayMs(s.Rule) / 20f;
            float baseNow = s.Clock.BaseAt(tick);
            float omega = s.Clock.Omega;

            Vector2? goal = null;
            float gd = float.MaxValue;
            bool hasShot = false;
            float shotTt = 0f;
            Vector2 shotPos = Vector2.zero;
            bool shotCycles = false;
            bool hasMisjudge = false;
            float misjudgeTt = 0f;
            Vector2 misjudgePos = Vector2.zero;
            bool hasSlip = false;
            float slipTt = 0f;
            Vector2 slipPos = Vector2.zero;

            for (int i = 0; i < TriGrabConfig.Slots; i++)
            {
                var slot = s.Slots[i];
                if (!slot.Card.HasValue) continue;
                var c = slot.Card.Value;
                bool seenValid = slot.ValidSinceTick >= 0 && tick - slot.ValidSinceTick >= rt;
                // 도는 카드는 "보고 있었다면" 정답 색이 돌아올 때를 노린다
                bool known = c.Cycles
                    ? ((tick - c.T0Tick >= rt && s.Rule.DependsOnInk && CanEverMatch(s.Rule, c)) || seenValid)
                    : seenValid;

                var (tt, ipos) = Intercept(pos, i, baseNow, omega);
                float dist = (ipos - pos).magnitude - TriGrabConfig.BodyRadius;

                if (known)
                {
                    Vector2 sp = TriGrabCarousel.SlotPosition(i, baseNow + omega * 0.7f);
                    float k = (TriGrabConfig.BotRingMin + 0.2f) / TriGrabConfig.RingRadius;
                    Vector2 g = sp * k;
                    float d = (g - pos).magnitude;
                    if (d < gd) { gd = d; goal = g; }
                }

                if (reloading || dist > TriGrabConfig.HookLength) continue;

                if (known)
                {
                    bool willBeValid = s.Rule.Matches(c, tick + (long)Mathf.Ceil(tt));
                    if (!Blocked(s, pos, i, ipos, tt, baseNow))
                    {
                        if (willBeValid)
                        {
                            if (!hasShot || tt < shotTt) { hasShot = true; shotTt = tt; shotPos = ipos; shotCycles = c.Cycles; }
                        }
                        else if (c.Cycles && (!hasMisjudge || tt < misjudgeTt))
                        {
                            hasMisjudge = true; misjudgeTt = tt; misjudgePos = ipos;
                        }
                    }
                }
                else if (!s.Rule.Matches(c, tick) && dist < TriGrabConfig.HookLength * 0.8f)
                {
                    if (!hasSlip || tt < slipTt) { hasSlip = true; slipTt = tt; slipPos = ipos; }
                }
            }

            // 도는 카드 오판: 도는 카드를 노려 쏘기로 한 순간 15%로 박자를 잘못 읽어, 지금 정답 색이 아닌 도는 카드를 쏜다.
            // 쏠 때마다 한 번만 굴린다 — 주기마다 굴리면 라운드가 길어질수록(2차: 최대 20초) 오답이 쌓인다.
            if (hasShot && shotCycles && hasMisjudge && _rng.NextDouble() < 0.15)
            {
                shotTt = misjudgeTt; shotPos = misjudgePos;
            }

            // 헛손질: 쏘기로 한 순간 정답 대신 가까운 오답 카드를 쏜다(쏠 때마다 한 번만 굴린다, 주문이 어려울수록 자주).
            if (hasShot && hasSlip && _rng.NextDouble() < SlipProbability(s.Rule))
            {
                shotTt = slipTt; shotPos = slipPos;
            }

            if (hasShot)
            {
                float e = Gauss() * 0.04f;
                Vector2 d = shotPos - pos;
                float cs = Mathf.Cos(e), sn = Mathf.Sin(e);
                Vector2 aimed = new Vector2(d.x * cs - d.y * sn, d.x * sn + d.y * cs);
                fireYawDegrees = TriGrabMath.YawFromDir(aimed);
                mind.RtTicks = SampleRtTicks(mind);
                return;
            }

            if (!goal.HasValue) goal = HomePoint(mind, TriGrabConfig.BotRingMin + 0.8f);
            WalkTo(pos, goal.Value, out move);
        }

        static bool CanEverMatch(in TriGrabRule r, in TriGrabCard c)
        {
            for (int ink = 0; ink < 3; ink++) if (r.MatchesWithInk(c, ink)) return true;
            return false;
        }

        static Vector2 HomePoint(BotMind mind, float radius) => new Vector2(radius * Mathf.Cos(mind.Home), radius * Mathf.Sin(mind.Home));

        static void WalkTo(Vector2 pos, Vector2 goal, out Vector2 move)
        {
            move = Vector2.zero;
            Vector2 to = goal - pos;
            float dd = to.magnitude;
            if (dd > 0.3f) move = to / dd;
        }
    }
}
