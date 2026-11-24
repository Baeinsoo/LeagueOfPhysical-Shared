using System.Numerics;
using NUnit.Framework;

namespace LOP.TriGrab.Tests
{
    public class TriGrabMatchTests
    {
        static readonly Vector2 BodyA = new Vector2(8f, 0f);
        // 카드 가까이에 서 있어야 B의 손이 끌려가는 카드와 겹친다.
        static readonly Vector2 BodyB = new Vector2(1f, 0f);
        const float YawMinusX = 270f;
        const float YawPlusX = 90f;

        TriGrabMatch _m;
        long _t;

        static Vector2 BodyOf(string id) => id == "a" ? BodyA : BodyB;

        // 두더지가 길을 막지 않게 매 틱 비운다(두더지는 MolesTests가 따로 본다).
        void Step(int n = 1, bool clearMoles = true)
        {
            for (int i = 0; i < n; i++)
            {
                _t++;
                if (clearMoles) _m.State.Moles.Clear();
                _m.Tick(_t, BodyOf);
            }
        }

        void Setup(int players = 2)
        {
            _m = new TriGrabMatch(7);
            _m.AddPlayer("a");
            _m.AddPlayer("b");
            for (int i = 2; i < players; i++) _m.AddPlayer("p" + i);
            _t = 0;
            _m.Start(_t);
        }

        void ToLive()
        {
            for (int i = 0; i < 100 && _m.State.Phase != TriGrabPhase.Live; i++) Step();
            Assert.AreEqual(TriGrabPhase.Live, _m.State.Phase);
        }

        // Live 시작 직후 카드 7장을 시험용으로 고정한다: 첫 장은 (3,0)의 목표, 나머지는 손 길(y=0)에서 먼 곳.
        TriGrabCard SetupTarget(bool ok)
        {
            ToLive();
            var s = _m.State;
            s.Quota = 99;
            var spots = new[] { new Vector2(3f, 0f), new Vector2(-3f, 3f), new Vector2(0f, 3.4f), new Vector2(3f, 3f), new Vector2(-3f, -3f), new Vector2(0f, -3.4f), new Vector2(3f, -3f) };
            Assert.AreEqual(7, s.Cards.Count, "시작 카드 수");
            for (int i = 0; i < s.Cards.Count; i++)
            {
                var c = s.Cards[i];
                c.Pos = spots[i];
                c.Vel = Vector2.Zero;
                c.BornTick = -100;
                // 맞는 카드를 둘 이상 둬서 보충이 새 카드를 만들지 않게 한다.
                c.Ok = i == 0 ? ok : i <= 2;
            }
            return s.Cards[0];
        }

        [Test]
        public void Round_FlowsPrepLiveResolved()
        {
            Setup();
            Assert.AreEqual(TriGrabPhase.Prep, _m.State.Phase);
            Assert.AreEqual(1, _m.State.Round);
            long liveAt = -1, resolvedAt = -1, nextPrepAt = -1;
            for (int i = 0; i < 700 && nextPrepAt < 0; i++)
            {
                var before = _m.State.Phase;
                Step();
                var after = _m.State.Phase;
                if (before == TriGrabPhase.Prep && after == TriGrabPhase.Live) liveAt = _t;
                if (before == TriGrabPhase.Live && after == TriGrabPhase.Resolved) resolvedAt = _t;
                if (before == TriGrabPhase.Resolved && after == TriGrabPhase.Prep) nextPrepAt = _t;
            }
            Assert.AreEqual(TriGrabConfig.AskTicks, liveAt);
            Assert.AreEqual(liveAt + TriGrabConfig.LiveLimitTicks, resolvedAt);
            Assert.AreEqual(resolvedAt + TriGrabConfig.ResolvedTicks, nextPrepAt);
            Assert.AreEqual(2, _m.State.Round);
        }

        [Test]
        public void Correct_PlusOne()
        {
            Setup();
            var card = SetupTarget(true);
            Assert.IsTrue(_m.TryFire("a", BodyA, YawMinusX, _t));
            Step(80);
            var p = _m.State.Players["a"];
            Assert.IsTrue(card.Gone);
            Assert.AreEqual(1, p.Score);
            Assert.AreEqual(1, p.Correct);
            Assert.AreEqual(0, p.Wrong);
            Assert.IsNull(_m.HookOf("a"));
        }

