using System;
using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;

namespace LOP.TriGrab.Tests
{
    public class TriGrabBotBrainTests
    {
        const long Now = 1000;
        static readonly Vector2 BotPos = new Vector2(8f, 0f);
        // 규칙: 글씨 색 0이 정답. 미끼 색은 1.
        static readonly TriGrabRule Rule = new TriGrabRule(TriGrabRuleType.Ink, 0, 1);

        static TriGrabMatchState NewState()
        {
            var s = new TriGrabMatchState { Phase = TriGrabPhase.Live, Round = 1, Rule = Rule };
            s.Players["bot"] = new TriGrabPlayerScore();
            return s;
        }

        static TriGrabCard Card(int id, Vector2 pos, bool ok)
        {
            // 오답 카드는 함정(IsTrap)도 아니게 만든다: 글씨 색 2, 글자 3, 바탕 3.
            return new TriGrabCard
            {
                Id = id, Pos = pos, Ok = ok, BornTick = -100,
                Ink = ok ? 0 : 2, Word = ok ? 1 : 3, Fill = ok ? 2 : 3,
            };
        }

        // 기다림·목표 고르기까지 충분히 돌리며 쏜 방향과 움직임을 모은다.
        static (int fires, float firstYaw, Vector2 maxMove) Run(TriGrabBotBrain brain, TriGrabMatchState s, int calls = 300)
        {
            int fires = 0;
            float yaw = float.NaN;
            var mm = Vector2.Zero;
            for (int i = 0; i < calls; i++)
            {
                brain.Think("bot", BotPos, Now, s, out var move, out var fire);
                if (fire.HasValue) { fires++; if (float.IsNaN(yaw)) yaw = fire.Value; }
                if (move.Length() > mm.Length()) mm = move;
            }
            return (fires, yaw, mm);
        }

        [Test]
        public void ClearLane_Fires_AtTarget()
        {
            var s = NewState();
            s.Cards.Add(Card(1, new Vector2(3f, 0f), true));
            var r = Run(new TriGrabBotBrain(1), s);
            Assert.Greater(r.fires, 0, "막힘이 없으면 쏜다");
            Assert.Less(TriGrabMath.DirFromYaw(r.firstYaw).X, -0.99f, "-x 쪽 카드를 향한다");
        }

        [Test]
        public void BlockedLane_Strafes_NotFires()
        {
            var s = NewState();
            s.Cards.Add(Card(1, new Vector2(3f, 0f), true));
            s.Cards.Add(Card(2, new Vector2(5.5f, 0f), false));
            var r = Run(new TriGrabBotBrain(1), s);
            Assert.AreEqual(0, r.fires, "길을 막은 카드가 있으면 쏘지 않는다");
            Assert.Greater(r.maxMove.Length(), 0.5f, "옆으로 걷는다");
            Assert.AreEqual(0f, Vector2.Dot(Vector2.Normalize(r.maxMove), new Vector2(1f, 0f)), 0.1f, "둘레길 접선 방향");
        }

        [Test]
        public void MoleInLane_Waits()
        {
            var s = NewState();
            s.Cards.Add(Card(1, new Vector2(3f, 0f), true));
            s.Moles.Add(new TriGrabMole { Id = 1, Pos = new Vector2(6f, 0f), SpawnTick = Now - 10, StandTicks = 500 });
            var r = Run(new TriGrabBotBrain(1), s);
            Assert.AreEqual(0, r.fires, "두더지가 서 있으면 기다린다");
            Assert.AreEqual(0f, r.maxMove.Length(), 1e-4f, "기다리는 동안 안 움직인다");

            s.Moles.Clear();
            Assert.Greater(Run(new TriGrabBotBrain(1), s).fires, 0, "두더지가 없으면 쏜다");
        }

