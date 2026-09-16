using System.Collections.Generic;
using NUnit.Framework;

namespace LOP.Tests
{
    public class ArcheryRingScoringTests
    {
        //  띠는 "바깥 경계까지"를 뜻한다 — 0.2면 중심에서 반지름의 20%까지가 그 띠다.
        [Test]
        public void 띠는_바깥_경계와_점수를_들고_있다()
        {
            var band = new ArcheryRingBand(0.2f, 10);

            Assert.AreEqual(0.2f, band.OuterRatio, 1e-6f);
            Assert.AreEqual(10, band.Points);
        }

        //  지금 과녁은 "어디를 맞히든 같은 점수"다 — 그게 띠 하나짜리다.
        [Test]
        public void 종류는_모양과_띠를_들고_있다()
        {
            var bands = new List<ArcheryRingBand> { new ArcheryRingBand(1f, 2) };
            var kind = new ArcheryTargetKind(0.3f, 2, 40, false, ArcheryTargetShape.Sphere, bands);

            Assert.AreEqual(ArcheryTargetShape.Sphere, kind.Shape);
            Assert.AreEqual(1, kind.Bands.Count);
            Assert.AreEqual(2, kind.Bands[0].Points);
        }

        static ArcheryTarget TargetWith(bool isTrap, int points, params ArcheryRingBand[] bands)
        {
            return new ArcheryTarget(
                waveIndex: 0, slotIndex: 0,
                origin: UnityEngine.Vector3.zero, riseSpeed: 0f, spawnTick: 0L,
                radius: 0.4f, points: points, isTrap: isTrap,
                shape: ArcheryTargetShape.Face,
                bands: new List<ArcheryRingBand>(bands),
                facing: new UnityEngine.Vector3(0f, 0f, -1f));
        }

        //  띠가 없으면 예전처럼 "어디를 맞히든 같은 점수"다. 띠 데이터가 아직 없는 과녁이
        //  조용히 0점이 되면 안 된다.
        [Test]
        public void 띠가_없으면_과녁_점수를_그대로_준다()
        {
            var target = TargetWith(isTrap: false, points: 2);

            Assert.AreEqual(2, ArcheryHitRules.Resolve(target, 0f).Gained);
            Assert.AreEqual(2, ArcheryHitRules.Resolve(target, 0.99f).Gained);
        }

        [Test]
        public void 중심에_가까울수록_높은_띠를_받는다()
        {
            var target = TargetWith(isTrap: false, points: 0,
                new ArcheryRingBand(0.2f, 10),
                new ArcheryRingBand(0.5f, 8),
                new ArcheryRingBand(1.0f, 5));

            Assert.AreEqual(10, ArcheryHitRules.Resolve(target, 0.0f).Gained, "정중앙");
            Assert.AreEqual(10, ArcheryHitRules.Resolve(target, 0.1f).Gained);
            Assert.AreEqual(8, ArcheryHitRules.Resolve(target, 0.35f).Gained);
            Assert.AreEqual(5, ArcheryHitRules.Resolve(target, 0.9f).Gained);
        }

        //  경계선 위는 "그 띠까지"로 친다 — 안 그러면 경계에 맞을 때마다 값이 흔들린다.
        [Test]
        public void 띠_경계선_위는_그_띠에_속한다()
        {
            var target = TargetWith(isTrap: false, points: 0,
                new ArcheryRingBand(0.2f, 10),
                new ArcheryRingBand(1.0f, 5));

            Assert.AreEqual(10, ArcheryHitRules.Resolve(target, 0.2f).Gained);
        }

        //  가장자리 밖으로 조금 새는 값이 와도 마지막 띠로 받는다 — 부동소수 오차로 1.0000001이
        //  들어와 점수가 0이 되는 일을 막는다.
        [Test]
        public void 가장자리를_아주_조금_넘어도_마지막_띠를_준다()
        {
            var target = TargetWith(isTrap: false, points: 0,
                new ArcheryRingBand(1.0f, 5));

            Assert.AreEqual(5, ArcheryHitRules.Resolve(target, 1.0001f).Gained);
        }

        //  함정은 어느 띠든 벌점이다. 데이터에 -5로 적든 5로 적든 같은 벌점이 되어야 한다.
        [Test]
        public void 함정은_띠_점수의_크기만큼_깎는다()
        {
            var trap = TargetWith(isTrap: true, points: 0,
                new ArcheryRingBand(0.3f, -10),
                new ArcheryRingBand(1.0f, 5));

            Assert.AreEqual(0, ArcheryHitRules.Resolve(trap, 0.1f).Gained);
            Assert.AreEqual(10, ArcheryHitRules.Resolve(trap, 0.1f).Lost);
            Assert.AreEqual(5, ArcheryHitRules.Resolve(trap, 0.9f).Lost);
        }

        //  성한 과녁에 음수가 적혀 있으면 몰래 깎는 대신 아무것도 주지 않는다(기존 규칙 유지).
        [Test]
        public void 성한_과녁의_음수_띠는_0점이_된다()
        {
            var target = TargetWith(isTrap: false, points: 0, new ArcheryRingBand(1.0f, -7));

            Assert.AreEqual(0, ArcheryHitRules.Resolve(target, 0.5f).Gained);
            Assert.AreEqual(0, ArcheryHitRules.Resolve(target, 0.5f).Lost);
        }

        //  띠 목록이 아예 null인 경우도 빈 목록과 같게 다뤄야 한다 — 마스터데이터에 그 과녁의
        //  띠가 한 줄도 없으면 provider가 빈 목록을 주지만, 코드 어딘가가 null을 주더라도
        //  조용히 0점이 되면 안 된다.
        [Test]
        public void 띠_목록이_null이어도_과녁_점수를_준다()
        {
            var target = new ArcheryTarget(
                waveIndex: 0, slotIndex: 0,
                origin: UnityEngine.Vector3.zero, riseSpeed: 0f, spawnTick: 0L,
                radius: 0.4f, points: 2, isTrap: false,
                shape: ArcheryTargetShape.Sphere, bands: null, facing: UnityEngine.Vector3.zero);

            Assert.AreEqual(2, ArcheryHitRules.Resolve(target, 0f).Gained);
            Assert.AreEqual(2, ArcheryHitRules.Resolve(target, 0.99f).Gained);
        }

        //  채점은 띠 목록이 중심→바깥 순서라는 데 기대고 있다. 그 전제가 깨지면 예외도 경고도
        //  없이 점수만 틀리므로, 어떻게 틀리는지를 여기 박아 둔다 — 목록을 만드는 쪽이 정렬을
        //  빠뜨리면 정확히 이 모양이 된다.
        [Test]
        public void 띠_순서가_뒤집히면_한가운데를_맞혀도_바깥_점수가_나온다()
        {
            var reversed = TargetWith(isTrap: false, points: 0,
                new ArcheryRingBand(1.0f, 5),
                new ArcheryRingBand(0.2f, 10));

            //  정중앙인데도 바깥 띠(5점)가 먼저 걸린다.
            Assert.AreEqual(5, ArcheryHitRules.Resolve(reversed, 0f).Gained,
                            "순서가 뒤집히면 이렇게 된다 — 정렬은 목록을 만드는 쪽의 몫이다");
        }
    }
}
