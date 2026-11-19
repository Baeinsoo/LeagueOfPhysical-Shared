using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class TriGrabMathTests
    {
        static TriGrabCard Card(int word, int ink, bool cycles = false, int period = 10, long t0 = 0) =>
            new TriGrabCard { Id = 1, Word = word, Ink = ink, Cycles = cycles, PeriodTicks = period, T0Tick = t0 };

        [Test]
        public void 안_도는_카드는_색이_그대로다()
        {
            var c = Card(0, 2);
            Assert.AreEqual(2, c.InkAt(0));
            Assert.AreEqual(2, c.InkAt(500));
        }

        [Test]
        public void 도는_카드는_주기마다_빨노파로_한_칸씩_간다()
        {
            var c = Card(1, 0, true, 10, 100);
            Assert.AreEqual(0, c.InkAt(100));
            Assert.AreEqual(0, c.InkAt(109));
            Assert.AreEqual(1, c.InkAt(110));
            Assert.AreEqual(2, c.InkAt(120));
            Assert.AreEqual(0, c.InkAt(130));
            Assert.AreEqual(0, c.InkAt(50), "T0 이전엔 처음 색");
        }

        [Test]
        public void 순환_주기는_라운드가_갈수록_짧아지고_하한이_있다()
        {
            Assert.AreEqual(34, TriGrabConfig.CyclePeriodTicks(1));
            Assert.AreEqual(33, TriGrabConfig.CyclePeriodTicks(2));
            Assert.AreEqual(21, TriGrabConfig.CyclePeriodTicks(20));
            Assert.AreEqual(21, TriGrabConfig.CyclePeriodTicks(99));
        }

        [Test]
        public void 주문_글자색은_글자색만_본다()
        {
            var rule = new TriGrabRule(TriGrabRuleType.Ink, 0);
            Assert.IsTrue(rule.Matches(Card(2, 0), 0));
            Assert.IsFalse(rule.Matches(Card(0, 2), 0));
        }

        [Test]
        public void 주문_글자는_글자만_본다()
        {
            var rule = new TriGrabRule(TriGrabRuleType.Word, 1);
            Assert.IsTrue(rule.Matches(Card(1, 2), 0));
            Assert.IsFalse(rule.Matches(Card(2, 1), 0));
        }

        [Test]
        public void 주문_일치는_글자와_그_틱의_색이_같을_때다()
        {
            var rule = new TriGrabRule(TriGrabRuleType.Match, 0);
            Assert.IsTrue(rule.Matches(Card(1, 1), 0));
            Assert.IsFalse(rule.Matches(Card(1, 2), 0));
            var cyc = Card(1, 0, true, 10, 0);
            Assert.IsFalse(rule.Matches(cyc, 5));
            Assert.IsTrue(rule.Matches(cyc, 10), "도는 카드는 닿는 틱의 색으로 판정");
        }

        [Test]
        public void 주문_한국어_글()
        {
            Assert.AreEqual("글자색이 빨강인 카드", new TriGrabRule(TriGrabRuleType.Ink, 0).KoreanText);
            Assert.AreEqual("'노랑'이라고 쓰인 카드", new TriGrabRule(TriGrabRuleType.Word, 1).KoreanText);
            Assert.AreEqual("글자와 색이 같은 카드", new TriGrabRule(TriGrabRuleType.Match, 2).KoreanText);
        }

        [Test]
        public void 회전판_준비_중엔_안_돌고_Live에_돌고_멈춘_뒤엔_고정()
        {
            var clock = new TriGrabRoundClock { BaseAngle0 = 1f, Omega = 0.5f, LiveStartTick = 100, FreezeTick = 300 };
            Assert.AreEqual(1f, clock.BaseAt(0), 1e-5f);
            Assert.AreEqual(1f, clock.BaseAt(100), 1e-5f);
            Assert.AreEqual(1f + 0.5f * 0.02f * 50, clock.BaseAt(150), 1e-4f);
            float frozen = 1f + 0.5f * 0.02f * 200;
            Assert.AreEqual(frozen, clock.BaseAt(300), 1e-4f);
            Assert.AreEqual(frozen, clock.BaseAt(900), 1e-4f);
        }

        [Test]
        public void 슬롯_자리는_반지름_4의_고리를_8등분한다()
        {
            var p0 = TriGrabCarousel.SlotPosition(0, 0f);
            Assert.AreEqual(4f, p0.x, 1e-4f);
            Assert.AreEqual(0f, p0.y, 1e-4f);
            var p2 = TriGrabCarousel.SlotPosition(2, 0f);
            Assert.AreEqual(0f, p2.x, 1e-4f);
            Assert.AreEqual(4f, p2.y, 1e-4f);
            var rot = TriGrabCarousel.SlotPosition(0, Mathf.PI);
            Assert.AreEqual(-4f, rot.x, 1e-4f);
        }

        [Test]
        public void 갈고리_길이는_뻗고_꺾이고_되감긴다()
        {
            var h = new TriGrabHook
            {
                FireTick = 100, Origin = Vector2.zero, Dir = new Vector2(0, 1),
                TurnTick = 118, TurnLength = 9f,
            };
            h.EndTick = TriGrabHook.ComputeEndTick(h.TurnTick, h.TurnLength, false);
            Assert.AreEqual(0f, h.LengthAt(100), 1e-4f);
            Assert.AreEqual(5f, h.LengthAt(110), 1e-4f);
            Assert.AreEqual(9f, h.LengthAt(118), 1e-4f);
            Assert.AreEqual(9f - 0.65f * 12, h.LengthAt(130), 1e-3f);
            Assert.AreEqual(0f, h.LengthAt(h.EndTick), 1e-4f);
        }

        [Test]
        public void 끝_자리는_몸_반지름만큼_앞에서_시작한다()
        {
            var h = new TriGrabHook { FireTick = 0, Origin = new Vector2(1, 1), Dir = new Vector2(1, 0), TurnTick = 18, TurnLength = 9f };
            var tip = h.TipAt(10);
            Assert.AreEqual(1f + 0.7f + 5f, tip.x, 1e-4f);
            Assert.AreEqual(1f, tip.y, 1e-4f);
        }

        [Test]
        public void 끝_틱은_빈손_14틱_카드_20틱_회수()
        {
            Assert.AreEqual(100 + 14, TriGrabHook.ComputeEndTick(100, 9f, false));
            Assert.AreEqual(100 + 20, TriGrabHook.ComputeEndTick(100, 9f, true));
            Assert.AreEqual(100 + 10, TriGrabHook.ComputeEndTick(100, 6.5f, false), "딱 떨어지면 한 틱 더 세지 않는다");
        }

        [Test]
        public void 카드를_잡으면_더_느리게_돌아온다()
        {
            var h = new TriGrabHook { FireTick = 0, Origin = Vector2.zero, Dir = Vector2.up, TurnTick = 10, TurnLength = 5f };
            float empty = h.LengthAt(15);
            h.Carried = new TriGrabCard();
            Assert.Greater(h.LengthAt(15), empty);
        }

        [Test]
        public void 닿은_카드_중_쏜_몸에_가장_가까운_것을_고른다()
        {
            var pos = new[] { new Vector2(0, 1), new Vector2(0, 0), new Vector2(5, 5) };
            var occ = new[] { true, true, true };
            // 끝은 두 카드 모두 반지름 안. 몸은 (0,-3)이라 (0,0) 쪽이 더 가깝다.
            Assert.AreEqual(1, TriGrabMath.PickHit(new Vector2(0, 0.5f), new Vector2(0, -3), pos, occ));
            Assert.AreEqual(0, TriGrabMath.PickHit(new Vector2(0, 0.5f), new Vector2(0, 5), pos, occ));
            occ[1] = false;
            Assert.AreEqual(0, TriGrabMath.PickHit(new Vector2(0, 0.5f), new Vector2(0, -3), pos, occ), "빈 슬롯은 건너뛴다");
            Assert.AreEqual(-1, TriGrabMath.PickHit(new Vector2(20, 20), Vector2.zero, pos, occ));
        }

        [Test]
        public void yaw_방향_왕복()
        {
            var d = TriGrabMath.DirFromYaw(90f);
            Assert.AreEqual(1f, d.x, 1e-5f);
            Assert.AreEqual(0f, d.y, 1e-5f);
            Assert.AreEqual(-90f, TriGrabMath.YawFromDir(new Vector2(-1, 0)), 1e-3f);
        }
    }
}