        [Test]
        public void IgnoresCarriedAndUnpoppedCards()
        {
            var s = NewState();
            var carried = Card(1, new Vector2(3f, 0f), true);
            carried.CarriedBy = "other";
            var fresh = Card(2, new Vector2(3f, 1f), true);
            fresh.BornTick = Now;
            s.Cards.Add(carried);
            s.Cards.Add(fresh);
            Assert.AreEqual(0, Run(new TriGrabBotBrain(1), s).fires);
        }

        [Test]
        public void DoesNotFire_WhileHookOut_Stunned_OrCooling()
        {
            var s = NewState();
            s.Cards.Add(Card(1, new Vector2(3f, 0f), true));
            s.Hooks.Add(new TriGrabHook { Owner = "bot" });
            Assert.AreEqual(0, Run(new TriGrabBotBrain(1), s).fires, "손이 나가 있음");
            s.Hooks.Clear();
            s.Players["bot"].StunUntilTick = Now + 1000;
            Assert.AreEqual(0, Run(new TriGrabBotBrain(1), s).fires, "기절");
            s.Players["bot"].StunUntilTick = 0;
            s.Players["bot"].CooldownUntilTick = Now + 1000;
            Assert.AreEqual(0, Run(new TriGrabBotBrain(1), s).fires, "쿨다운");
        }

        [Test]
        public void FarTarget_WalksAlongLane()
        {
            var s = NewState();
            s.Cards.Add(Card(1, new Vector2(0f, 4.4f), true));
            var brain = new TriGrabBotBrain(1);
            var mm = Vector2.Zero;
            for (int i = 0; i < 200; i++)
            {
                brain.Think("bot", new Vector2(0f, -9f), Now, s, out var move, out _);
                if (move.Length() > mm.Length()) mm = move;
            }
            Assert.Greater(mm.Length(), 0.5f, "사거리 밖이면 다가간다");
        }

        [Test]
        public void FourBots_FullMatch_Ends_ScoresPositive()
        {
            var match = new TriGrabMatch(42);
            var ids = new[] { "b0", "b1", "b2", "b3" };
            var pos = new Dictionary<string, Vector2>();
            var brains = new Dictionary<string, TriGrabBotBrain>();
            for (int i = 0; i < ids.Length; i++)
            {
                match.AddPlayer(ids[i]);
                float a = i * (float)Math.PI / 2f;
                pos[ids[i]] = new Vector2((float)Math.Cos(a), (float)Math.Sin(a)) * 7.8f;
                brains[ids[i]] = new TriGrabBotBrain(100 + i);
            }
            match.Start(0);
            long t = 0;
            const int Cap = 12000;
            for (; t < Cap && match.State.Phase != TriGrabPhase.Over; )
            {
                t++;
                foreach (var id in ids)
                {
                    brains[id].Think(id, pos[id], t, match.State, out var move, out var fire);
                    var sc = match.State.Players[id];
                    if (fire.HasValue) match.TryFire(id, pos[id], fire.Value, t);
                    if (t >= sc.StunUntilTick && match.HookOf(id) == null && move.LengthSquared() > 0f)
                    {
                        var p = pos[id] + move * TriGrabConfig.MoveSpeed * TriGrabConfig.TickSeconds;
                        float r = p.Length();
                        float cr = Math.Max(TriGrabConfig.LaneMin, Math.Min(TriGrabConfig.LaneMax, r));
                        pos[id] = p * (cr / r);
                    }
                }
                match.Tick(t, id => pos[id]);
            }
            Assert.AreEqual(TriGrabPhase.Over, match.State.Phase, "판이 상한 안에 끝난다");
            float seconds = t * TriGrabConfig.TickSeconds;
            double avg = 0;
            foreach (var id in ids) avg += match.State.Players[id].Score;
            avg /= ids.Length;
            TestContext.Out.WriteLine($"[bot-match] ticks={t} seconds={seconds:F1} avgScore={avg:F2} scores={string.Join(",", Array.ConvertAll(ids, i => match.State.Players[i].Score))}");
            Assert.That(seconds, Is.InRange(60f, 120f), "길이");
            Assert.Greater(avg, 3.0, "봇 평균 점수");
        }
    }
}
