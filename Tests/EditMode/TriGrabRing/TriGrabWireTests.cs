using System.Numerics;
using NUnit.Framework;

namespace LOP.TriGrab.Tests
{
    public class TriGrabWireTests
    {
        [Test]
        public void ToWire_RoundTripsCardsMolesHooksPlayers()
        {
            var m = new TriGrabMatch(1);
            m.AddPlayer("a"); m.AddPlayer("b");
            var s = m.State;
            s.Phase = TriGrabPhase.Live; s.PhaseEndTick = 900; s.Round = 3; s.TotalRounds = 5;
            s.Rule = new TriGrabRule(TriGrabRuleType.Match, 2, 1);
            s.LiveStartTick = 400; s.Quota = 7; s.OkSpawned = 4; s.Version = 12;
            s.Cards.Add(new TriGrabCard { Id = 9, Word = 1, Ink = 2, Fill = 3, Ok = true, BornTick = 410, CarriedBy = "a" });
            s.Cards.Add(new TriGrabCard { Id = 10, Word = 0, Ink = 1, Fill = 0, BornTick = 420 });
            s.Hooks.Add(new TriGrabHook
            {
                Owner = "a", FireTick = 500, Origin = new Vector2(1, 2), Dir = new Vector2(0, 1),
                State = TriGrabHookState.Back, End = TriGrabHookEnd.Card, CardId = 9,
                TurnTick = 510, TurnLength = 4.5f, Tip = new Vector2(1.5f, 3.5f),
            });
            s.Moles.Add(new TriGrabMole { Id = 5, Pos = new Vector2(-2, 3), SpawnTick = 450, StandTicks = 70, BonkTick = 480, SayIndex = 2, BonkLineIndex = 4 });
            s.Players["a"].Score = 3; s.Players["a"].Correct = 4; s.Players["a"].Wrong = 1;
            s.Players["a"].StunUntilTick = 600; s.Players["a"].CooldownUntilTick = 620;
            s.Fx.Add(new TriGrabFx { Kind = TriGrabFxKind.MoleBonk, Pos = new Vector2(7, 8), Owner = "b", LineIndex = 3 });

            var w = TriGrabWire.ToWire(m, 777);

            Assert.AreEqual(12, w.Version);
            Assert.AreEqual((int)TriGrabPhase.Live, w.Phase);
            Assert.AreEqual(900, w.PhaseEndTick);
            Assert.AreEqual(3, w.Round);
            Assert.AreEqual(5, w.TotalRounds);
            Assert.AreEqual((int)TriGrabRuleType.Match, w.RuleType);
            Assert.AreEqual(2, w.RuleValue);
            Assert.AreEqual(1, w.RuleLure);
            Assert.AreEqual(400, w.LiveStartTick);
            Assert.AreEqual(7, w.Quota);
            Assert.AreEqual(4, w.OkSpawned);

            Assert.AreEqual(2, w.Cards.Count);
            Assert.AreEqual(9, w.Cards[0].Id);
            Assert.AreEqual(3, w.Cards[0].Fill);
            Assert.IsTrue(w.Cards[0].Ok);
            Assert.AreEqual(410, w.Cards[0].BornTick);
            Assert.AreEqual("a", w.Cards[0].CarriedBy);
            Assert.AreEqual("", w.Cards[1].CarriedBy);

            Assert.AreEqual(1, w.Hooks.Count);
            var h = w.Hooks[0];
            Assert.AreEqual("a", h.OwnerId);
            Assert.AreEqual(500, h.FireTick);
            Assert.AreEqual(2f, h.OriginZ);
            Assert.AreEqual(1f, h.DirZ);
            Assert.AreEqual((int)TriGrabHookState.Back, h.State);
            Assert.AreEqual((int)TriGrabHookEnd.Card, h.End);
            Assert.AreEqual(9, h.CardId);
            Assert.AreEqual(510, h.TurnTick);
            Assert.AreEqual(4.5f, h.TurnLength);
            Assert.AreEqual(1.5f, h.TipX);
            Assert.AreEqual(3.5f, h.TipZ);

            Assert.AreEqual(1, w.Moles.Count);
            var mo = w.Moles[0];
            Assert.AreEqual(5, mo.Id);
            Assert.AreEqual(-2f, mo.X);
            Assert.AreEqual(3f, mo.Z);
            Assert.AreEqual(70, mo.StandTicks);
            Assert.AreEqual(480, mo.BonkTick);
            Assert.AreEqual(2, mo.Say);
            Assert.AreEqual(4, mo.BonkLine);

            Assert.AreEqual(2, w.Players.Count);
            Assert.AreEqual("a", w.Players[0].EntityId);
            Assert.AreEqual(3, w.Players[0].Score);
            Assert.AreEqual(4, w.Players[0].Correct);
            Assert.AreEqual(1, w.Players[0].Wrong);
            Assert.AreEqual(600, w.Players[0].StunUntilTick);
            Assert.AreEqual(620, w.Players[0].CooldownUntilTick);
            Assert.AreEqual(1, w.Players[0].Place);
            Assert.AreEqual(2, w.Players[1].Place);

            Assert.AreEqual(1, w.Fx.Count);
            Assert.AreEqual((int)TriGrabFxKind.MoleBonk, w.Fx[0].Kind);
            Assert.AreEqual(7f, w.Fx[0].X);
            Assert.AreEqual(8f, w.Fx[0].Z);
            Assert.AreEqual("b", w.Fx[0].OwnerId);
            Assert.AreEqual(3, w.Fx[0].Line);
            Assert.AreEqual(777, w.Fx[0].Tick);

            var back = new TriGrabRingStateToC();
            back.Deserialize(w.Serialize());
            Assert.AreEqual(w, back);
        }

        [Test]
        public void CardsToWire_IncludesCarried_ExcludesGone()
        {
            var s = new TriGrabMatchState();
            s.Cards.Add(new TriGrabCard { Id = 1, Pos = new Vector2(1, 2) });
            s.Cards.Add(new TriGrabCard { Id = 2, Pos = new Vector2(3, 4), CarriedBy = "a" });
            s.Cards.Add(new TriGrabCard { Id = 3, Pos = new Vector2(5, 6), Gone = true });

            var w = TriGrabWire.CardsToWire(s, 42);

            Assert.AreEqual(42, w.Tick);
            CollectionAssert.AreEqual(new[] { 1, 2 }, w.Ids);
            CollectionAssert.AreEqual(new[] { 1f, 3f }, w.Xs);
            CollectionAssert.AreEqual(new[] { 2f, 4f }, w.Zs);
        }
    }
}
