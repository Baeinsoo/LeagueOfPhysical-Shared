using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 화살 수. <b>컴포넌트가 없으면 무제한</b>이라 원형 맵은 아무것도 안 바뀐다 —
    /// 사거리 맵에서만 이 그릇이 붙는다.
    /// </summary>
    public class ArcheryQuiverTests
    {
        const float TickInterval = 0.02f;

        static Entity Archer(int arrows = -1)
        {
            var entity = new Entity("archer-1");
            entity.Add(new GameFramework.World.Transform { Position = Vector3.zero.ToNumerics() });
            entity.Add(new ArcheryAim());
            entity.Add(new InputBuffer());
            if (arrows >= 0)
            {
                entity.Add(new ArcheryQuiver { Remaining = arrows });
            }
            return entity;
        }

        static void Feed(Entity entity, bool drawing, bool release, float drawRatio = 1f)
        {
            entity.Get<InputBuffer>().Current = new InputCommand
            {
                AimYaw = 0f, AimPitch = 0f, Drawing = drawing, Release = release, DrawRatio = drawRatio,
            };
        }

        //  이 파일은 화살 수만 잰다 — 흔들림은 관심사가 아니므로 shakeMaxDegrees=0으로 꺼 둔다.
        static ArcheryConfig NoSwayConfig() => new ArcheryConfig(
            wavePeriodTicks: 100, minTargets: 1, maxTargets: 1,
            spawnRadius: 1f, spawnMinY: 0f, spawnMaxY: 1f, minSeparation: 1f,
            trapRatioMin: 0f, trapRatioMax: 0f,
            shakeFreeSeconds: 1f, shakeRampSeconds: 1f, shakeMaxDegrees: 0f,
            riseHeightMin: 0.1f, riseHeightMax: 0.1f, staggerTicks: 1, restTicks: 1,
            kinds: null);

        //  한 발 쏘는 데 필요한 두 틱: 당기고, 뗀다.
        static ArcheryShot? DrawAndRelease(Entity archer, ArcheryAimSystem system, long tick)
        {
            Feed(archer, drawing: true, release: false);
            system.Tick(archer, tick, TickInterval);
            Feed(archer, drawing: false, release: true);
            return system.Tick(archer, tick + 1, TickInterval);
        }

        [Test]
        public void 쏘면_화살이_하나_준다()
        {
            var archer = Archer(arrows: 3);
            var system = new ArcheryAimSystem(NoSwayConfig());

            Assert.IsNotNull(DrawAndRelease(archer, system, 100));
            Assert.AreEqual(2, archer.Get<ArcheryQuiver>().Remaining);
        }

        [Test]
        public void 화살이_없으면_못_쏜다()
        {
            var archer = Archer(arrows: 0);
            var system = new ArcheryAimSystem(NoSwayConfig());

            Assert.IsNull(DrawAndRelease(archer, system, 100), "화살이 0인데 화살이 나갔다");
            Assert.AreEqual(0, archer.Get<ArcheryQuiver>().Remaining, "0 아래로 내려가면 안 된다");
        }

        [Test]
        public void 화살통이_없으면_무제한이다()
        {
            var archer = Archer();   // 원형 맵 — 그릇 자체가 없다
            var system = new ArcheryAimSystem(NoSwayConfig());

            for (int i = 0; i < 10; i++)
            {
                Assert.IsNotNull(DrawAndRelease(archer, system, 100 + i * 2), $"{i + 1}번째 발이 안 나갔다");
            }
        }

        [Test]
        public void 취소한_당김은_화살을_안_쓴다()
        {
            var archer = Archer(arrows: 3);
            var system = new ArcheryAimSystem(NoSwayConfig());

            //  임계치를 못 넘고 뗐다 — 시위가 걸린 적이 없으니 화살도 안 나간다.
            Feed(archer, drawing: true, release: false, drawRatio: 0.05f);
            system.Tick(archer, 100, TickInterval);
            Feed(archer, drawing: false, release: true, drawRatio: 0.05f);

            Assert.IsNull(system.Tick(archer, 101, TickInterval));
            Assert.AreEqual(3, archer.Get<ArcheryQuiver>().Remaining, "안 나간 화살이 소모됐다");
        }

        [Test]
        public void 되감으면_화살_수도_되돌아온다()
        {
            var registry = new EntityRegistry();
            var world = new ArcheryWorld(registry, new WorldEventBuffer(), new ArcheryAimSystem(NoSwayConfig()), TickInterval);

            var archer = Archer(arrows: 3);
            archer.Add(new Simulated());   // 되감기 대상은 내가 굴리는 몸뿐이다
            registry.Add(archer);

            world.SaveState(100);

            Feed(archer, drawing: true, release: false);
            world.Tick(101, TickInterval);
            Feed(archer, drawing: false, release: true);
            world.Tick(102, TickInterval);
            Assert.AreEqual(2, archer.Get<ArcheryQuiver>().Remaining, "쏘고도 안 줄었다 — 시험이 아무것도 재지 못한다");

            world.LoadState(100);

            Assert.AreEqual(3, archer.Get<ArcheryQuiver>().Remaining,
                "되감았는데 화살이 그대로다 — 예측이 빗나갈 때마다 화살이 사라진다");
        }
    }
}
