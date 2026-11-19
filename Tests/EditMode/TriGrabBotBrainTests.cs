using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class TriGrabBotBrainTests
    {
        static TriGrabMatch LiveMatch(out long t, params string[] ids)
        {
            var m = new TriGrabMatch(77);
            foreach (var id in ids) m.AddPlayer(id);
            m.Start(0);
            for (t = 0; t <= TriGrabConfig.PrepTicks; t++) m.Tick(t);
            m.State.Rule = new TriGrabRule(TriGrabRuleType.Ink, 0);
            m.State.Clock = new TriGrabRoundClock { BaseAngle0 = 0f, Omega = 0f, LiveStartTick = t, FreezeTick = long.MaxValue };
            foreach (var s in m.State.Slots) { s.Card = null; s.EmptyUntilTick = long.MaxValue; s.ValidSinceTick = -1; }
            return m;
        }

        static readonly Vector2 Body = new Vector2(8f, 0f);

        [Test]
        public void 정답이_오래_떠_있으면_그_카드를_향해_쏜다()
        {
            var m = LiveMatch(out long t, "bot");
            m.State.Slots[0].Card = new TriGrabCard { Id = 1, Word = 1, Ink = 0 };
            m.State.Slots[0].ValidSinceTick = t - 60;
            var brain = new TriGrabBotBrain(1);
            brain.Think("bot", Body, t, m.State, out var move, out var yaw);
            Assert.IsTrue(yaw.HasValue);
            Assert.AreEqual(-90f, yaw.Value, 10f, "카드(4,0)는 몸(8,0)의 -x 방향");
            Assert.AreEqual(Vector2.zero, move);
        }

        [Test]
        public void 이번_라운드에_정답을_냈으면_쏘지_않는다_양성_대조()
        {
            var m = LiveMatch(out long t, "bot", "other");
            m.State.Slots[0].Card = new TriGrabCard { Id = 1, Word = 1, Ink = 0 };
            m.State.Slots[0].ValidSinceTick = t - 60;
            var brain = new TriGrabBotBrain(1);
            brain.Think("bot", Body, t, m.State, out _, out var before);
            Assert.IsTrue(before.HasValue, "양성 대조 — 아직 못 냈으면 쏜다");
            m.State.Players["bot"].RoundPlace = 1;
            brain.Think("bot", Body, t, m.State, out _, out var after);
            Assert.IsFalse(after.HasValue, "정답을 낸 라운드엔 안 쏜다");
        }

        [Test]
        public void 정답이_막_떠서_아직_모르면_쏘지_않는다()
        {
            var m = LiveMatch(out long t, "bot");
            m.State.Slots[0].Card = new TriGrabCard { Id = 1, Word = 1, Ink = 0 };
            m.State.Slots[0].ValidSinceTick = t; // 반응 시간(최소 10틱)이 안 지났다
            var brain = new TriGrabBotBrain(1);
            brain.Think("bot", Body, t, m.State, out var move, out var yaw);
            Assert.IsFalse(yaw.HasValue);
        }

        [Test]
        public void 갈고리가_나가_있으면_가만히_서고_쏘지_않는다()
        {
            var m = LiveMatch(out long t, "bot");
            m.State.Slots[0].Card = new TriGrabCard { Id = 1, Word = 1, Ink = 0 };
            m.State.Slots[0].ValidSinceTick = t - 60;
            var brain = new TriGrabBotBrain(1);
            brain.Think("bot", Body, t, m.State, out _, out var yawBefore);
            Assert.IsTrue(yawBefore.HasValue, "양성 대조 — 갈고리가 없으면 쏜다");

            Assert.IsTrue(m.TryFire("bot", Body, 90f, t));
            brain.Think("bot", Body, t + 1, m.State, out var move, out var yaw);
            Assert.IsFalse(yaw.HasValue);
            Assert.AreEqual(Vector2.zero, move);
        }

        [Test]
        public void 재장전_중에는_쏘지_않는다()
        {
            var m = LiveMatch(out long t, "bot");
            m.State.Slots[0].Card = new TriGrabCard { Id = 1, Word = 1, Ink = 0 };
            m.State.Slots[0].ValidSinceTick = t - 60;
            m.State.Players["bot"].ReloadUntilTick = t + 50;
            var brain = new TriGrabBotBrain(1);
            brain.Think("bot", Body, t, m.State, out _, out var yaw);
            Assert.IsFalse(yaw.HasValue);
        }

        [Test]
        public void 앞을_다른_카드가_막으면_쏘지_않는다()
        {
            var m = LiveMatch(out long t, "bot");
            // 슬롯 0(4,0)이 정답. 몸이 (0,-8)... 대신 같은 줄 위 슬롯 4(-4,0) 쪽에서 쏘게 몸을 (-8,0)에 둔다: 슬롯 4가 앞을 막는다.
            m.State.Slots[0].Card = new TriGrabCard { Id = 1, Word = 1, Ink = 0 };
            m.State.Slots[0].ValidSinceTick = t - 60;
            m.State.Slots[4].Card = new TriGrabCard { Id = 2, Word = 0, Ink = 2 };
            var brain = new TriGrabBotBrain(1);
            brain.Think("bot", new Vector2(-8f, 0f), t, m.State, out _, out var yaw);
            Assert.IsFalse(yaw.HasValue);
        }

        [Test]
        public void Live가_아니면_가만히_있다()
        {
            var m = new TriGrabMatch(1);
            m.AddPlayer("bot");
            m.Start(0);
            var brain = new TriGrabBotBrain(1);
            brain.Think("bot", Body, 5, m.State, out var move, out var yaw);
            Assert.IsFalse(yaw.HasValue);
            Assert.AreEqual(Vector2.zero, move);
        }

        [Test]
        public void 정답이_없으면_고리_안쪽_집_자리로_걸어간다()
        {
            var m = LiveMatch(out long t, "bot");
            var brain = new TriGrabBotBrain(1);
            var far = new Vector2(9.4f, 0f);
            brain.Think("bot", far, t, m.State, out _, out _); // 처음 본 자리가 집이 된다
            brain.Think("bot", new Vector2(0f, 9.4f), t, m.State, out var move, out _);
            Assert.Greater(move.magnitude, 0.9f, "집(반지름 7.1, 0도)으로 향한다");
            Assert.Greater(move.x, 0f);
        }

        [Test]
        public void 봇_8명_한_판이_상한_안에_끝난다()
        {
            const int n = 8;
            var m = new TriGrabMatch(2024);
            var brain = new TriGrabBotBrain(99);
            var ids = new string[n];
            var pos = new Vector2[n];
            for (int i = 0; i < n; i++) { ids[i] = "bot" + i; m.AddPlayer(ids[i]); pos[i] = TriGrabMatchTests.Ring(i, n); }
            m.Start(0);

            long t = 0;
            int fired = 0, roundsEndedByQuota = 0;
            var prevPhase = m.State.Phase;
            for (; t < TriGrabConfig.MatchCapTicks && m.State.Phase != TriGrabPhase.Over; t++)
            {
                for (int i = 0; i < n; i++)
                {
                    brain.Think(ids[i], pos[i], t, m.State, out var mv, out var yaw);
                    if (yaw.HasValue) { if (m.TryFire(ids[i], pos[i], yaw.Value, t)) fired++; }
                    else pos[i] = TriGrabMatchTests.ClampRing(pos[i] + mv * (TriGrabConfig.MoveSpeed * TriGrabConfig.TickSeconds));
                }
                m.Tick(t);
                if (prevPhase == TriGrabPhase.Live && m.State.Phase == TriGrabPhase.Resolved && m.State.RoundCorrectCount == 6) roundsEndedByQuota++;
                prevPhase = m.State.Phase;
            }

            Assert.AreEqual(TriGrabPhase.Over, m.State.Phase);
            Assert.Less(t, TriGrabConfig.MatchCapTicks, "안전 상한이 아니라 다섯 라운드로 끝나야 한다");
            Assert.AreEqual(TriGrabConfig.TotalRounds, m.State.Round);
            Assert.Less(t, TriGrabConfig.TotalRounds * (TriGrabConfig.PrepTicks + TriGrabConfig.RoundLimitTicks + TriGrabConfig.ResolvedTicks) + 10);
            Assert.GreaterOrEqual(roundsEndedByQuota, 3, $"라운드 대부분이 6명 정답으로 끝난다 (시간 상한 아님) — {roundsEndedByQuota}");
            int top = 0;
            foreach (var p in m.State.Players.Values) top = Mathf.Max(top, p.Score);
            Assert.Greater(top, 20, "누적 점수");
            Assert.Greater(fired, 5);
        }

        [Test]
        public void 봇_8명_판에서_오답_집기_비율이_낮다()
        {
            const int n = 8;
            int wrong = 0, right = 0, w1 = 0, w2 = 0;
            for (int seed = 1; seed <= 4; seed++)
            {
                var m = new TriGrabMatch(seed * 31);
                var brain = new TriGrabBotBrain(seed * 7);
                var ids = new string[n];
                var pos = new Vector2[n];
                for (int i = 0; i < n; i++) { ids[i] = "bot" + i; m.AddPlayer(ids[i]); pos[i] = TriGrabMatchTests.Ring(i, n); }
                m.Start(0);
                int prevM = 0; var prevPh = m.State.Phase;
                for (long t = 0; t < TriGrabConfig.MatchCapTicks && m.State.Phase != TriGrabPhase.Over; t++)
                {
                    for (int i = 0; i < n; i++)
                    {
                        brain.Think(ids[i], pos[i], t, m.State, out var mv, out var yaw);
                        if (yaw.HasValue) m.TryFire(ids[i], pos[i], yaw.Value, t);
                        else pos[i] = TriGrabMatchTests.ClampRing(pos[i] + mv * (TriGrabConfig.MoveSpeed * TriGrabConfig.TickSeconds));
                    }
                    m.Tick(t);
                    if (prevPh == TriGrabPhase.Live && m.State.Phase != TriGrabPhase.Live) right += m.State.RoundCorrectCount;
                    prevPh = m.State.Phase;
                    int mm = 0; foreach (var pp in m.State.Players.Values) mm += pp.Mistakes;
                    if (mm > prevM) { if (m.State.Round >= 2) w2 += mm - prevM; else w1 += mm - prevM; prevM = mm; }
                }
                foreach (var p in m.State.Players.Values) wrong += p.Mistakes;
            }
            Assert.Greater(right, 8, "양성 대조 — 라운드가 충분히 돌았다");
            //  2차(순위 점수)부터는 한 라운드에 여섯 명이 정답을 낼 때까지 모두 계속 쏘므로, 남이 먼저 가져간 카드 자리로
            //  날아간 갈고리(경합)가 오답에 섞인다(4판 기준 약 3할). 그래서 1차의 0.25보다 넉넉하게 둔다 —
            //  도는 카드 오판을 주기마다 굴리던 옛 식이면 이 비율이 더 올라간다.
            Assert.Less(wrong / (float)(wrong + right), 0.55f, $"오답 {wrong} (r1 {w1}, r2+ {w2}) / 정답 {right}");
        }

        [Test]
        public void 도는_카드_오판은_쏠_때만_굴린다_정답_쏘기가_없으면_틀린_도는_카드를_쏘지_않는다()
        {
            int fired = 0;
            for (int seed = 0; seed < 100; seed++)
            {
                var m = LiveMatch(out long t, "bot");
                // 도는 카드(주기 60틱, 이미 30틱 지남) — 지금도, 갈고리가 닿을 때(최대 17틱 뒤)도 같은 주기 안이라 계속 오답이다.
                m.State.Slots[0].Card = new TriGrabCard { Id = 1, Word = 1, Ink = 2, Cycles = true, PeriodTicks = 60, T0Tick = t - 30 };
                var brain = new TriGrabBotBrain(seed);
                for (long k = t; k < t + 10; k++)
                {
                    brain.Think("bot", Body, k, m.State, out _, out var yaw);
                    if (yaw.HasValue) { fired++; break; }
                }
            }
            Assert.AreEqual(0, fired, "주기마다 굴리면 라운드가 길수록 틀린 도는 카드를 쏜다(2차는 라운드가 최대 20초)");
        }

        [Test]
        public void 도는_카드를_노려_쏠_때_15퍼센트쯤_박자를_잘못_읽는다()
        {
            int fired = 0, wrong = 0;
            for (int seed = 0; seed < 400; seed++)
            {
                var m = LiveMatch(out long t, "bot");
                // 슬롯 0 (4,0): 지금 글자색 빨강(정답)인 도는 카드. 슬롯 2 (0,4): 지금 파랑인 도는 카드.
                m.State.Slots[0].Card = new TriGrabCard { Id = 1, Word = 1, Ink = 0, Cycles = true, PeriodTicks = 100, T0Tick = t - 40 };
                m.State.Slots[2].Card = new TriGrabCard { Id = 2, Word = 1, Ink = 2, Cycles = true, PeriodTicks = 100, T0Tick = t - 40 };
                m.State.Slots[0].ValidSinceTick = t - 60;
                var brain = new TriGrabBotBrain(seed);
                brain.Think("bot", new Vector2(6.5f, 6.5f), t, m.State, out _, out var yaw);
                if (!yaw.HasValue) continue;
                fired++;
                var dir = TriGrabMath.DirFromYaw(yaw.Value);
                //  (6.5,6.5)에서 (4,0) 쪽은 z로 더 많이, (0,4) 쪽은 x로 더 많이 간다.
                if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) wrong++;
            }
            Assert.Greater(fired, 300, "양성 대조 — 거의 매번 쏜다");
            float rate = wrong / (float)fired;
            Assert.That(rate, Is.InRange(0.07f, 0.25f), $"오판 {wrong}/{fired}");
        }
    }
}
