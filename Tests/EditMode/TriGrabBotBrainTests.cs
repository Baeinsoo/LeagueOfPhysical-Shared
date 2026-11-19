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
            int fired = 0;
            for (; t < TriGrabConfig.MatchCapTicks && m.State.Phase != TriGrabPhase.Over; t++)
            {
                for (int i = 0; i < n; i++)
                {
                    brain.Think(ids[i], pos[i], t, m.State, out var mv, out var yaw);
                    if (yaw.HasValue) { if (m.TryFire(ids[i], pos[i], yaw.Value, t)) fired++; }
                    else pos[i] = TriGrabMatchTests.ClampRing(pos[i] + mv * (TriGrabConfig.MoveSpeed * TriGrabConfig.TickSeconds));
                }
                m.Tick(t);
            }

            Assert.AreEqual(TriGrabPhase.Over, m.State.Phase);
            Assert.Less(t, TriGrabConfig.MatchCapTicks, "상한에 걸려 끝난 것이 아니라 점수로 끝나야 한다");
            int top = 0;
            foreach (var p in m.State.Players.Values) top = Mathf.Max(top, p.Score);
            Assert.GreaterOrEqual(top, TriGrabConfig.TargetScore);
            Assert.AreNotEqual("", m.State.MatchWinnerId);
            Assert.Greater(fired, 5);
        }
    }
}
