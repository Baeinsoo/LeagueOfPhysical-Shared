using NUnit.Framework;
using static LOP.Tests.ArcheryCatapultFixture;

namespace LOP.Tests
{
    /// <summary>
    /// 한 판의 시계. 라운드 = 소개 80 + 사냥 1250 + 결과 110 = 1440틱, 무리는 사냥 시작부터 180틱마다
    /// "준비"(35틱 뒤 발사). 사냥 마지막 215틱 안에서는 준비를 시작하지 않으므로 라운드당 6무리다.
    /// </summary>
    public class ArcheryCatapultScheduleTests
    {
        [Test]
        public void 라운드_길이와_무리_수와_판_길이()
        {
            var s = Settings();
            Assert.AreEqual(1440, s.RoundTicks);
            Assert.AreEqual(6, s.VolleysPerRound);   // 0·180·…·900 < 1035, 1080은 마지막 4.3초 안
            Assert.AreEqual(18, s.VolleyCount);
            Assert.AreEqual(4320L, s.MatchTicks);    // 86.4초 ≈ 1분 27초
        }

        [Test]
        public void 무리의_준비와_발사_틱()
        {
            var s = Settings();
            Assert.AreEqual(1080L, ArcheryCatapultSchedule.CueTick(s, 0, StartTick));
            Assert.AreEqual(1115L, ArcheryCatapultSchedule.LaunchTick(s, 0, StartTick));
            Assert.AreEqual(1980L, ArcheryCatapultSchedule.CueTick(s, 5, StartTick));
            Assert.AreEqual(2520L, ArcheryCatapultSchedule.CueTick(s, 6, StartTick));   // 2라운드 첫 무리
            Assert.AreEqual(4895L, ArcheryCatapultSchedule.LaunchTick(s, 17, StartTick));
        }

        [Test]
        public void 마지막_무리는_사냥이_끝나기_전에_사라진다()
        {
            var s = Settings();
            for (int round = 0; round < s.RoundCount; round++)
            {
                int last = (round + 1) * s.VolleysPerRound - 1;
                long gone = ArcheryCatapultSchedule.LaunchTick(s, last, StartTick) + s.TargetLifeTicks;
                Assert.Less(gone, ArcheryCatapultSchedule.HuntEndTick(s, round, StartTick), $"라운드 {round}");
            }
        }

        [TestCase(999, -1)]
        [TestCase(1000, -1)]   // 1라운드 소개
        [TestCase(1079, -1)]
        [TestCase(1080, 0)]
        [TestCase(1259, 0)]
        [TestCase(1260, 1)]
        [TestCase(1980, 5)]
        [TestCase(2439, 5)]    // 1라운드 결과 — 마지막 무리를 그대로 가리킨다
        [TestCase(2440, 5)]    // 2라운드 소개
        [TestCase(2520, 6)]
        [TestCase(5319, 17)]
        [TestCase(5320, 18)]   // 판 끝 = VolleyCount
        public void 틱에서_무리_번호(long tick, int expected)
        {
            Assert.AreEqual(expected, ArcheryCatapultSchedule.VolleyIndexAt(Settings(), tick, StartTick));
        }

        [Test]
        public void 출발_틱을_모르면_아무_무리도_없다()
        {
            Assert.AreEqual(-1, ArcheryCatapultSchedule.VolleyIndexAt(Settings(), 5000, long.MaxValue));
            Assert.AreEqual(-1, ArcheryCatapultSchedule.LiveVolleyAt(Settings(), 5000, long.MaxValue));
            Assert.AreEqual(ArcheryCatapultPhase.Waiting,
                ArcheryCatapultSchedule.PhaseAt(Settings(), 5000, long.MaxValue, out _));
        }

        [TestCase(999, ArcheryCatapultPhase.Waiting, -1)]
        [TestCase(1000, ArcheryCatapultPhase.Intro, 0)]
        [TestCase(1080, ArcheryCatapultPhase.Hunt, 0)]
        [TestCase(2329, ArcheryCatapultPhase.Hunt, 0)]
        [TestCase(2330, ArcheryCatapultPhase.Result, 0)]
        [TestCase(2440, ArcheryCatapultPhase.Intro, 1)]
        [TestCase(5319, ArcheryCatapultPhase.Result, 2)]
        [TestCase(5320, ArcheryCatapultPhase.Over, 2)]
        public void 틱에서_단계와_라운드(long tick, ArcheryCatapultPhase phase, int round)
        {
            Assert.AreEqual(phase, ArcheryCatapultSchedule.PhaseAt(Settings(), tick, StartTick, out int r));
            Assert.AreEqual(round, r);
        }

        [Test]
        public void 설정이_없으면_무리도_판도_없다()
        {
            var none = ArcheryCatapultSettings.None;
            Assert.AreEqual(0, none.VolleyCount);
            Assert.AreEqual(0L, none.MatchTicks);
            Assert.AreEqual(-1, ArcheryCatapultSchedule.VolleyIndexAt(none, 5000, StartTick));
            Assert.AreEqual(-1, ArcheryCatapultSchedule.LiveVolleyAt(none, 5000, StartTick));
        }

        /// <summary>
        /// preflight F1: 무리는 "준비"(VolleyIndexAt)가 아니라 발사(LaunchTick) 기준으로 살아 있어야
        /// 한다 — 발사 뒤 TargetLifeTicks(180틱 = 3.6초)가 다 지나기 전까지는 그 무리 표적이
        /// 판정·화면에 그대로 남아 있어야 하는데, 다음 무리의 "준비"는 발사 145틱 뒤(준비 간격
        /// 180 − 준비 0.7초 35)에 시작해 버려 VolleyIndexAt만 쓰면 마지막 35틱(0.7초)이 잘린다.
        /// LiveVolleyAt은 그 수명 마지막 틱까지 같은 무리를 가리켜야 한다.
        /// </summary>
        [Test]
        public void 무리의_마지막_살아있는_틱에_LiveVolleyAt이_그_무리를_가리킨다()
        {
            var s = Settings();
            for (int volley = 0; volley < s.VolleyCount; volley++)
            {
                long lastLiveTick = ArcheryCatapultSchedule.LaunchTick(s, volley, StartTick) + s.TargetLifeTicks - 1;
                Assert.AreEqual(volley, ArcheryCatapultSchedule.LiveVolleyAt(s, lastLiveTick, StartTick),
                    $"무리 {volley}");
            }
        }

        [Test]
        public void 발사_전에는_LiveVolleyAt이_먼저_넘어가지_않는다()
        {
            var s = Settings();
            long launch0 = ArcheryCatapultSchedule.LaunchTick(s, 0, StartTick);
            Assert.AreEqual(-1, ArcheryCatapultSchedule.LiveVolleyAt(s, launch0 - 1, StartTick));
            Assert.AreEqual(0, ArcheryCatapultSchedule.LiveVolleyAt(s, launch0, StartTick));
        }
    }
}
