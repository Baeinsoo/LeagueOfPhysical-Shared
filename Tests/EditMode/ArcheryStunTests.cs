using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>쏘면 안 되는 것을 맞힌 벌: 정해진 틱까지 활을 못 든다(당기던 것도 풀린다).</summary>
    public class ArcheryStunTests
    {
        const float Dt = 0.02f;

        static ArcheryConfig NoSway() => new ArcheryConfig(
            wavePeriodTicks: 100, minTargets: 1, maxTargets: 1,
            spawnRadius: 1f, spawnMinY: 0f, spawnMaxY: 1f, minSeparation: 1f,
            trapRatioMin: 0f, trapRatioMax: 0f,
            shakeFreeSeconds: 1f, shakeRampSeconds: 1f, shakeMaxDegrees: 0f,
            riseHeightMin: 0.1f, riseHeightMax: 0.1f, staggerTicks: 1, restTicks: 1, kinds: null);

        static Entity Archer()
        {
            var e = new Entity("archer-1");
            e.Add(new GameFramework.World.Transform());
            e.Add(new ArcheryAim());
            e.Add(new InputBuffer());
            e.Add(new Simulated());
            return e;
        }

        static void Feed(Entity e, bool drawing, bool release)
            => e.Get<InputBuffer>().Current = new InputCommand { Drawing = drawing, Release = release, DrawRatio = 1f };

        //  틱 from부터 to까지 당기고 있다가 release 틱에 뗀다. 쏜 화살(있으면)을 돌려준다.
        static ArcheryShot? HoldThenRelease(ArcheryAimSystem system, Entity e, long from, long release)
        {
            for (long t = from; t < release; t++)
            {
                Feed(e, drawing: true, release: false);
                system.Tick(e, t, Dt);
            }
            Feed(e, drawing: false, release: true);
            return system.Tick(e, release, Dt);
        }

        [Test]
        public void 금지_중에는_당겨도_시위가_안_걸리고_떼도_안_나간다()
        {
            var system = new ArcheryAimSystem(NoSway());
            var e = Archer();
            e.Get<ArcheryAim>().StunnedUntilTick = 200;

            var shot = HoldThenRelease(system, e, 100, 150);
            Assert.IsNull(shot);
            Assert.IsFalse(e.Get<ArcheryAim>().Drawing);
            Assert.AreEqual(0f, e.Get<ArcheryAim>().DrawRatio, 1e-5f);
        }

        [Test]
        public void 금지_중에는_누르고_있어도_시위가_0에_머문다()
        {
            //  위 시험은 뗀 틱에야 보므로 그 틱의 풀림이 한 틱분 당김을 가려 버린다 — 누르고 있는 동안을 직접 본다.
            var system = new ArcheryAimSystem(NoSway());
            var e = Archer();
            e.Get<ArcheryAim>().StunnedUntilTick = 200;
            for (long t = 100; t < 110; t++)
            {
                Feed(e, drawing: true, release: false);
                system.Tick(e, t, Dt);
                Assert.AreEqual(0f, e.Get<ArcheryAim>().DrawRatio, 1e-5f);
            }
        }

        [Test]
        public void 금지가_풀린_다음_틱부터_새로_당긴다()
        {
            var system = new ArcheryAimSystem(NoSway());
            var e = Archer();
            e.Get<ArcheryAim>().StunnedUntilTick = 200;

            //  200까지 금지, 계속 누르고 있다 — 201에 새 당김이 시작돼 25틱(0.5초) 뒤 만작.
            var shot = HoldThenRelease(system, e, 190, 230);
            Assert.IsTrue(shot.HasValue);
            Assert.AreEqual(201, e.Get<ArcheryAim>().DrawStartTick);
        }

        [Test]
        public void 당기던_중에_금지되면_시위가_풀린다()
        {
            var system = new ArcheryAimSystem(NoSway());
            var e = Archer();
            for (long t = 100; t < 120; t++) { Feed(e, true, false); system.Tick(e, t, Dt); }
            Assert.IsTrue(e.Get<ArcheryAim>().Drawing);

            e.Get<ArcheryAim>().StunnedUntilTick = 150;
            Feed(e, true, false);
            system.Tick(e, 120, Dt);
            Assert.IsFalse(e.Get<ArcheryAim>().Drawing);
        }

        [Test]
        public void 금지가_없으면_예전처럼_쏜다()
        {
            //  양성 대조.
            var system = new ArcheryAimSystem(NoSway());
            Assert.IsTrue(HoldThenRelease(system, Archer(), 100, 130).HasValue);
        }

        [Test]
        public void 함정을_맞힌_결과는_사격_금지를_싣는다()
        {
            var trap = new ArcheryTarget(0, 2, Vector3.zero, 0f, 0, 0.85f, -3, true, ArcheryTargetShape.Sphere,
                                         null, Vector3.back, 2.6f, string.Empty, 0f, 0f, stunTicks: 75);
            var outcome = ArcheryHitRules.Resolve(trap, 0f);
            Assert.AreEqual(3, outcome.Lost);
            Assert.AreEqual(75, outcome.StunTicks);
        }

        [Test]
        public void 성한_표적은_사격_금지가_없다()
        {
            var clean = new ArcheryTarget(0, 0, Vector3.zero, 0f, 0, 0.6f, 3, false, ArcheryTargetShape.Sphere,
                                          null, Vector3.back, 2.6f, string.Empty, 0f, 0f, stunTicks: 75);
            Assert.AreEqual(0, ArcheryHitRules.Resolve(clean, 0f).StunTicks);
        }

        //  ---- 되감기 ----

        static (ArcheryWorld world, Entity archer) World()
        {
            var registry = new EntityRegistry();
            var archer = Archer();
            registry.Add(archer);
            var config = NoSway();
            var course = new ArcheryCourse(config, new ZeroSeed(), new string[0], Dt, () => null);
            return (ArcheryWorldFixture.Still(registry, new ArcheryAimSystem(config), course, Dt), archer);
        }

        private sealed class ZeroSeed : IMatchSeed { public ulong Value => 1UL; }

        [Test]
        public void 되감아도_사격_금지가_안_풀린다()
        {
            //  금지는 서버가 확정한 사실이다 — 되감기 전 값으로 돌아가면 그 사이 쏜 것처럼 예측한다.
            var (world, archer) = World();
            world.Tick(10, Dt);
            world.SaveState(10);
            archer.Get<ArcheryAim>().StunnedUntilTick = 500;
            world.LoadState(10);
            Assert.AreEqual(500, archer.Get<ArcheryAim>().StunnedUntilTick);
        }

        [Test]
        public void 지운_화살은_되감아도_안_돌아온다()
        {
            var (world, archer) = World();
            for (long t = 1; t <= 30; t++) { Feed(archer, true, false); world.Tick(t, Dt); }
            Feed(archer, false, true);
            world.Tick(31, Dt);
            world.SaveState(31);
            Assert.AreEqual(1, world.Shots.Count);

            world.CancelShots("archer-1", 20, 100);
            Assert.AreEqual(0, world.Shots.Count);

            world.LoadState(31);
            Assert.AreEqual(0, world.Shots.Count);
        }

        [Test]
        public void 구간_밖의_화살과_남의_화살은_안_지운다()
        {
            var (world, archer) = World();
            world.IngestRemoteShot(new ArcheryShot("other", 25, Vector3.zero, Vector3.forward));
            world.IngestRemoteShot(new ArcheryShot("archer-1", 20, Vector3.zero, Vector3.forward));   // 경계(afterTick) — 남는다
            world.IngestRemoteShot(new ArcheryShot("archer-1", 21, Vector3.zero, Vector3.forward));
            world.CancelShots("archer-1", 20, 100);
            Assert.AreEqual(2, world.Shots.Count);
        }
    }
}
