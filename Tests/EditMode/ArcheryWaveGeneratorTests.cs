using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class ArcheryWaveGeneratorTests
    {
        private static ArcheryTargetKind[] Kinds()
        {
            return new[]
            {
                new ArcheryTargetKind(0.60f, 1, 50, false),
                new ArcheryTargetKind(0.40f, 2, 35, false),
                new ArcheryTargetKind(0.25f, 4, 15, false),
            };
        }

        //  간격을 숫자로 적어 넣지 않고 종류에서 뽑는다. 중심 사이 거리 하나로 모든 조합을 막아야
        //  하므로, 기준은 "가장 큰 과녁 둘이 딱 맞닿는 거리" = 최대 반경 × 2다. 종류의 반경을
        //  키우면 이 값도 따라 커져, 픽스처가 배포 데이터와 다른 조건을 시험하는 일이 없다.
        private static ArcheryConfig Config()
        {
            return ConfigWith(Kinds(), TouchingDistance(Kinds()));
        }

        private static ArcheryConfig ConfigWith(ArcheryTargetKind[] kinds, float minSeparation)
        {
            return ConfigWith(kinds, minSeparation, trapRatioMin: 0f, trapRatioMax: 0f);
        }

        private static ArcheryConfig ConfigWith(ArcheryTargetKind[] kinds, float minSeparation,
                                                float trapRatioMin, float trapRatioMax)
        {
            return new ArcheryConfig(
                wavePeriodTicks: 88, minTargets: 2, maxTargets: 3,
                spawnRadius: 2f, spawnMinY: 2f, spawnMaxY: 6f, minSeparation: minSeparation,
                trapRatioMin: trapRatioMin, trapRatioMax: trapRatioMax,
                shakeFreeSeconds: 1f, shakeRampSeconds: 2f, shakeMaxDegrees: 3f,
                kinds: kinds);
        }

        //  최대 반경은 설정이 스스로 계산한다. 여기서 또 훑으면 계산이 두 군데가 되므로,
        //  간격을 0으로 둔 설정을 한 번 만들어 그 값을 빌린다.
        private static float TouchingDistance(ArcheryTargetKind[] kinds)
        {
            return ConfigWith(kinds, 0f).MaxTargetRadius * 2f;
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
                        //  딱 맞닿는 건 겹친 게 아니다 — 간격 기준이 정확히 그 거리라 경계가 허용된다.
                        Assert.GreaterOrEqual(gap, touching, $"wave {wave}: {i}과 {j}가 겹친다");
                    }
                }
            }
        }

        //  위 테스트가 통과하는 건 간격 기준이 "가장 큰 둘이 맞닿는 거리"라서다. 그보다 짧게 주면
        //  정말로 겹친 과녁이 나온다 — 배포 데이터 검사(min_separation >= 최대반경 x 2)가 막는 게
        //  이것이고, 이 테스트가 그 검사의 존재 이유다.
        [Test]
        public void 간격_기준이_맞닿는_거리보다_짧으면_겹친_과녁이_나온다()
        {
            var kinds = Kinds();
            var config = ConfigWith(kinds, TouchingDistance(kinds) * 0.5f);
            var targets = new List<ArcheryTarget>();

            for (int wave = 0; wave < 200; wave++)
            {
                ArcheryWaveGenerator.Fill(targets, 99UL, wave, config);
                for (int i = 0; i < targets.Count; i++)
                {
                    for (int j = i + 1; j < targets.Count; j++)
                    {
                        float gap = Vector3.Distance(targets[i].Center, targets[j].Center);
                        if (gap < targets[i].Radius + targets[j].Radius)
                        {
                            Assert.Pass($"wave {wave}: {i}과 {j}가 겹쳤다 — 기준이 짧으면 이렇게 된다");
                        }
                    }
                }
            }
            Assert.Fail("간격 기준을 절반으로 줄였는데도 200 웨이브에서 겹친 과녁이 하나도 없다 — "
                        + "간격 기준이 겹침을 막는 장치가 맞는지 다시 봐야 한다");
        }

        [Test]
        public void 종류가_비면_최대_반경은_0이다()
        {
            var config = ConfigWith(new ArcheryTargetKind[0], 1f);
            Assert.AreEqual(0f, config.MaxTargetRadius);
        }

        [Test]
        public void 최대_반경은_가장_큰_종류를_따른다()
        {
            Assert.AreEqual(0.60f, Config().MaxTargetRadius);
        }

        [Test]
        public void 과녁은_자기_종류의_함정_표시를_이어받는다()
        {
            var kinds = new[]
            {
                new ArcheryTargetKind(0.60f, 1, 50, false),
                new ArcheryTargetKind(0.40f, -3, 50, true),
            };
            var config = ConfigWith(kinds, TouchingDistance(kinds), trapRatioMin: 0f, trapRatioMax: 1f);
            var targets = new List<ArcheryTarget>();

            for (int wave = 0; wave < 100; wave++)
            {
                ArcheryWaveGenerator.Fill(targets, 11UL, wave, config);
                for (int i = 0; i < targets.Count; i++)
                {
                    //  함정 종류는 반경 0.40 하나뿐이라, 표시가 제대로 따라왔으면 둘이 항상 같이 움직인다.
                    bool fromRadius = Mathf.Approximately(targets[i].Radius, 0.40f);
                    Assert.AreEqual(fromRadius, targets[i].IsTrap, $"wave {wave} slot {i}");
                }
            }
        }

        [Test]
        public void 종류를_성한_것과_함정으로_갈라_들고_있는다()
        {
            var kinds = new[]
            {
                new ArcheryTargetKind(0.60f, 1, 50, false),
                new ArcheryTargetKind(0.40f, -3, 30, true),
                new ArcheryTargetKind(0.25f, 4, 20, false),
            };
            var config = ConfigWith(kinds, TouchingDistance(kinds));

            Assert.AreEqual(2, config.CleanKinds.Count);
            Assert.AreEqual(1, config.TrapKinds.Count);
            Assert.IsTrue(config.TrapKinds[0].IsTrap);
        }

        [Test]
        public void 종류가_한쪽뿐이면_다른_쪽은_빈_목록이다()
        {
            var kinds = new[] { new ArcheryTargetKind(0.60f, 1, 50, false) };
            var config = ConfigWith(kinds, TouchingDistance(kinds));

            Assert.AreEqual(1, config.CleanKinds.Count);
            Assert.AreEqual(0, config.TrapKinds.Count);
        }

        //  이 비율 손잡이가 실제로 듣는지 본다 — 안 들으면 "참을까 말까"를 조절할 방법이 없다.
        [Test]
        public void 비율을_0으로_두면_함정이_하나도_안_뜬다()
        {
            var kinds = TrapMixedKinds();
            var config = ConfigWith(kinds, TouchingDistance(kinds), trapRatioMin: 0f, trapRatioMax: 0f);
            var targets = new List<ArcheryTarget>();

            for (int wave = 0; wave < 300; wave++)
            {
                ArcheryWaveGenerator.Fill(targets, 3UL, wave, config);
                for (int i = 0; i < targets.Count; i++)
                {
                    Assert.IsFalse(targets[i].IsTrap, $"wave {wave} slot {i}");
                }
            }
        }

        [Test]
        public void 비율을_1로_두면_전부_함정이다()
        {
            var kinds = TrapMixedKinds();
            var config = ConfigWith(kinds, TouchingDistance(kinds), trapRatioMin: 1f, trapRatioMax: 1f);
            var targets = new List<ArcheryTarget>();

            for (int wave = 0; wave < 300; wave++)
            {
                ArcheryWaveGenerator.Fill(targets, 3UL, wave, config);
                for (int i = 0; i < targets.Count; i++)
                {
                    Assert.IsTrue(targets[i].IsTrap, $"wave {wave} slot {i}");
                }
            }
        }

        //  spec 3절: "0개도, 전부도 가능". 범위를 열어 두면 양 끝이 실제로 나와야 한다.
        [Test]
        public void 범위를_열어_두면_전부_성한_웨이브와_전부_함정인_웨이브가_둘_다_나온다()
        {
            var kinds = TrapMixedKinds();
            var config = ConfigWith(kinds, TouchingDistance(kinds), trapRatioMin: 0f, trapRatioMax: 1f);
            var targets = new List<ArcheryTarget>();

            bool sawAllClean = false;
            bool sawAllTrap = false;
            for (int wave = 0; wave < 300; wave++)
            {
                ArcheryWaveGenerator.Fill(targets, 7UL, wave, config);
                int traps = 0;
                for (int i = 0; i < targets.Count; i++)
                {
                    traps += targets[i].IsTrap ? 1 : 0;
                }
                sawAllClean |= traps == 0;
                sawAllTrap |= traps == targets.Count && targets.Count > 0;
            }

            Assert.IsTrue(sawAllClean, "전부 성한 웨이브가 한 번도 안 나왔다");
            Assert.IsTrue(sawAllTrap, "전부 함정인 웨이브가 한 번도 안 나왔다");
        }

        //  데이터에 함정 종류가 없는데 비율만 올려 둔 경우. 조용히 성한 과녁을 함정으로 만들면 안 된다.
        [Test]
        public void 함정_종류가_없으면_비율이_1이어도_함정이_안_뜬다()
        {
            var kinds = Kinds();
            var config = ConfigWith(kinds, TouchingDistance(kinds), trapRatioMin: 1f, trapRatioMax: 1f);
            var targets = new List<ArcheryTarget>();

            for (int wave = 0; wave < 100; wave++)
            {
                ArcheryWaveGenerator.Fill(targets, 9UL, wave, config);
                Assert.That(targets.Count, Is.InRange(config.MinTargets, config.MaxTargets),
                            $"wave {wave}: 과녁이 아예 안 떴다");
                for (int i = 0; i < targets.Count; i++)
                {
                    Assert.IsFalse(targets[i].IsTrap, $"wave {wave} slot {i}");
                }
            }
        }

        private static ArcheryTargetKind[] TrapMixedKinds()
        {
            return new[]
            {
                new ArcheryTargetKind(0.60f, 1, 50, false),
                new ArcheryTargetKind(0.40f, 2, 35, false),
                new ArcheryTargetKind(0.50f, -3, 40, true),
            };
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
