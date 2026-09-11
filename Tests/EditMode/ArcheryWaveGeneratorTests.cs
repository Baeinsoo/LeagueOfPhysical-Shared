using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class ArcheryWaveGeneratorTests
    {
        private static ArcheryConfig Config()
        {
            return new ArcheryConfig(
                wavePeriodTicks: 88, minTargets: 2, maxTargets: 3,
                spawnRadius: 2f, spawnMinY: 2f, spawnMaxY: 6f, minSeparation: 1.2f,
                kinds: new[]
                {
                    new ArcheryTargetKind(0.60f, 1, 50),
                    new ArcheryTargetKind(0.40f, 2, 35),
                    new ArcheryTargetKind(0.25f, 4, 15),
                });
        }

        [Test]
        public void 출발_전에는_웨이브가_없다()
        {
            Assert.AreEqual(-1, ArcheryWaveGenerator.WaveIndexAt(500, 1000, Config()));
        }

        [Test]
        public void 출발틱이_아직_안_정해졌으면_웨이브가_없다()
        {
            Assert.AreEqual(-1, ArcheryWaveGenerator.WaveIndexAt(500, long.MaxValue, Config()));
        }

        [Test]
        public void 웨이브는_주기마다_한_칸씩_오른다()
        {
            var config = Config();
            Assert.AreEqual(0, ArcheryWaveGenerator.WaveIndexAt(1000, 1000, config));
            Assert.AreEqual(0, ArcheryWaveGenerator.WaveIndexAt(1087, 1000, config));
            Assert.AreEqual(1, ArcheryWaveGenerator.WaveIndexAt(1088, 1000, config));
            Assert.AreEqual(2, ArcheryWaveGenerator.WaveIndexAt(1176, 1000, config));
        }

        [Test]
        public void 웨이브_시작틱은_웨이브_번호의_역함수다()
        {
            var config = Config();
            for (int i = 0; i < 30; i++)
            {
                long start = ArcheryWaveGenerator.WaveStartTick(i, 1000, config);
                Assert.AreEqual(i, ArcheryWaveGenerator.WaveIndexAt(start, 1000, config));
            }
        }

        // 이 게임의 생명줄이다 — 깨지면 사람마다 다른 과녁을 보는데 화면은 멀쩡해 보이고 점수만 이상해진다.
        [Test]
        public void 같은_씨앗과_웨이브는_언제_몇_번_물어도_같은_과녁을_준다()
        {
            var config = Config();
            var a = new List<ArcheryTarget>();
            var b = new List<ArcheryTarget>();

            for (int wave = 0; wave < 40; wave++)
            {
                ArcheryWaveGenerator.Fill(a, 0xC0FFEEUL, wave, config);
                ArcheryWaveGenerator.Fill(b, 0xC0FFEEUL, wave, config);

                Assert.AreEqual(a.Count, b.Count, $"wave {wave}");
                for (int i = 0; i < a.Count; i++)
                {
                    Assert.AreEqual(a[i].SlotIndex, b[i].SlotIndex);
                    Assert.AreEqual(a[i].Radius, b[i].Radius);
                    Assert.AreEqual(a[i].Points, b[i].Points);
                    //  부동소수도 *완전히* 같아야 한다 — 근사 비교로 두면 갈라지는 순간을 못 잡는다.
                    Assert.AreEqual(a[i].Center.x, b[i].Center.x);
                    Assert.AreEqual(a[i].Center.y, b[i].Center.y);
                    Assert.AreEqual(a[i].Center.z, b[i].Center.z);
                }
            }
        }

        [Test]
        public void 씨앗이_다르면_다른_과녁이_나온다()
        {
            var config = Config();
            var a = new List<ArcheryTarget>();
            var b = new List<ArcheryTarget>();
            ArcheryWaveGenerator.Fill(a, 1UL, 0, config);
            ArcheryWaveGenerator.Fill(b, 2UL, 0, config);
            Assert.AreNotEqual(a[0].Center, b[0].Center);
        }

        [Test]
        public void 웨이브가_다르면_다른_과녁이_나온다()
        {
            var config = Config();
            var a = new List<ArcheryTarget>();
            var b = new List<ArcheryTarget>();
            ArcheryWaveGenerator.Fill(a, 7UL, 0, config);
            ArcheryWaveGenerator.Fill(b, 7UL, 1, config);
            Assert.AreNotEqual(a[0].Center, b[0].Center);
        }

        [Test]
        public void 과녁은_정해진_공간_안에만_뜬다()
        {
            var config = Config();
            var targets = new List<ArcheryTarget>();
            for (int wave = 0; wave < 200; wave++)
            {
                ArcheryWaveGenerator.Fill(targets, 42UL, wave, config);
                Assert.That(targets.Count, Is.InRange(config.MinTargets, config.MaxTargets));
                for (int i = 0; i < targets.Count; i++)
                {
                    var c = targets[i].Center;
                    float horizontal = new Vector2(c.x, c.z).magnitude;
                    Assert.LessOrEqual(horizontal, config.SpawnRadius + 1e-4f);
                    Assert.That(c.y, Is.InRange(config.SpawnMinY, config.SpawnMaxY));
                    Assert.AreEqual(wave, targets[i].WaveIndex);
                    Assert.AreEqual(i, targets[i].SlotIndex);
                }
            }
        }

        [Test]
        public void 같은_웨이브의_과녁끼리는_겹치지_않는다()
        {
            var config = Config();
            var targets = new List<ArcheryTarget>();
            for (int wave = 0; wave < 200; wave++)
            {
                ArcheryWaveGenerator.Fill(targets, 99UL, wave, config);
                for (int i = 0; i < targets.Count; i++)
                {
                    for (int j = i + 1; j < targets.Count; j++)
                    {
                        float gap = Vector3.Distance(targets[i].Center, targets[j].Center);
                        float touching = targets[i].Radius + targets[j].Radius;
                        Assert.Greater(gap, touching, $"wave {wave}: {i}과 {j}가 겹친다");
                    }
                }
            }
        }

        [Test]
        public void 목록을_다시_채우면_앞의_것이_남지_않는다()
        {
            var config = Config();
            var targets = new List<ArcheryTarget> { default, default, default, default, default };
            ArcheryWaveGenerator.Fill(targets, 5UL, 0, config);
            Assert.That(targets.Count, Is.InRange(config.MinTargets, config.MaxTargets));
        }
    }
}
