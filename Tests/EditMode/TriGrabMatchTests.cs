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
        public void 둘이면_첫_정답에_10점_라운드가_끝나고_회전판이_멈춘다()
        {
            var m = LiveMatch(1, out long t, A, B);
            m.State.Slots[0].Card = RedInk();

            Assert.IsTrue(m.TryFire(A, Body, YawIn, t));
            long grab = -1;
            for (long k = t + 1; k < t + 40 && m.State.Phase == TriGrabPhase.Live; k++) { m.Tick(k); grab = k; }

            Assert.AreEqual(TriGrabPhase.Resolved, m.State.Phase, "2명이면 1명 남을 때 끝난다");
            Assert.AreEqual(10, m.State.Players[A].Score);
            Assert.AreEqual(1, m.State.Players[A].RoundPlace);
            Assert.AreEqual(10, m.State.Players[A].RoundPoints);
            Assert.AreEqual(0, m.State.Players[B].Score);
            Assert.AreEqual(0, m.State.Players[B].RoundPlace);
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
        public void 라운드가_남았으면_쉼이_끝나고_다음_라운드_준비로_간다()
        {
            var m = LiveMatch(9, out long t, A);
            m.State.Slots[0].Card = RedInk();
            Assert.IsTrue(m.TryFire(A, Body, YawIn, t));
            long k = t;
            while (m.State.Phase == TriGrabPhase.Live && k < t + 2000) m.Tick(++k);
            long end = m.State.PhaseEndTick;
            float frozenBase = m.State.Clock.BaseAt(k);
            m.Tick(end);
            Assert.AreEqual(TriGrabPhase.Prep, m.State.Phase);
            Assert.AreEqual(2, m.State.Round);
            Assert.AreEqual(0, m.State.Hooks.Count);
            Assert.AreEqual(frozenBase, m.State.Clock.BaseAngle0, 1e-5f, "앞 라운드가 멈춘 각에서 이어진다");
            Assert.Less(m.State.Clock.Omega, 0f, "짝수 라운드는 반대로 돈다");
            Assert.AreEqual(end + TriGrabConfig.PrepTicks, m.State.Clock.LiveStartTick);
            Assert.AreEqual(0, m.State.Players[A].RoundPlace, "새 라운드엔 등수가 지워진다");
            Assert.AreEqual(0, m.State.Players[A].RoundPoints);
            Assert.AreEqual(10, m.State.Players[A].Score, "점수는 누적");
            Assert.AreEqual(m.State.Clock.LiveStartTick + TriGrabConfig.RoundLimitTicks, m.State.RoundLimitTick);
        }

        // ── 2차: 순위 점수 누적 ──

        // 슬롯 i 바로 바깥(반지름 8)에 서서 그 슬롯을 향해 쏘는 자리와 방향(Omega 0 기준).
        static Vector2 BodyFacing(int slot) => TriGrabCarousel.SlotPosition(slot, 0f) * 2f;
        static float YawToward(int slot) => TriGrabMath.YawFromDir(-BodyFacing(slot));

        static string[] Ids(int n)
        {
            var ids = new string[n];
            for (int i = 0; i < n; i++) ids[i] = "p" + i;
            return ids;
        }

        // n명이 한 명씩 자기 앞 슬롯의 정답 카드를 쏜다(2틱 간격). 라운드가 끝나면 멈춘다. 반환: 라운드가 끝난 틱(안 끝나면 -1)
        static long EveryoneGrabsInOrder(TriGrabMatch m, long t, string[] ids)
        {
            for (int i = 0; i < ids.Length; i++) m.State.Slots[i].Card = RedInk(100 + i);
            int next = 0;
            for (long k = t; k < t + 200; k++)
            {
                if (next < ids.Length && (k - t) % 2 == 0)
                {
                    Assert.IsTrue(m.TryFire(ids[next], BodyFacing(next), YawToward(next), k), "쏘기 " + ids[next]);
                    next++;
                }
                m.Tick(k + 1);
                if (m.State.Phase != TriGrabPhase.Live) return k + 1;
            }
            return -1;
        }

        [Test]
        public void 정답_순서대로_10_9_8점을_받는다()
        {
            var ids = Ids(4);
            var m = LiveMatch(20, out long t, ids);
            long end = EveryoneGrabsInOrder(m, t, ids);
            Assert.Greater(end, 0, "라운드가 끝났다");
            var p = m.State.Players;
            Assert.AreEqual(new[] { 1, 2, 3, 0 }, new[] { p["p0"].RoundPlace, p["p1"].RoundPlace, p["p2"].RoundPlace, p["p3"].RoundPlace });
            Assert.AreEqual(new[] { 10, 9, 8, 0 }, new[] { p["p0"].Score, p["p1"].Score, p["p2"].Score, p["p3"].Score });
            Assert.AreEqual(new[] { 10, 9, 8, 0 }, new[] { p["p0"].RoundPoints, p["p1"].RoundPoints, p["p2"].RoundPoints, p["p3"].RoundPoints });
            Assert.AreEqual("p0", m.State.RoundWinnerId, "라운드 1등");
            Assert.AreEqual(3, m.State.RoundCorrectCount);
        }

        [Test]
        public void 등수_점수는_11_빼기_등수_최소_1()
        {
            Assert.AreEqual(10, TriGrabConfig.PlacePoints(1));
            Assert.AreEqual(9, TriGrabConfig.PlacePoints(2));
            Assert.AreEqual(4, TriGrabConfig.PlacePoints(7));
            Assert.AreEqual(1, TriGrabConfig.PlacePoints(10));
            Assert.AreEqual(1, TriGrabConfig.PlacePoints(30));
        }

        [TestCase(2, 1)]
        [TestCase(4, 3)]
        [TestCase(6, 5)]
        [TestCase(8, 6)]
        public void 남은_사람이_인원의_4분의_1_이하면_라운드가_끝난다(int n, int correctToEnd)
        {
            var ids = Ids(n);
            var m = LiveMatch(30 + n, out long t, ids);
            long end = EveryoneGrabsInOrder(m, t, ids);
            Assert.Greater(end, 0);
            Assert.AreEqual(TriGrabPhase.Resolved, m.State.Phase);
            Assert.AreEqual(correctToEnd, m.State.RoundCorrectCount, $"{n}명 — 정답 {correctToEnd}명에서 끝");
            Assert.AreEqual(end, m.State.Clock.FreezeTick, "라운드 끝에 회전판이 멈춘다");
            int scored = 0;
            foreach (var id in ids) if (m.State.Players[id].RoundPlace > 0) scored++;
            Assert.AreEqual(correctToEnd, scored);
        }

        [Test]
        public void 첫_정답_뒤에도_라운드는_이어지고_회전판은_돈다()
        {
            var m = LiveMatch(21, out long t, Ids(4));
            m.State.Slots[0].Card = RedInk();
            Assert.IsTrue(m.TryFire("p0", BodyFacing(0), YawToward(0), t));
            long k = t;
            while (m.State.Players["p0"].RoundPlace == 0 && k < t + 40) m.Tick(++k);
            Assert.AreEqual(1, m.State.Players["p0"].RoundPlace, "양성 대조 — 정답이 났다");
            Assert.AreEqual(TriGrabPhase.Live, m.State.Phase, "4명 중 1명만 정답 — 계속");
            Assert.AreEqual(long.MaxValue, m.State.Clock.FreezeTick, "회전판은 멈추지 않는다");
            Assert.IsFalse(m.State.Slots[0].Card.HasValue, "정답 카드는 슬롯에서 빠진다");
            Assert.AreEqual(k + TriGrabConfig.RefillTicks, m.State.Slots[0].EmptyUntilTick, "35틱 뒤 채운다");
            for (long j = k + 1; j <= k + TriGrabConfig.RefillTicks; j++) m.Tick(j);
            Assert.IsTrue(m.State.Slots[0].Card.HasValue, "채워졌다");
        }

        [Test]
        public void 정답을_낸_사람은_그_라운드에_더_쏘지_못한다()
        {
            var m = LiveMatch(22, out long t, Ids(4));
            m.State.Slots[0].Card = RedInk();
            long k = FireAndRun(m, "p0", t, BodyFacing(0), YawToward(0));
            Assert.AreEqual(1, m.State.Players["p0"].RoundPlace);
            Assert.AreEqual(TriGrabPhase.Live, m.State.Phase);
            Assert.IsNull(m.HookOf("p0"));
            Assert.LessOrEqual(m.State.Players["p0"].ReloadUntilTick, k, "재장전은 끝났다");
            Assert.IsFalse(m.TryFire("p0", BodyFacing(0), 90f, k), "이번 라운드엔 끝");
            Assert.IsTrue(m.TryFire("p1", BodyFacing(1), 90f, k), "양성 대조 — 아직 못 낸 사람은 쏜다");
        }

        [Test]
        public void 정답을_낸_사람은_다음_라운드에_다시_쏜다()
        {
            var m = LiveMatch(23, out long t, A);
            m.State.Slots[0].Card = RedInk();
            Assert.IsTrue(m.TryFire(A, Body, YawIn, t));
            long k = t;
            while (m.State.Phase != TriGrabPhase.Prep && k < t + 2000) m.Tick(++k);
            while (m.State.Phase != TriGrabPhase.Live && k < t + 2000) m.Tick(++k);
            Assert.AreEqual(2, m.State.Round);
            Assert.IsTrue(m.TryFire(A, Body, 90f, k));
        }

        [Test]
        public void 정답을_남은_사람을_위해_정답_카드가_계속_나온다()
        {
            var m = LiveMatch(24, out long t, Ids(4));
            foreach (var sl in m.State.Slots) sl.Card = BlueInk();
            m.State.Slots[0].Card = RedInk();
            long k = FireAndRun(m, "p0", t, BodyFacing(0), YawToward(0));
            Assert.AreEqual(1, m.State.Players["p0"].RoundPlace);
            bool seen = false;
            for (long j = k + 1; j <= k + TriGrabConfig.ForceValidAfterTicks + 5 && !seen; j++)
            {
                m.Tick(j);
                foreach (var sl in m.State.Slots) if (sl.Card.HasValue && m.State.Rule.Matches(sl.Card.Value, j)) seen = true;
            }
            Assert.IsTrue(seen, "남은 사람이 잡을 정답 카드가 다시 생긴다");
            Assert.AreEqual(TriGrabPhase.Live, m.State.Phase);
        }

        [Test]
        public void Live가_1000틱_지나면_남은_사람은_0점으로_라운드가_끝난다()
        {
            var m = LiveMatch(25, out long t, A, B);
            long limit = m.State.RoundLimitTick;
            Assert.AreEqual(m.State.Clock.LiveStartTick + TriGrabConfig.RoundLimitTicks, limit);
            for (long k = t + 1; k < limit; k++) m.Tick(k);
            Assert.AreEqual(TriGrabPhase.Live, m.State.Phase, "상한 한 틱 전엔 Live");
            Assert.IsTrue(m.TryFire(A, Body, 90f, limit - 1), "양성 대조 — 상한 전엔 쏜다");
            m.Tick(limit);
            Assert.AreEqual(TriGrabPhase.Resolved, m.State.Phase);
            Assert.AreEqual(limit, m.State.Clock.FreezeTick);
            Assert.AreEqual(limit + TriGrabConfig.ResolvedTicks, m.State.PhaseEndTick);
            Assert.AreEqual(0, m.State.Players[A].Score);
            Assert.AreEqual(0, m.State.Players[B].RoundPoints);
        }

        // ── 리뷰 수정: 경합·나간 사람·같은 틱 ──

        // 슬롯 0 (4,0)과 슬롯 3 (135°) 을 잇는 현 위, 슬롯 0 앞 2.5 m에 서서 슬롯 3 쪽으로 쏜다.
        // 슬롯 0 카드가 남아 있으면 그걸 잡고, 없으면 그대로 뻗어 9.7 m 끝에서 슬롯 3 카드(9.89 m)에 닿는다.
        static void ChordShot(out Vector2 body, out float yaw)
        {
            Vector2 a = TriGrabCarousel.SlotPosition(0, 0f), b = TriGrabCarousel.SlotPosition(3, 0f);
            Vector2 d = (b - a).normalized;
            body = a - d * 2.5f;
            yaw = TriGrabMath.YawFromDir(d);
        }

        [Test]
        public void 겨눈_카드를_남이_먼저_가져가면_뒤의_카드를_집지_않고_빈손으로_돌아온다()
        {
            ChordShot(out var body, out float yaw);
            var m = LiveMatch(40, out long t, Ids(4));
            m.State.Slots[0].Card = RedInk(1);
            m.State.Slots[3].Card = BlueInk(3);   // 오답 — 옛 규칙이면 p1이 이걸 집어 −1
            Assert.IsTrue(m.TryFire("p0", body, yaw, t));
            Assert.IsTrue(m.TryFire("p1", body, yaw, t + 1));
            Assert.AreEqual(1, m.HookOf("p1").TargetCardId, "쏜 순간 처음 닿을 카드 = 슬롯 0");
            long k = t + 1;
            while (m.HookOf("p1") != null && k < t + 80) m.Tick(++k);
            Assert.AreEqual(1, m.State.Players["p0"].RoundPlace, "양성 대조 — p0이 먼저 가져갔다");
            Assert.AreEqual(0, m.State.Players["p1"].Mistakes, "남이 가져간 카드 뒤의 카드를 집지 않는다");
            Assert.AreEqual(0, m.State.Players["p1"].RoundPlace);
            Assert.IsTrue(m.State.Slots[3].Card.HasValue, "뒤의 카드는 그대로");
        }

        [Test]
        public void 겨눈_카드가_사라지면_그_자리에서_꺾어_빈손_재장전()
        {
            ChordShot(out var body, out float yaw);
            var m = LiveMatch(41, out long t, Ids(4));
            m.State.Slots[0].Card = RedInk(1);
            Assert.IsTrue(m.TryFire("p1", body, yaw, t));
            m.Tick(t + 1);
            m.State.Slots[0].Card = null;   // 누가 가져갔다
            m.Tick(t + 2);
            var hook = m.HookOf("p1");
            Assert.AreEqual(t + 2, hook.TurnTick, "사라진 틱에 꺾는다");
            Assert.IsFalse(hook.Carried.HasValue);
            Assert.AreEqual(hook.EndTick + TriGrabConfig.ReloadMissTicks, m.State.Players["p1"].ReloadUntilTick);
        }

        [Test]
        public void 겨눈_카드가_그대로면_잡는다_양성_대조()
        {
            ChordShot(out var body, out float yaw);
            var m = LiveMatch(42, out long t, Ids(4));
            m.State.Slots[0].Card = RedInk(1);
            m.State.Slots[3].Card = BlueInk(3);
            FireAndRun(m, "p1", t, body, yaw);
            Assert.AreEqual(1, m.State.Players["p1"].RoundPlace);
            Assert.IsTrue(m.State.Slots[3].Card.HasValue);
        }

        [Test]
        public void 닿을_카드가_없던_갈고리는_나중에_들어온_카드도_안_잡는다()
        {
            var m = LiveMatch(43, out long t, Ids(4));
            Assert.IsTrue(m.TryFire("p0", Body, YawIn, t));
            Assert.AreEqual(0, m.HookOf("p0").TargetCardId);
            m.State.Slots[0].Card = RedInk(1);   // 쏜 뒤에 들어온 카드
            for (long k = t + 1; k <= t + TriGrabConfig.ReachTicks; k++) m.Tick(k);
            Assert.AreEqual(0, m.State.Players["p0"].RoundPlace);
            Assert.IsTrue(m.State.Slots[0].Card.HasValue);
        }

        [Test]
        public void 나간_사람은_끝난_쪽으로_센다_8명_중_3명이_나가면_3명_정답에서_끝()
        {
            var ids = Ids(8);
            var m = LiveMatch(44, out long t, ids);
            foreach (var id in new[] { "p5", "p6", "p7" }) m.SetActive(id, false);
            long end = EveryoneGrabsInOrder(m, t, new[] { "p0", "p1", "p2", "p3", "p4" });
            Assert.Greater(end, 0);
            Assert.AreEqual(3, m.State.RoundCorrectCount, "정답 3 + 나감 3 = 6 → 남은 2 ≤ 2");
        }

        [Test]
        public void 나간_사람은_쏘지_못하고_돌아오면_쏜다()
        {
            var m = LiveMatch(45, out long t, Ids(4));
            m.SetActive("p0", false);
            Assert.IsFalse(m.TryFire("p0", Body, 90f, t));
            m.SetActive("p0", true);
            Assert.IsTrue(m.TryFire("p0", Body, 90f, t));
        }

        [Test]
        public void 라운드_도중_나가서_정원이_차면_다음_틱에_끝난다()
        {
            var m = LiveMatch(46, out long t, Ids(4));
            m.State.Slots[0].Card = RedInk();
            long k = FireAndRun(m, "p0", t, BodyFacing(0), YawToward(0));
            Assert.AreEqual(TriGrabPhase.Live, m.State.Phase, "양성 대조 — 1명 정답, 3명 남음");
            m.SetActive("p1", false);
            m.Tick(k + 1);
            Assert.AreEqual(TriGrabPhase.Live, m.State.Phase, "정답 1 + 나감 1 → 남은 2 > 1");
            m.SetActive("p2", false);
            m.Tick(k + 2);
            Assert.AreEqual(TriGrabPhase.Resolved, m.State.Phase, "남은 1 ≤ 1");
        }

        [Test]
        public void 모두_나가면_라운드가_바로_끝난다()
        {
            var m = LiveMatch(47, out long t, A, B);
            m.Tick(t);
            Assert.AreEqual(TriGrabPhase.Live, m.State.Phase, "양성 대조");
            m.SetActive(A, false); m.SetActive(B, false);
            m.Tick(t + 1);
            Assert.AreEqual(TriGrabPhase.Resolved, m.State.Phase);
        }

        [Test]
        public void 혼자인_판은_시작하자마자_끝나지_않는다()
        {
            var m = LiveMatch(48, out long t, A);
            for (long k = t; k < t + 20; k++) m.Tick(k);
            Assert.AreEqual(TriGrabPhase.Live, m.State.Phase);
        }

        [Test]
        public void 라운드를_끝내는_틱에_같이_닿은_정답도_점수를_받는다()
        {
            //  4명 판: p0·p1이 먼저 정답. p2·p3가 같은 틱에 닿으면 p2(또는 p3) 하나만으로도 라운드가 끝나지만, 둘 다 등수를 받는다.
            var m = LiveMatch(49, out long t, Ids(4));
            m.State.Slots[0].Card = RedInk(1);
            long k = FireAndRun(m, "p0", t, BodyFacing(0), YawToward(0));
            m.State.Slots[2].Card = RedInk(12);
            k = FireAndRun(m, "p1", k, BodyFacing(2), YawToward(2));
            Assert.AreEqual(TriGrabPhase.Live, m.State.Phase, "양성 대조 — 2명 정답, 아직 2명 남음");
            m.State.Slots[4].Card = RedInk(14);
            m.State.Slots[6].Card = RedInk(16);
            Assert.IsTrue(m.TryFire("p2", BodyFacing(4), YawToward(4), k));
            Assert.IsTrue(m.TryFire("p3", BodyFacing(6), YawToward(6), k));
            long j = k;
            while (m.State.Phase == TriGrabPhase.Live && j < k + 40) m.Tick(++j);
            Assert.AreEqual(TriGrabPhase.Resolved, m.State.Phase);
            Assert.AreEqual(j, m.State.Clock.FreezeTick);
            var p2 = m.State.Players["p2"]; var p3 = m.State.Players["p3"];
            CollectionAssert.AreEquivalent(new[] { 3, 4 }, new[] { p2.RoundPlace, p3.RoundPlace }, "같은 틱 둘 다 등수");
            CollectionAssert.AreEquivalent(new[] { 8, 7 }, new[] { p2.Score, p3.Score });
        }

        // 아무도 안 쏘면 라운드마다 시간 상한으로 끝난다.
        static long RunUntilOver(TriGrabMatch m, long from, out int maxRoundSeen)
        {
            maxRoundSeen = 0;
            long k = from;
            for (; k < from + TriGrabConfig.MatchCapTicks && m.State.Phase != TriGrabPhase.Over; k++)
            {
                m.Tick(k);
                maxRoundSeen = Mathf.Max(maxRoundSeen, m.State.Round);
            }
            return k;
        }

        [Test]
        public void 다섯_라운드가_끝나면_Over()
        {
            var m = new TriGrabMatch(26);
            m.AddPlayer(A); m.AddPlayer(B);
            m.Start(0);
            long k = 0;
            //  상한을 둔다 — 라운드가 안 끝나는 회귀면 무한히 돌지 않고 실패하게.
            while (!(m.State.Round == 5 && m.State.Phase == TriGrabPhase.Resolved) && k < TriGrabConfig.MatchCapTicks * 2) m.Tick(k++);
            Assert.AreEqual(TriGrabPhase.Resolved, m.State.Phase);
            long end = m.State.PhaseEndTick;
            m.Tick(end - 1);
            Assert.AreEqual(TriGrabPhase.Resolved, m.State.Phase, "쉬는 동안엔 Over가 아니다");
            m.Tick(end);
            Assert.AreEqual(TriGrabPhase.Over, m.State.Phase);
            Assert.AreEqual(5, m.State.Round, "여섯째 라운드는 없다");
            Assert.AreEqual(TriGrabConfig.TotalRounds, m.State.TotalRounds);
            Assert.Less(end, TriGrabConfig.MatchCapTicks, "안전 상한이 아니라 라운드 수로 끝났다");
        }

        [Test]
        public void 오답은_누적_점수에서_1_깎고_라운드를_넘어_이어진다()
        {
            var m = LiveMatch(27, out long t, A);
            m.State.Slots[0].Card = RedInk();
            Assert.IsTrue(m.TryFire(A, Body, YawIn, t));
            long k = t;
            while ((m.State.Phase != TriGrabPhase.Live || m.State.Round != 2) && k < t + 2000) m.Tick(++k);
            Assert.AreEqual(10, m.State.Players[A].Score);
            m.State.Rule = new TriGrabRule(TriGrabRuleType.Ink, 0);
            var c = m.State.Clock; c.Omega = 0f; c.BaseAngle0 = 0f; m.State.Clock = c;
            foreach (var sl in m.State.Slots) { sl.Card = null; sl.EmptyUntilTick = long.MaxValue; }
            m.State.Slots[0].Card = BlueInk();
            FireAndRun(m, A, k, Body, YawIn);
            Assert.AreEqual(1, m.State.Players[A].Mistakes);
            Assert.AreEqual(9, m.State.Players[A].Score, "누적 10 → 9");
            Assert.AreEqual(0, m.State.Players[A].RoundPoints, "이번 라운드 점수는 정답에서만");
        }

        [Test]
        public void 판_끝에_누적_점수가_같으면_우승자가_없고_다르면_있다()
        {
            var tie = new TriGrabMatch(28);
            tie.AddPlayer(A); tie.AddPlayer(B);
            tie.Start(0);
            tie.State.Players[A].Score = 19; tie.State.Players[B].Score = 19;
            RunUntilOver(tie, 0, out _);
            Assert.AreEqual(TriGrabPhase.Over, tie.State.Phase);
            Assert.AreEqual("", tie.State.MatchWinnerId);

            var win = new TriGrabMatch(28);
            win.AddPlayer(A); win.AddPlayer(B);
            win.Start(0);
            win.State.Players[A].Score = 18; win.State.Players[B].Score = 19;
            RunUntilOver(win, 0, out _);
            Assert.AreEqual(B, win.State.MatchWinnerId);
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
            while (m.State.Phase == TriGrabPhase.Live && k < t + 2000) m.Tick(++k);
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
