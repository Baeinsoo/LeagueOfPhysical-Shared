using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>2차 표적 종류 — 움직임 값 묶음과 역할(일반·대박·금지물) 나누기.</summary>
    public class ArcheryCatapultKindTests
    {
        private static ArcheryTargetKind Kind(int id, bool trap = false, bool jackpot = false,
                                              ArcheryCatapultMotion motion = default)
            => new ArcheryTargetKind(0.8f, trap ? -3 : 1, 1, trap, ArcheryTargetShape.Sphere, null,
                                     id: id, distanceMinM: 30f, distanceMaxM: 40f, driftMinMps: 1f, driftMaxMps: 2f,
                                     stunTicks: trap ? 75 : 0, isJackpot: jackpot, motion: motion);

        [Test]
        public void 종류는_움직임_묶음과_대박_표시를_들고_있다()
        {
            var motion = new ArcheryCatapultMotion(heightMinM: 2.5f, heightMaxM: 5f, arcMinM: 3f, arcMaxM: 4.5f,
                                                   climbMinMps: 3.5f, climbMaxMps: 5f, bobAmplitudeM: 0.6f, bobHz: 0.48f,
                                                   spinHz: 1.1f, lateralHalfM: 8f,
                                                   spawnSideMinM: 3f, spawnSideMaxM: 7f, lifeTicks: 100,
                                                   swingAmplitudeM: 2.5f, swingDipM: 1.6f);
            var kind = Kind(18, jackpot: true, motion: motion);
            Assert.IsTrue(kind.IsJackpot);
            Assert.AreEqual(2.5f, kind.Motion.HeightMinM);
            Assert.AreEqual(5f, kind.Motion.HeightMaxM);
            Assert.AreEqual(3f, kind.Motion.ArcMinM);
            Assert.AreEqual(4.5f, kind.Motion.ArcMaxM);
            Assert.AreEqual(3.5f, kind.Motion.ClimbMinMps);
            Assert.AreEqual(5f, kind.Motion.ClimbMaxMps);
            Assert.AreEqual(0.6f, kind.Motion.BobAmplitudeM);
            Assert.AreEqual(0.48f, kind.Motion.BobHz);
            Assert.AreEqual(1.1f, kind.Motion.SpinHz);
            Assert.AreEqual(8f, kind.Motion.LateralHalfM);
            //  3차 — 값이 서로 다르게 넣어 칸을 바꿔 적으면 걸리게 한다.
            Assert.AreEqual(3f, kind.Motion.SpawnSideMinM);
            Assert.AreEqual(7f, kind.Motion.SpawnSideMaxM);
            Assert.AreEqual(100, kind.Motion.LifeTicks);
            Assert.AreEqual(2.5f, kind.Motion.SwingAmplitudeM);
            Assert.AreEqual(1.6f, kind.Motion.SwingDipM);
        }

        [Test]
        public void 옛_종류는_움직임이_없고_대박이_아니다()
        {
            var old = new ArcheryTargetKind(0.6f, 3, 30, false, ArcheryTargetShape.Sphere, null);
            Assert.IsFalse(old.IsJackpot);
            Assert.AreEqual(default(ArcheryCatapultMotion), old.Motion);
        }

        [Test]
        public void 설정은_일반과_대박과_금지물을_따로_모은다()
        {
            var kinds = new[] { Kind(14), Kind(18, jackpot: true), Kind(20, trap: true), Kind(15), Kind(19, jackpot: true) };
            var c = new ArcheryConfig(0, 0, 0, 16f, 3.5f, 13f, 0f, 0f, 0f, 0f, 2.5f, 0f, 0.5f, 1.5f, 0, 0, kinds,
                                      courseKind: ArcheryCourseKind.Catapult);
            CollectionAssert.AreEqual(new[] { 14, 15 }, c.NormalKinds.Select(k => k.Id).ToArray());
            CollectionAssert.AreEqual(new[] { 18, 19 }, c.JackpotKinds.Select(k => k.Id).ToArray());
            CollectionAssert.AreEqual(new[] { 20 }, c.TrapKinds.Select(k => k.Id).ToArray());
            //  웨이브 생성기 계약 — 성한 것은 대박까지 다 든다(웨이브는 대박을 모른다).
            CollectionAssert.AreEqual(new[] { 14, 18, 15, 19 }, c.CleanKinds.Select(k => k.Id).ToArray());
        }

        [Test]
        public void 대박_확률은_설정에_있고_안_적으면_0이다()
        {
            var s = new ArcheryCatapultSettings(3, 80, 1250, 110, 180, 35, 215, 180, 0.35f, 16f, jackpotChance: 0.28f);
            Assert.AreEqual(0.28f, s.JackpotChance);
            Assert.AreEqual(0f, new ArcheryCatapultSettings(3, 80, 1250, 110, 180, 35, 215, 180, 0.35f, 16f).JackpotChance);
            Assert.AreEqual(0f, ArcheryCatapultSettings.None.JackpotChance);
        }

        [Test]
        public void 표적은_대박과_움직임_값을_들고_있다()
        {
            var t = new ArcheryTarget(1, 6, Vector3.zero, 0f, 100, 0.8f, 5, false, ArcheryTargetShape.Sphere, null,
                                      Vector3.back, 2.6f, string.Empty, 0f, 0f,
                                      isJackpot: true, bobAmplitude: 0.3f, bobHz: 0.32f, spinHz: 1.1f, climbMps: 4f, motionPhase: 0.25f,
                                      swing: new Vector3(-3f, 0f, 0f), swingDip: 1.6f);
            Assert.IsTrue(t.IsJackpot);
            Assert.AreEqual(0.3f, t.BobAmplitude);
            Assert.AreEqual(0.32f, t.BobHz);
            Assert.AreEqual(1.1f, t.SpinHz);
            Assert.AreEqual(4f, t.Climb);
            Assert.AreEqual(0.25f, t.MotionPhase);
            Assert.AreEqual(new Vector3(-3f, 0f, 0f), t.Swing);
            Assert.AreEqual(1.6f, t.SwingDip);
            //  옛 호출부(그네 인수 없음)는 그네가 없다.
            var old = new ArcheryTarget(1, 0, Vector3.zero, 0f, 100, 0.8f, 1, false, ArcheryTargetShape.Sphere, null,
                                        Vector3.back, 2.6f, string.Empty, 0f, 0f);
            Assert.AreEqual(Vector3.zero, old.Swing);
            Assert.AreEqual(0f, old.SwingDip);
        }
    }
}
