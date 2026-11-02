using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// 연속 날갯짓 고비와 보상(spec 2026-10-27 §1·§2). 기본은 사용자 결정(10-27) 숫자, 맵 마커 값이 이상하면 기본으로 간다
    /// — 판이 안 뜨는 것보다 낫다.
    /// </summary>
    public class FlappyFlapComboRulesTests
    {
        [Test]
        public void 기본은_10_25_50_100에_10분의1_4분의1_2분의1_2분의1칸()
        {
            var rules = FlappyFlapComboRules.Default;

            Assert.AreEqual(4, rules.Count);
            Assert.AreEqual(0.1f, rules.Gain(10), "10-30: 10도 작은 대시 게이지를 준다");
            Assert.AreEqual(0.25f, rules.Gain(25));
            Assert.AreEqual(0.5f, rules.Gain(50));
            Assert.AreEqual(0.5f, rules.Gain(100));
            Assert.AreEqual(0, rules.MilestoneIndex(10));
            Assert.AreEqual(3, rules.MilestoneIndex(100));
        }

        [Test]
        public void 고비가_아닌_수는_보상_0이고_번호는_마이너스_1()
        {
            foreach (int n in new[] { 0, 1, 9, 11, 24, 26, 99, 101, 149, 151, 175, 199 })
            {
                Assert.AreEqual(0f, FlappyFlapComboRules.Default.Gain(n), n.ToString());
                Assert.AreEqual(-1, FlappyFlapComboRules.Default.MilestoneIndex(n), n.ToString());
                Assert.AreEqual(-1, FlappyFlapComboRules.Default.RewardIndex(n), n.ToString());
            }
        }

        [Test]
        public void 마지막_고비_뒤로는_50마다_마지막_보상을_다시_준다()
        {
            //  사용자 결정 11-02 — 100 뒤로 150·200·250…마다 0.5칸. 끊지 않는 쪽이 박고 다시 쌓는 쪽보다 늘 낫게.
            var rules = FlappyFlapComboRules.Default;
            foreach (int n in new[] { 150, 200, 250, 1000 })
            {
                Assert.AreEqual(0.5f, rules.Gain(n), n.ToString());
                Assert.AreEqual(3, rules.RewardIndex(n), n.ToString() + " — 연출은 마지막 고비(전설!)와 같게");
                Assert.AreEqual(-1, rules.MilestoneIndex(n), n.ToString() + " — 고비 번호는 정확한 고비에만(서버·코인과 무관)");
            }
            Assert.AreEqual(0, rules.RewardIndex(10));
            Assert.AreEqual(3, rules.RewardIndex(100));
        }

        [Test]
        public void 되풀이는_맵이_준_마지막_고비에서부터_센다()
        {
            Assert.IsTrue(FlappyFlapComboRules.TryCreate(new[] { 5, 20 }, new[] { 0.1f, 2f }, out var rules));

            Assert.AreEqual(2f, rules.Gain(70));
            Assert.AreEqual(1, rules.RewardIndex(70));
            Assert.AreEqual(0f, rules.Gain(50), "기본 고비(…100)에서 세지 않는다");
            Assert.AreEqual(0f, rules.Gain(-30), "음수 쪽으로 되풀이하지 않는다");
            Assert.AreEqual(-1, FlappyFlapComboRules.Off.RewardIndex(50), "보상 없는 맵은 되풀이도 없다");
            Assert.AreEqual(0f, FlappyFlapComboRules.Off.Gain(50));
        }

        [Test]
        public void 둘_다_비우면_보상_없는_맵()
        {
            Assert.IsTrue(FlappyFlapComboRules.TryCreate(new int[0], new float[0], out var rules));

            Assert.AreSame(FlappyFlapComboRules.Off, rules);
            Assert.AreEqual(0, rules.Count);
            Assert.AreEqual(0f, rules.Gain(10));
            Assert.AreEqual(-1, rules.MilestoneIndex(10));
        }

        [Test]
        public void 맵이_준_고비를_쓴다()
        {
            Assert.IsTrue(FlappyFlapComboRules.TryCreate(new[] { 5, 20 }, new[] { 0.1f, 2f }, out var rules));

            Assert.AreEqual(0.1f, rules.Gain(5));
            Assert.AreEqual(2f, rules.Gain(20));
            Assert.AreEqual(1, rules.MilestoneIndex(20));
            Assert.AreEqual(0f, rules.Gain(10), "기본 고비가 섞이지 않는다");
        }

        [Test]
        public void 값이_이상하면_기본으로_간다()
        {
            var bad = new (int[] milestones, float[] gains, string why)[]
            {
                (null, new[] { 1f }, "고비 없음"),
                (new[] { 10 }, null, "보상 없음"),
                (new[] { 10, 25 }, new[] { 0.25f }, "짝이 안 맞음"),
                (new[] { 25, 10 }, new[] { 0.5f, 0.25f }, "오름차순 아님"),
                (new[] { 10, 10 }, new[] { 0.25f, 0.25f }, "같은 고비 둘"),
                (new[] { 0 }, new[] { 0.25f }, "0 고비"),
                (new[] { 10 }, new[] { -0.25f }, "음수 보상"),
                (new[] { 10 }, new[] { 2.5f }, "2칸 넘는 보상"),
                (new[] { 10 }, new[] { float.NaN }, "NaN 보상"),
            };
            foreach (var (milestones, gains, why) in bad)
            {
                Assert.IsFalse(FlappyFlapComboRules.TryCreate(milestones, gains, out var rules), why);
                Assert.AreSame(FlappyFlapComboRules.Default, rules, why);
            }
        }

        [Test]
        public void 넘겨준_배열을_나중에_바꿔도_규칙은_그대로()
        {
            //  마커 배열은 인스펙터에서 바뀔 수 있다 — 판 도중에 규칙이 따라 바뀌면 클·서가 갈린다.
            var milestones = new[] { 5 };
            var gains = new[] { 1f };
            FlappyFlapComboRules.TryCreate(milestones, gains, out var rules);

            milestones[0] = 7;
            gains[0] = 0f;

            Assert.AreEqual(1f, rules.Gain(5));
            Assert.AreEqual(-1, rules.MilestoneIndex(7));
        }

        [Test]
        public void 기본값_배열은_부를_때마다_새것()
        {
            //  마커 필드 초기값이 이 배열이다 — 한 마커가 고치면 다른 마커·기본 규칙까지 바뀌면 안 된다.
            var first = FlappyFlapComboRules.DefaultMilestones();
            first[0] = 999;

            CollectionAssert.AreEqual(new[] { 10, 25, 50, 100 }, FlappyFlapComboRules.DefaultMilestones());
            CollectionAssert.AreEqual(new[] { 0.1f, 0.25f, 0.5f, 0.5f }, FlappyFlapComboRules.DefaultGains());
            Assert.AreEqual(0.1f, FlappyFlapComboRules.Default.Gain(10));
        }

        [Test]
        public void 로그_한_줄로_적는다()
        {
            Assert.AreEqual("10:+0.1 25:+0.25 50:+0.5 100:+0.5 그뒤50마다:+0.5", FlappyFlapComboRules.Default.Describe());
            Assert.AreEqual("없음", FlappyFlapComboRules.Off.Describe());
        }
    }
}
