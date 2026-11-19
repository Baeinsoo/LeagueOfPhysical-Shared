using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class TriGrabMatchTests
    {
        const string A = "a";
        const string B = "b";

        // 손으로 배치할 수 있게 회전판을 세우고(Omega 0) 슬롯을 비워 둔 Live 판.
        static TriGrabMatch LiveMatch(int seed, out long t, params string[] ids)
        {
            var m = new TriGrabMatch(seed);
            foreach (var id in ids) m.AddPlayer(id);
            m.Start(0);
            for (t = 0; t <= TriGrabConfig.PrepTicks; t++) m.Tick(t);
            Assert.AreEqual(TriGrabPhase.Live, m.State.Phase);
            m.State.Rule = new TriGrabRule(TriGrabRuleType.Ink, 0);
            var c = m.State.Clock;
            c.Omega = 0f;
            c.BaseAngle0 = 0f;
            m.State.Clock = c;
            foreach (var s in m.State.Slots) { s.Card = null; s.EmptyUntilTick = long.MaxValue; s.ValidSinceTick = -1; }
            m.State.LastValidTick = t;
            return m;
        }

        static TriGrabCard RedInk(int id = 1) => new TriGrabCard { Id = id, Word = 1, Ink = 0 };   // 글자색 빨강 = 정답
        static TriGrabCard BlueInk(int id = 2) => new TriGrabCard { Id = id, Word = 0, Ink = 2 };  // 오답

        // 슬롯 0은 (4,0). 바깥 (8,0)에서 안쪽(-x)으로 쏜다.
        static readonly Vector2 Body = new Vector2(8f, 0f);
        const float YawIn = -90f;

        // 쏘고 갈고리가 끝날 때까지 한 틱씩 돌린다. 반환: 마지막 틱
        static long FireAndRun(TriGrabMatch m, string id, long t, Vector2 body, float yaw)
        {
            Assert.IsTrue(m.TryFire(id, body, yaw, t));
            var hook = m.HookOf(id);
            long end = hook.EndTick + 1;
            for (long k = t + 1; k <= end; k++) m.Tick(k);
            return end;
        }

        [Test]
        public void 정답을_끌어오면_점수_1_라운드가_끝나고_회전판이_멈춘다()
        {
            var m = LiveMatch(1, out long t, A, B);
            m.State.Slots[0].Card = RedInk();

            Assert.IsTrue(m.TryFire(A, Body, YawIn, t));
            long grab = -1;
            for (long k = t + 1; k < t + 40 && m.State.Phase == TriGrabPhase.Live; k++) { m.Tick(k); grab = k; }

            Assert.AreEqual(TriGrabPhase.Resolved, m.State.Phase);
            Assert.AreEqual(1, m.State.Players[A].Score);
            Assert.AreEqual(0, m.State.Players[B].Score);
            Assert.AreEqual(A, m.State.RoundWinnerId);
            Assert.AreEqual(grab, m.State.Clock.FreezeTick);
            Assert.AreEqual(TriGrabHookResult.Correct, m.HookOf(A).Result);
            Assert.IsFalse(m.State.Slots[0].Card.HasValue);
            Assert.AreEqual(grab + TriGrabConfig.ResolvedTicks, m.State.PhaseEndTick);
            m.State.Clock = new TriGrabRoundClock { BaseAngle0 = 0f, Omega = 0.5f, LiveStartTick = t, FreezeTick = m.State.Clock.FreezeTick };
            Assert.AreEqual(m.State.Clock.BaseAt(grab), m.State.Clock.BaseAt(grab + 30), 1e-6f, "멈춘 뒤엔 각이 그대로");
            Assert.Greater(m.State.Clock.BaseAt(grab), m.State.Clock.BaseAt(t), "멈추기 전엔 돌았다");
        }

        [Test]
        public void 오답은_감점_0_아래로_안_내려가고_재장전은_끝틱_65틱_뒤()
        {
            var m = LiveMatch(2, out long t, A);
            m.State.Slots[0].Card = BlueInk();
            Assert.IsTrue(m.TryFire(A, Body, YawIn, t));
            for (long k = t + 1; k <= t + 8; k++) m.Tick(k);

            var hook = m.HookOf(A);
            Assert.AreEqual(TriGrabHookResult.Wrong, hook.Result);
            Assert.AreEqual(0, m.State.Players[A].Score, "0점에서 오답은 0 그대로");
            Assert.AreEqual(1, m.State.Players[A].Mistakes);
            Assert.AreEqual(hook.EndTick + TriGrabConfig.ReloadWrongTicks, m.State.Players[A].ReloadUntilTick);
            Assert.AreEqual(TriGrabPhase.Live, m.State.Phase, "오답은 라운드를 끝내지 않는다");
        }

        [Test]
        public void 오답은_점수를_1_깎는다()
        {
            var m = LiveMatch(3, out long t, A);
            m.State.Players[A].Score = 2;
            m.State.Slots[0].Card = BlueInk();
            FireAndRun(m, A, t, Body, YawIn);
            Assert.AreEqual(1, m.State.Players[A].Score);
        }

        [Test]
        public void 빗나가면_재장전은_끝틱_13틱_뒤고_그_전엔_못_쏜다()
        {
            var m = LiveMatch(4, out long t, A);
            Assert.IsTrue(m.TryFire(A, Body, 90f, t)); // 벽 쪽 — 카드 없음
            var hook = m.HookOf(A);
            long reload = hook.EndTick + TriGrabConfig.ReloadMissTicks;
            Assert.AreEqual(reload, m.State.Players[A].ReloadUntilTick);
            for (long k = t + 1; k <= reload - 1; k++) { m.Tick(k); }
            Assert.IsNull(m.HookOf(A), "갈고리는 끝나면 목록에서 빠진다");
            Assert.IsFalse(m.TryFire(A, Body, 90f, reload - 1), "재장전 중");
            Assert.IsTrue(m.TryFire(A, Body, 90f, reload), "재장전이 끝나면 쏜다");
            Assert.AreEqual(0, m.State.Players[A].Score);
        }

        [Test]
        public void 몸에서_가장_먼저_닿는_카드를_잡는다()
        {
            var m = LiveMatch(5, out long t, A);
            // 슬롯 6은 (0,-4), 슬롯 2는 (0,4). (0,-8)에서 +z로 쏘면 6번이 먼저 닿는다. 6번은 오답, 2번은 정답.
            m.State.Slots[6].Card = BlueInk(10);
            m.State.Slots[2].Card = RedInk(11);
            FireAndRun(m, A, t, new Vector2(0, -8), 0f);
            Assert.AreEqual(1, m.State.Players[A].Mistakes);
            Assert.IsFalse(m.State.Slots[6].Card.HasValue);
            Assert.IsTrue(m.State.Slots[2].Card.HasValue, "뒤의 카드는 건드리지 않는다");
        }

        [Test]
        public void 빈_슬롯은_35틱_뒤_새_카드로_찬다()
        {
            var m = LiveMatch(6, out long t, A);
            m.State.Slots[0].Card = BlueInk();
            m.State.Slots[0].EmptyUntilTick = 0;
            Assert.IsTrue(m.TryFire(A, Body, YawIn, t));
            long grab = -1;
            for (long k = t + 1; k < t + 30; k++)
            {
                m.Tick(k);
                if (!m.State.Slots[0].Card.HasValue) { grab = k; break; }
            }
            Assert.Greater(grab, 0, "카드가 잡혔다");
            Assert.AreEqual(grab + TriGrabConfig.RefillTicks, m.State.Slots[0].EmptyUntilTick);
            for (long k = grab + 1; k < grab + TriGrabConfig.RefillTicks; k++)
            {
                m.Tick(k);
                Assert.IsFalse(m.State.Slots[0].Card.HasValue, "아직 비어 있다 @" + (k - grab));
            }
            m.Tick(grab + TriGrabConfig.RefillTicks);
            Assert.IsTrue(m.State.Slots[0].Card.HasValue, "35틱째에 찬다");
        }

        [Test]
        public void 정답이_120틱_없으면_카드_하나를_정답으로_바꾼다()
        {
            var m = LiveMatch(7, out long t, A);
            // 주문은 글자색 빨강 — 전부 오답(파랑)으로 채운다
            foreach (var s in m.State.Slots) s.Card = BlueInk();
            long last = m.State.LastValidTick;
            long forcedAt = -1;
            for (long k = t + 1; k <= last + 300; k++)
            {
                m.Tick(k);
                bool anyValid = false;
                foreach (var s in m.State.Slots) if (s.Card.HasValue && m.State.Rule.Matches(s.Card.Value, k)) anyValid = true;
                if (anyValid) { forcedAt = k; break; }
            }
            Assert.AreEqual(last + TriGrabConfig.ForceValidAfterTicks, forcedAt);
        }

        [Test]
        public void 목표_점수에_닿으면_라운드가_끝난_뒤_Over()
        {
            var m = LiveMatch(8, out long t, A, B);
            m.State.Players[A].Score = TriGrabConfig.TargetScore - 1;
            m.State.Slots[0].Card = RedInk();
            Assert.IsTrue(m.TryFire(A, Body, YawIn, t));
            long k = t;
            while (m.State.Phase == TriGrabPhase.Live) m.Tick(++k);
            Assert.AreEqual(TriGrabPhase.Resolved, m.State.Phase);
            Assert.AreEqual(TriGrabConfig.TargetScore, m.State.Players[A].Score);
            long end = m.State.PhaseEndTick;
            m.Tick(end - 1);
            Assert.AreEqual(TriGrabPhase.Resolved, m.State.Phase, "쉬는 동안엔 Over가 아니다");
            m.Tick(end);
            Assert.AreEqual(TriGrabPhase.Over, m.State.Phase);
            Assert.AreEqual(A, m.State.MatchWinnerId);
        }

        [Test]
        public void 목표_점수_전이면_쉼이_끝나고_다음_라운드_준비로_간다()
        {
            var m = LiveMatch(9, out long t, A);
            m.State.Slots[0].Card = RedInk();
            Assert.IsTrue(m.TryFire(A, Body, YawIn, t));
            long k = t;
            while (m.State.Phase == TriGrabPhase.Live) m.Tick(++k);
            long end = m.State.PhaseEndTick;
            float frozenBase = m.State.Clock.BaseAt(k);
            m.Tick(end);
            Assert.AreEqual(TriGrabPhase.Prep, m.State.Phase);
            Assert.AreEqual(2, m.State.Round);
            Assert.AreEqual(0, m.State.Hooks.Count);
            Assert.AreEqual(frozenBase, m.State.Clock.BaseAngle0, 1e-5f, "앞 라운드가 멈춘 각에서 이어진다");
            Assert.Less(m.State.Clock.Omega, 0f, "짝수 라운드는 반대로 돈다");
            Assert.AreEqual(end + TriGrabConfig.PrepTicks, m.State.Clock.LiveStartTick);
        }

        [Test]
        public void 쏘기_거절_Live가_아닐_때()
        {
            var m = new TriGrabMatch(10);
            m.AddPlayer(A);
            Assert.IsFalse(m.TryFire(A, Body, YawIn, 0), "시작 전");
            m.Start(0);
            Assert.AreEqual(TriGrabPhase.Prep, m.State.Phase);
            Assert.IsFalse(m.TryFire(A, Body, YawIn, 1), "준비 중");
        }

        [Test]
        public void 쏘기_거절_갈고리가_나가_있거나_모르는_선수일_때_양성_대조()
        {
            var m = LiveMatch(11, out long t, A, B);
            Assert.IsTrue(m.TryFire(A, Body, 90f, t), "양성 대조 — 조건이 맞으면 쏜다");
            Assert.IsFalse(m.TryFire(A, Body, 90f, t + 1), "이미 나가 있음");
            Assert.IsTrue(m.TryFire(B, Body, 90f, t + 1), "다른 선수는 쏜다");
            Assert.IsFalse(m.TryFire("nobody", Body, 90f, t + 1));
        }

        [Test]
        public void 쏘기_거절_Resolved에서도()
        {
            var m = LiveMatch(12, out long t, A, B);
            m.State.Slots[0].Card = RedInk();
            Assert.IsTrue(m.TryFire(A, Body, YawIn, t));
            long k = t;
            while (m.State.Phase == TriGrabPhase.Live) m.Tick(++k);
            Assert.IsFalse(m.TryFire(B, Body, 90f, k + 1));
        }

        [Test]
        public void 판본은_보이는_것이_바뀔_때만_오르고_갈고리가_뻗는_것만으로는_안_오른다()
        {
            var m = LiveMatch(13, out long t, A);
            int v0 = m.State.Version;
            Assert.IsTrue(m.TryFire(A, Body, 90f, t));
            int v1 = m.State.Version;
            Assert.Greater(v1, v0, "갈고리가 생김");
            m.Tick(t + 1); m.Tick(t + 2); m.Tick(t + 3);
            Assert.AreEqual(v1, m.State.Version, "뻗는 것은 식이라 안 오른다");
            long hookEnd = m.HookOf(A).EndTick;
            for (long k = t + 4; k <= hookEnd + 1; k++) m.Tick(k);
            Assert.Greater(m.State.Version, v1, "갈고리가 사라짐");
        }

        [Test]
        public void 같은_씨앗은_같은_판이다()
        {
            Assert.AreEqual(Trace(1234), Trace(1234));
            Assert.AreNotEqual(Trace(1234), Trace(4321));
        }

        [Test]
        public void 연달은_라운드의_주문은_화면_글이_같지_않다()
        {
            int checkedRounds = 0;
            for (int seed = 1; seed <= 12; seed++)
            {
                var m = new TriGrabMatch(seed);
                var brain = new TriGrabBotBrain(seed + 1);
                var ids = new[] { "a", "b", "c", "d" };
                var pos = new Vector2[4];
                for (int i = 0; i < 4; i++) { m.AddPlayer(ids[i]); pos[i] = Ring(i, 4); }
                m.Start(0);
                int round = 0;
                string prev = null;
                for (long t = 0; t < TriGrabConfig.MatchCapTicks && m.State.Phase != TriGrabPhase.Over; t++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        brain.Think(ids[i], pos[i], t, m.State, out var mv, out var yaw);
                        if (yaw.HasValue) m.TryFire(ids[i], pos[i], yaw.Value, t);
                        else pos[i] = ClampRing(pos[i] + mv * (TriGrabConfig.MoveSpeed * TriGrabConfig.TickSeconds));
                    }
                    m.Tick(t);
                    if (m.State.Round != round)
                    {
                        round = m.State.Round;
                        string text = m.State.Rule.KoreanText;
                        if (prev != null) { Assert.AreNotEqual(prev, text, $"seed {seed} 라운드 {round}"); checkedRounds++; }
                        prev = text;
                    }
                }
            }
            Assert.Greater(checkedRounds, 20, "양성 대조 — 라운드 전환을 충분히 봤다");
        }

        static string Trace(int seed)
        {
            var m = new TriGrabMatch(seed);
            var brain = new TriGrabBotBrain(seed + 1);
            var ids = new[] { "a", "b", "c", "d" };
            var pos = new Vector2[4];
            for (int i = 0; i < 4; i++) { m.AddPlayer(ids[i]); pos[i] = Ring(i, 4); }
            m.Start(0);
            var sb = new StringBuilder();
            for (long t = 0; t < 1500; t++)
            {
                for (int i = 0; i < 4; i++)
                {
                    brain.Think(ids[i], pos[i], t, m.State, out var mv, out var yaw);
                    if (yaw.HasValue) m.TryFire(ids[i], pos[i], yaw.Value, t);
                    else pos[i] = ClampRing(pos[i] + mv * (TriGrabConfig.MoveSpeed * TriGrabConfig.TickSeconds));
                }
                m.Tick(t);
                var s = m.State;
                sb.Append(s.Version).Append(',').Append((int)s.Phase).Append(',').Append(s.Round).Append(';');
                foreach (var slot in s.Slots) sb.Append(slot.Card.HasValue ? slot.Card.Value.Id : 0).Append('.');
                foreach (var id in ids) sb.Append(s.Players[id].Score).Append('|');
            }
            return sb.ToString();
        }

        internal static Vector2 Ring(int i, int n)
        {
            float a = i * 2f * Mathf.PI / n;
            return 7.85f * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        internal static Vector2 ClampRing(Vector2 p)
        {
            float r = p.magnitude;
            if (r < 1e-5f) return p;
            return p * (Mathf.Clamp(r, TriGrabConfig.BotRingMin, TriGrabConfig.BotRingMax) / r);
        }
    }
}
