using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class ArcheryHitRulesTests
    {
        private static ArcheryTarget Target(int points, bool isTrap)
        {
            return new ArcheryTarget(0, 0, Vector3.zero, 0f, 0L, 0.5f, points, isTrap,
                ArcheryTargetShape.Sphere, null, Vector3.zero, 10f, string.Empty, 0f, 0f);
        }

        private static ArcheryTarget Owned(string ownerUserId)
        {
            return new ArcheryTarget(0, 0, Vector3.zero, 0f, 0L, 0.5f, 5, false,
                ArcheryTargetShape.Face, null, Vector3.zero, 10f, ownerUserId, 0f, 0f);
        }

        //  ── 맞고 나면 사라지는가 ────────────────────────────────────────────
        //
        //  주인이 있는 과녁(사거리)은 **안 사라져야** 자리당 여러 발을 같은 과녁에 꽂을 수 있다.
        //  사라지면 첫 발이 맞는 순간 남은 화살이 쏠 곳을 잃고, 착탄 기록판에 점이 하나만 찍혀
        //  군집이 안 보인다 — 리드를 고칠 근거가 사라진다. (2026-09-22 실측에서 드러났다.)

        [Test]
        public void 주인이_있으면_맞아도_안_사라진다()
        {
            Assert.IsFalse(ArcheryHitRules.ConsumedOnHit(Owned("user-a")),
                "사거리 과녁이 첫 발에 사라진다 — 남은 화살이 쏠 곳을 잃는다");
        }

        //  주인이 없으면 먼저 맞힌 사람이 먹는 규칙이라, 안 사라지면 한 과녁으로
        //  여럿이 무한정 점수를 낸다.
        [Test]
        public void 주인이_없으면_맞으면_사라진다()
        {
            Assert.IsTrue(ArcheryHitRules.ConsumedOnHit(Owned(string.Empty)),
                "원형 맵 과녁이 안 사라지면 무한 득점이 열린다");
        }

        //  "가져갈 수 있나"와 "사라지나"는 다른 질문이다 — 주인 있는 과녁은
        //  주인만 가져갈 수 있지만(가져가도) 사라지지는 않는다.
        [Test]
        public void 가져가는_것과_사라지는_것은_다른_질문이다()
        {
            var mine = Owned("user-a");

            Assert.IsTrue(ArcheryHitRules.CanTake(mine, "user-a"), "주인이 자기 과녁을 못 가져간다");
            Assert.IsFalse(ArcheryHitRules.CanTake(mine, "user-b"), "남이 남의 과녁을 가져간다");
            Assert.IsFalse(ArcheryHitRules.ConsumedOnHit(mine), "주인이 가져갔다고 사라지면 안 된다");
        }

        [Test]
        public void 성한_과녁은_점수를_준다()
        {
            var outcome = ArcheryHitRules.Resolve(Target(points: 3, isTrap: false), 0f);

            Assert.AreEqual(3, outcome.Gained);
            Assert.AreEqual(0, outcome.Lost);
            Assert.AreEqual(3, outcome.Delta);
        }

        //  데이터에 함정 점수를 음수로 적든 양수로 적든 같은 벌점이 나와야 한다.
        //  적는 사람이 부호를 어느 쪽으로 쓸지 정해 두지 않으면 값이 두 배로 틀린다.
        [Test]
        public void 함정은_음수로_적어도_양수로_적어도_같은_벌점이다()
        {
            var written = ArcheryHitRules.Resolve(Target(points: -3, isTrap: true), 0f);
            var writtenPositive = ArcheryHitRules.Resolve(Target(points: 3, isTrap: true), 0f);

            Assert.AreEqual(0, written.Gained);
            Assert.AreEqual(3, written.Lost);
            Assert.AreEqual(-3, written.Delta);

            Assert.AreEqual(written.Gained, writtenPositive.Gained);
            Assert.AreEqual(written.Lost, writtenPositive.Lost);
        }

        [Test]
        public void 함정_점수가_0이면_아무_일도_안_일어난다()
        {
            var outcome = ArcheryHitRules.Resolve(Target(points: 0, isTrap: true), 0f);

            Assert.AreEqual(0, outcome.Gained);
            Assert.AreEqual(0, outcome.Lost);
            Assert.AreEqual(0, outcome.Delta);
        }

        //  성한 과녁에 음수를 적어 둔 데이터. 점수를 몰래 깎는 대신 아무것도 주지 않는다.
        [Test]
        public void 성한_과녁에_음수가_적혀_있으면_점수를_주지_않는다()
        {
            var outcome = ArcheryHitRules.Resolve(Target(points: -5, isTrap: false), 0f);

            Assert.AreEqual(0, outcome.Gained);
            Assert.AreEqual(0, outcome.Lost);
        }
    }
}
