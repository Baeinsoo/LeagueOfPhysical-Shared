using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class ArcheryHitRulesTests
    {
        private static ArcheryTarget Target(int points, bool isTrap)
        {
            return new ArcheryTarget(0, 0, Vector3.zero, 0f, 0L, 0.5f, points, isTrap,
                ArcheryTargetShape.Sphere, null, Vector3.zero, 10f, string.Empty);
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