        [Test]
        public void Wrong_MinusOneAndStun()
        {
            Setup();
            var card = SetupTarget(false);
            _m.TryFire("a", BodyA, YawMinusX, _t);
            for (int i = 0; i < 80 && !card.Gone; i++) Step();
            var p = _m.State.Players["a"];
            Assert.IsTrue(card.Gone);
            Assert.AreEqual(-1, p.Score);
            Assert.AreEqual(1, p.Wrong);
            Assert.AreEqual(_t + TriGrabConfig.StunTicks, p.StunUntilTick);
        }

        [Test]
        public void Stun_BlocksFireForOneSecond()
        {
            Setup();
            var card = SetupTarget(false);
            _m.TryFire("a", BodyA, YawMinusX, _t);
            for (int i = 0; i < 80 && !card.Gone; i++) Step();
            long until = _m.State.Players["a"].StunUntilTick;
            Assert.AreEqual(TriGrabPhase.Live, _m.State.Phase);
            Assert.IsFalse(_m.TryFire("a", BodyA, YawMinusX, until - 1), "기절 중");
            Assert.IsTrue(_m.TryFire("a", BodyA, YawMinusX, until), "기절이 끝나면 쏜다");
        }

        [Test]
        public void Cooldown_BlocksRefireBriefly()
        {
            Setup();
            var card = SetupTarget(true);
            _m.TryFire("a", BodyA, YawMinusX, _t);
            for (int i = 0; i < 80 && !card.Gone; i++) Step();
            long cd = _m.State.Players["a"].CooldownUntilTick;
            Assert.IsFalse(_m.TryFire("a", BodyA, YawMinusX, cd - 1));
            Assert.IsTrue(_m.TryFire("a", BodyA, YawMinusX, cd));
        }

        [Test]
        public void Fire_ThenSecondFire_RejectedWhileHookAlive()
        {
            Setup();
            SetupTarget(true);
            Assert.IsTrue(_m.TryFire("a", BodyA, YawMinusX, _t));
            Assert.IsFalse(_m.TryFire("a", BodyA, YawMinusX, _t));
        }

        [Test]
        public void Fire_IgnoredOutsideLive()
        {
            Setup();
            Assert.IsFalse(_m.TryFire("a", BodyA, YawMinusX, _t), "Prep");
            ToLive();
            Assert.IsTrue(_m.TryFire("a", BodyA, YawMinusX, _t), "Live(양성 대조)");
            for (int i = 0; i < 1000 && _m.State.Phase != TriGrabPhase.Resolved; i++) Step();
            Assert.IsFalse(_m.TryFire("b", BodyB, YawPlusX, _t), "Resolved");
        }

        [Test]
        public void RoundEnd_DropsHooksWithoutScore()
        {
            Setup();
            var card = SetupTarget(true);
            _m.TryFire("a", BodyA, YawMinusX, _t);
            for (int i = 0; i < 80 && _m.HookOf("a").State != TriGrabHookState.Back; i++) Step();
            Assert.AreEqual("a", card.CarriedBy);
            _m.State.PhaseEndTick = _t + 1;
            Step();
            Assert.AreEqual(TriGrabPhase.Resolved, _m.State.Phase);
            Assert.AreEqual(0, _m.State.Hooks.Count);
            Assert.AreEqual(0, _m.State.Players["a"].Score);
            Assert.IsNull(card.CarriedBy);
            Assert.IsTrue(card.Gone);
        }

        [Test]
        public void Inactive_CannotFire_StaysRanked()
        {
            Setup();
            ToLive();
            _m.SetActive("a", false);
            Assert.IsFalse(_m.TryFire("a", BodyA, YawMinusX, _t));
            _m.SetActive("a", true);
            Assert.IsTrue(_m.TryFire("a", BodyA, YawMinusX, _t), "다시 켜면 쏜다(양성 대조)");
            _m.SetActive("a", false);
            var ids = new System.Collections.Generic.List<string>();
            foreach (var pl in _m.Placements()) ids.Add(pl.id);
            CollectionAssert.Contains(ids, "a");
        }

