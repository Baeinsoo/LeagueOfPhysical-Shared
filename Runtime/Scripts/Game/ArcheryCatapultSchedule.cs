namespace LOP
{
    public enum ArcheryCatapultPhase
    {
        /// <summary>판이 아직 시작 안 했다(또는 시작 틱을 모른다).</summary>
        Waiting = 0,
        Intro = 1,
        Hunt = 2,
        Result = 3,
        /// <summary>마지막 라운드 결과까지 끝났다.</summary>
        Over = 4,
    }

    /// <summary>
    /// 투석기 사냥의 시계 — 틱만 보고 지금이 몇 라운드의 어느 단계인지, 몇 번째 무리인지 답한다.
    /// 상태가 없어 클·서·화면이 각자 불러도 같은 답이다(표적을 통신하지 않는 이유).
    /// </summary>
    public static class ArcheryCatapultSchedule
    {
        public static ArcheryCatapultPhase PhaseAt(ArcheryCatapultSettings s, long tick, long start, out int round)
        {
            round = -1;
            if (start == long.MaxValue || tick < start || s.RoundTicks <= 0 || s.RoundCount <= 0)
            {
                return ArcheryCatapultPhase.Waiting;
            }
            long elapsed = tick - start;
            if (elapsed >= s.MatchTicks)
            {
                round = s.RoundCount - 1;
                return ArcheryCatapultPhase.Over;
            }
            round = (int)(elapsed / s.RoundTicks);
            long inRound = elapsed - (long)round * s.RoundTicks;
            if (inRound < s.IntroTicks)
            {
                return ArcheryCatapultPhase.Intro;
            }
            return inRound < s.IntroTicks + s.HuntTicks ? ArcheryCatapultPhase.Hunt : ArcheryCatapultPhase.Result;
        }

        public static long RoundStartTick(ArcheryCatapultSettings s, int round, long start)
            => start + (long)round * s.RoundTicks;

        public static long HuntEndTick(ArcheryCatapultSettings s, int round, long start)
            => RoundStartTick(s, round, start) + s.IntroTicks + s.HuntTicks;

        /// <summary>그 무리의 "준비"가 시작하는 틱.</summary>
        public static long CueTick(ArcheryCatapultSettings s, int volley, long start)
        {
            int round = volley / s.VolleysPerRound;
            int inRound = volley % s.VolleysPerRound;
            return RoundStartTick(s, round, start) + s.IntroTicks + (long)inRound * s.VolleyPeriodTicks;
        }

        /// <summary>그 무리가 날아오르는 틱 = 표적의 <c>SpawnTick</c>.</summary>
        public static long LaunchTick(ArcheryCatapultSettings s, int volley, long start)
            => CueTick(s, volley, start) + s.CueTicks;

        /// <summary>
        /// 이 틱에 마지막으로 "준비"를 시작한 무리. 첫 준비 전이면 −1, 판이 끝났으면 <c>VolleyCount</c>.
        /// 무리 사이·라운드 결과·다음 라운드 소개 동안에는 직전 무리를 그대로 가리킨다 —
        /// HUD "준비…" 신호와 투석기 팔 연출에만 쓴다(표적 판정·화면은 <see cref="LiveVolleyAt"/>을 쓴다 —
        /// "준비"는 발사보다 먼저 와서, 이 값으로 표적 목록을 갈아 끼우면 직전 무리가 수명을
        /// 다 채우기 전에 잘려 나간다).
        /// </summary>
        public static int VolleyIndexAt(ArcheryCatapultSettings s, long tick, long start)
        {
            if (start == long.MaxValue || tick < start || s.VolleysPerRound <= 0)
            {
                return -1;
            }
            long elapsed = tick - start;
            if (elapsed >= s.MatchTicks)
            {
                return s.VolleyCount;
            }
            int round = (int)(elapsed / s.RoundTicks);
            long sinceHunt = elapsed - (long)round * s.RoundTicks - s.IntroTicks;
            if (sinceHunt < 0)
            {
                return round * s.VolleysPerRound - 1;   // 소개 중 — 앞 라운드 마지막 무리(첫 라운드면 −1)
            }
            long inRound = System.Math.Min(sinceHunt / s.VolleyPeriodTicks, s.VolleysPerRound - 1);
            return round * s.VolleysPerRound + (int)inRound;
        }

        /// <summary>
        /// 이 틱에 "살아 있는" 무리 — 판정·화면이 쓴다(<c>ArcheryCourse.IndexAt</c>).
        /// <see cref="VolleyIndexAt"/>과 달리 "준비"가 아니라 <b>발사 틱</b>을 기준으로 넘어간다 —
        /// 그래야 무리가 <see cref="ArcheryCatapultSettings.TargetLifeTicks"/>(수명 2.6초)를
        /// 다 채울 때까지 같은 번호를 가리킨다. 발사 간격(3.2초)이 수명보다 길어 둘 사이에 빈
        /// 틈이 생기는데, 그 틈에서도 다음 무리가 뜨기 전까지는 직전 번호를 그대로 돌려준다 —
        /// 이미 수명이 다한 표적은 어차피 목록에서 빠져 있어 해가 없다.
        /// 첫 발사 전이면 −1, 판이 끝났으면 <c>VolleyCount</c>.
        /// </summary>
        public static int LiveVolleyAt(ArcheryCatapultSettings s, long tick, long start)
        {
            if (start == long.MaxValue || tick < start || s.VolleysPerRound <= 0)
            {
                return -1;
            }
            long elapsed = tick - start;
            if (elapsed >= s.MatchTicks)
            {
                return s.VolleyCount;
            }
            int round = (int)(elapsed / s.RoundTicks);
            long sinceFirstLaunch = elapsed - (long)round * s.RoundTicks - s.IntroTicks - s.CueTicks;
            if (sinceFirstLaunch < 0)
            {
                return round * s.VolleysPerRound - 1;   // 이 라운드에서 아직 아무도 발사되지 않음
            }
            long inRound = System.Math.Min(sinceFirstLaunch / s.VolleyPeriodTicks, s.VolleysPerRound - 1);
            return round * s.VolleysPerRound + (int)inRound;
        }
    }
}
