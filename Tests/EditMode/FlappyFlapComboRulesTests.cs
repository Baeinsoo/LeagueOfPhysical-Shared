using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// 연속 날갯짓 보상(spec 2026-10-27 §1·§2, 11-02 개정 — 10마다 커지는 고정 표 + 꼬리). 맵 마커 값이 이상하면 기본으로 간다
    /// — 판이 안 뜨는 것보다 낫다. 무엇보다 "끊지 않고 잇기"가 "박고 다시 쌓기"에 지면 안 된다.
    /// </summary>
    public class FlappyFlapComboRulesTests
    {
        private static readonly float[] Table =
        {
            0.06f, 0.07f, 0.07f, 0.08f, 0.09f, 0.10f, 0.11f, 0.12f, 0.13f, 0.14f, 0.16f, 0.17f, 0.19f, 0.21f, 0.23f,
        };

        //  연속 n번까지 받은 게이지 합(상한 없이). 판정에 쓰는 float 값을 그대로 더한다.
        private static double Cumulative(FlappyFlapComboRules rules, int n)
        {
            double sum = 0;
            for (int i = 1; i <= n; i++)
            {
                sum += rules.Gain(i);
            }
            return sum;
        }

        [Test]
        public void 기본은_10마다_표의_칸이고_150_뒤로는_4분의1칸()
        {
            var rules = FlappyFlapComboRules.Default;

            Assert.AreEqual(10, rules.Step);
            for (int k = 1; k <= Table.Length; k++)
            {
                Assert.AreEqual(Table[k - 1], rules.Gain(10 * k), (10 * k).ToString());
            }
            foreach (int n in new[] { 160, 170, 200, 250, 1000 })
            {
                Assert.AreEqual(0.25f, rules.Gain(n), n.ToString());
            }
        }

        [Test]
        public void 누적은_50에_0_37_100에_0_97_150에_1_93_200에_3_18()
        {
            var rules = FlappyFlapComboRules.Default;
            Assert.AreEqual(0.37, Cumulative(rules, 50), 1e-5);
            Assert.AreEqual(0.97, Cumulative(rules, 100), 1e-5);
            Assert.AreEqual(1.93, Cumulative(rules, 150), 1e-5);
            Assert.AreEqual(3.18, Cumulative(rules, 200), 1e-5);
        }

        [Test]
        public void 간격의_배수가_아닌_수는_보상_0()
        {
            foreach (int n in new[] { -10, 0, 1, 9, 11, 25, 99, 101, 149, 151, 155 })
            {
                Assert.AreEqual(0f, FlappyFlapComboRules.Default.Gain(n), n.ToString());
            }
        }

        [Test]
        public void 걸음마다_칸은_줄지_않는다()
        {
            var rules = FlappyFlapComboRules.Default;
            float previous = 0f;
            for (int n = 10; n <= 400; n += 10)
            {
                Assert.GreaterOrEqual(rules.Gain(n), previous, n.ToString());
                previous = rules.Gain(n);
            }
        }

        [Test]
        public void 끊지_않고_잇는_쪽이_박고_다시_쌓는_쪽에_지지_않는다()
        {
            //  11-02 사용자 결정의 핵심. c번에서 박고(StunLoss만큼 잃고) 다시 N−c번 쌓은 것보다 N번 이은 것이 늘 크거나 같다.
            var rules = FlappyFlapComboRules.Default;
            var cumulative = new double[301];
            for (int n = 1; n <= 300; n++)
            {
                cumulative[n] = cumulative[n - 1] + rules.Gain(n);
            }
            foreach (float stunLoss in new[] { 0f, 0.3f })
            {
                for (int n = 2; n <= 300; n++)
                {
                    for (int c = 1; c < n; c++)
                    {
                        double crashed = cumulative[c] - stunLoss + cumulative[n - c];
                        Assert.GreaterOrEqual(cumulative[n] + 1e-6, crashed, $"N={n} c={c} 잃음={stunLoss}");
                    }
                }
            }
        }

        [Test]
        public void 화면_고비는_10_30_50_100과_그뒤_50마다()
        {
            var rules = FlappyFlapComboRules.Default;
            Assert.AreEqual(0, rules.MilestoneIndex(10));
            Assert.AreEqual(1, rules.MilestoneIndex(30));
            Assert.AreEqual(2, rules.MilestoneIndex(50));
            Assert.AreEqual(3, rules.MilestoneIndex(100));
            Assert.AreEqual(3, rules.MilestoneIndex(150));
            Assert.AreEqual(3, rules.MilestoneIndex(200));
            foreach (int n in new[] { 0, 9, 20, 25, 40, 60, 90, 110, 140, 160, 175 })
            {
                Assert.AreEqual(-1, rules.MilestoneIndex(n), n.ToString());
            }
        }

        [Test]
        public void 간격이_0이면_보상_없는_맵()
        {
            Assert.IsTrue(FlappyFlapComboRules.TryCreate(0, FlappyFlapComboRules.DefaultGains(), 0.25f, out var rules));

            Assert.AreSame(FlappyFlapComboRules.Off, rules);
            Assert.IsFalse(rules.Rewards);
            Assert.AreEqual(0f, rules.Gain(10));
            Assert.AreEqual(0f, rules.Gain(200));
            Assert.AreEqual(-1, rules.MilestoneIndex(10), "보상 없는 맵은 고비 연출도 없다");
        }

        [Test]
        public void 맵이_준_간격과_표와_꼬리를_쓴다()
        {
            Assert.IsTrue(FlappyFlapComboRules.TryCreate(5, new[] { 0.1f, 0.2f }, 1f, out var rules));

            Assert.AreEqual(0.1f, rules.Gain(5));
            Assert.AreEqual(0.2f, rules.Gain(10));
            Assert.AreEqual(1f, rules.Gain(15));
            Assert.AreEqual(1f, rules.Gain(500));
            Assert.AreEqual(0f, rules.Gain(12));
        }

        [Test]
        public void 값이_이상하면_기본으로_간다()
        {
            var bad = new (int step, float[] gains, float tail, string why)[]
            {
                (-10, new[] { 0.1f }, 0.1f, "음수 간격"),
                (10, null, 0.1f, "표 없음"),
                (10, new float[0], 0.25f, "빈 표 — 인스펙터에서 비우면 null이 아니라 []다(리뷰2 M1)"),
                (10, new[] { 0.2f, 0.1f }, 0.2f, "뒤로 가며 줄어드는 칸"),
                (10, new[] { 0.1f, 0.2f }, 0.15f, "표 끝보다 작은 꼬리"),
                (10, new[] { -0.1f }, 0.1f, "음수 칸"),
                (10, new[] { 2.5f }, 2.5f, "2칸 넘는 칸"),
                (10, new[] { 0.1f }, 2.5f, "2칸 넘는 꼬리"),
                (10, new[] { float.NaN }, 0.1f, "NaN 칸"),
                (10, new[] { 0.1f }, float.NaN, "NaN 꼬리"),
            };
            foreach (var (step, gains, tail, why) in bad)
            {
                Assert.IsFalse(FlappyFlapComboRules.TryCreate(step, gains, tail, out var rules), why);
                Assert.AreSame(FlappyFlapComboRules.Default, rules, why);
            }
        }

        [Test]
        public void 넘겨준_배열을_나중에_바꿔도_규칙은_그대로()
        {
            //  마커 배열은 인스펙터에서 바뀔 수 있다 — 판 도중에 규칙이 따라 바뀌면 클·서가 갈린다.
            var gains = new[] { 1f };
            FlappyFlapComboRules.TryCreate(5, gains, 1f, out var rules);

            gains[0] = 0f;

            Assert.AreEqual(1f, rules.Gain(5));
        }

        [Test]
        public void 기본값_배열은_부를_때마다_새것()
        {
            //  마커 필드 초기값이 이 배열이다 — 한 마커가 고치면 다른 마커·기본 규칙까지 바뀌면 안 된다.
            var first = FlappyFlapComboRules.DefaultGains();
            first[0] = 9f;

            CollectionAssert.AreEqual(Table, FlappyFlapComboRules.DefaultGains());
            Assert.AreEqual(0.06f, FlappyFlapComboRules.Default.Gain(10));
        }

        [Test]
        public void 로그_한_줄로_적는다()
        {
            Assert.AreEqual("10마다 +0.06 +0.07 +0.07 +0.08 +0.09 +0.1 +0.11 +0.12 +0.13 +0.14 +0.16 +0.17 +0.19 +0.21 +0.23 그뒤 +0.25",
                            FlappyFlapComboRules.Default.Describe());
            Assert.AreEqual("없음", FlappyFlapComboRules.Off.Describe());
        }
    }
}