        [Test]
        public void Quota_EndsRoundWhenAllOkTaken()
        {
            Setup();
            var card = SetupTarget(true);
            _m.State.Quota = 1;
            _m.State.OkSpawned = 1;
            for (int i = 1; i < _m.State.Cards.Count; i++) _m.State.Cards[i].Ok = false;
            _m.TryFire("a", BodyA, YawMinusX, _t);
            for (int i = 0; i < 80 && _m.State.Phase == TriGrabPhase.Live; i++) Step();
            Assert.IsTrue(card.Gone);
            Assert.AreEqual(TriGrabPhase.Resolved, _m.State.Phase);
            Assert.Less(_t, TriGrabConfig.AskTicks + TriGrabConfig.LiveLimitTicks);
            Assert.AreEqual(1, _m.State.Players["a"].Score);
        }

        [Test]
        public void Quota_DoesNotEndWhileOkCardRemains()
        {
            Setup();
            SetupTarget(true);
            _m.State.Quota = 1;
            _m.State.OkSpawned = 1;
            Step(30);
            Assert.AreEqual(TriGrabPhase.Live, _m.State.Phase);
        }

        [Test]
        public void TenRounds_ThenOver()
        {
            Setup();
            int rounds = 0, lastRound = 1;
            for (int i = 0; i < 6000 && _m.State.Phase != TriGrabPhase.Over; i++)
            {
                Step();
                if (_m.State.Round != lastRound) { rounds++; lastRound = _m.State.Round; }
            }
            Assert.AreEqual(TriGrabPhase.Over, _m.State.Phase);
            Assert.AreEqual(TriGrabConfig.TotalRounds, _m.State.Round);
            Assert.AreEqual(TriGrabConfig.TotalRounds - 1, rounds, "라운드 번호가 바뀐 횟수(2~10)");
        }

        [Test]
        public void Placements_TiesShare()
        {
            _m = new TriGrabMatch(1);
            _m.AddPlayer("a"); _m.AddPlayer("b"); _m.AddPlayer("c");
            _m.State.Players["a"].Score = 3;
            _m.State.Players["b"].Score = 3;
            _m.State.Players["c"].Score = 1;
            var r = _m.Placements();
            Assert.AreEqual(3, r.Count);
            foreach (var x in r)
                Assert.AreEqual(x.id == "c" ? 3 : 1, x.place, x.id);
        }

        [Test]
        public void CarriedCard_NotOnDisc()
        {
            Setup();
            var card = SetupTarget(true);
            _m.TryFire("a", BodyA, YawMinusX, _t);
            for (int i = 0; i < 80 && _m.HookOf("a").State != TriGrabHookState.Back; i++) Step();
            Assert.AreEqual("a", card.CarriedBy);
            Assert.IsTrue(_m.TryFire("b", BodyB, YawPlusX, _t));
            var hb = _m.HookOf("b");
            for (int i = 0; i < 80 && hb.State == TriGrabHookState.Out; i++)
            {
                Step();
                Assert.AreEqual("a", card.CarriedBy == null ? "a" : card.CarriedBy, "B가 끌려가는 카드를 가로채면 안 된다");
            }
            Assert.AreEqual(TriGrabHookEnd.Range, hb.End);
            Assert.AreEqual(-1, hb.CardId);
        }

        [Test]
        public void Version_BumpsOnGrabAndScore()
        {
            Setup();
            var card = SetupTarget(true);
            int v0 = _m.State.Version;
            Step();
            Assert.AreEqual(v0, _m.State.Version, "위치만 변하면 안 오른다");
            _m.TryFire("a", BodyA, YawMinusX, _t);
            int v1 = _m.State.Version;
            Assert.Greater(v1, v0, "손 시작");
            for (int i = 0; i < 80 && card.CarriedBy == null; i++) Step();
            int v2 = _m.State.Version;
            Assert.Greater(v2, v1, "잡음");
            for (int i = 0; i < 80 && !card.Gone; i++) Step();
            Assert.Greater(_m.State.Version, v2, "점수");
        }
    }
}
