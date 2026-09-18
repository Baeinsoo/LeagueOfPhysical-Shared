using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// <b>과녁이 언제 어디 서는지를 정하는 한 곳.</b> 맵이 값으로 고른 방식(<see cref="ArcheryCourseKind"/>)에
    /// 따라 갈리지만, 부르는 쪽(서버 판정·클라 뷰·화살 꽂기)은 그 갈림을 모른다 — 셋이 각자 분기하면
    /// 한 곳만 안 고쳐져도 클·서가 다른 과녁을 본다.
    ///
    /// <para>클·서가 <b>같은 구체 클래스</b>를 돌린다(인터페이스 seam 금지). 같은 씨앗·같은 씬·같은
    /// 명단이면 같은 과녁이 나오므로 과녁은 통신하지 않는다.</para>
    /// </summary>
    public sealed class ArcheryCourse
    {
        //  순서 뽑기용 씨앗을 웨이브 스트림과 섞이지 않게 가른다. 웨이브는 Combine(seed, waveIndex)를
        //  쓰므로 작은 정수 영역을 피해 문자열 해시를 쓴다.
        private static readonly ulong CourseSalt = GameFramework.Rng.Hashing.Fnv1a64("archery-course");

        private readonly ArcheryConfig config;

        //  레이아웃은 **맵 씬이 다 뜬 뒤에** 읽어야 한다. 이 객체는 씬 로드보다 먼저 만들어지므로
        //  (서버는 스코프가 세워질 때 판정 시스템을 즉시 resolve한다) 값을 미리 받으면 빈 레이아웃이
        //  영영 굳는다 — 과녁이 하나도 안 뜨는데 에러도 안 난다. 그래서 "읽는 방법"만 받아 둔다.
        private readonly System.Func<ArcheryRangeLayout> layoutSource;
        private ArcheryRangeLayout layout;

        private readonly IMatchSeed matchSeed;
        private readonly IReadOnlyList<string> owners;
        private readonly float tickInterval;

        //  판 시작에 한 번 뽑은 순서(거리 자리 번호)와, 각 단계가 시작하는 상대 틱.
        //  씨앗이 늦게 도착하므로(서버가 보내 준다) 처음 쓸 때 만든다.
        private int[] order;
        private long[] stepStartTicks;

        public ArcheryCourse(ArcheryConfig config, IMatchSeed matchSeed, IReadOnlyList<string> owners,
                             float tickInterval, System.Func<ArcheryRangeLayout> layoutSource = null)
        {
            this.config = config;
            //  기본은 "열려 있는 씬에서 읽기". 시험만 다른 방법을 넣는다.
            this.layoutSource = layoutSource ?? ArcheryRangeLayout.FromOpenScenes;
            this.matchSeed = matchSeed;
            this.owners = owners ?? new string[0];
            this.tickInterval = tickInterval;
        }

        /// <summary>사거리 코스의 단계 수. 웨이브 맵은 끝이 없으므로 0이다.</summary>
        public int StepCount => config.CourseKind == ArcheryCourseKind.Range ? config.Range.Stands.Count : 0;

        /// <summary>사수 한 명이 받는 화살 수. <b>0이면 무제한</b>(웨이브 맵).</summary>
        public int ArrowsPerArcher => StepCount;

        /// <summary>
        /// 이 판의 길이(틱). 사거리는 <b>노출과 간격의 합</b>이라 순서와 무관하다 — 그래서 씨앗이
        /// 없어도 답할 수 있다. 웨이브 맵은 데이터가 적어 둔 값을 그대로 쓴다.
        /// </summary>
        public long MatchDurationTicks
        {
            get
            {
                if (config.CourseKind != ArcheryCourseKind.Range)
                {
                    return config.MatchDurationTicks;
                }

                long total = 0;
                for (int i = 0; i < config.Range.Stands.Count; i++)
                {
                    total += config.Range.Stands[i].ExposureTicks + config.Range.StepGapTicks;
                }
                return total;
            }
        }

        /// <summary>
        /// 이 틱에 서 있는 단계(웨이브 맵에서는 웨이브 번호). 출발 전이면 −1,
        /// 사거리에서 순서가 끝난 뒤면 <see cref="StepCount"/> 이상이다.
        /// </summary>
        public int IndexAt(long tick, long gameplayStartTick)
        {
            if (config.CourseKind != ArcheryCourseKind.Range)
            {
                return ArcheryWaveGenerator.WaveIndexAt(tick, gameplayStartTick, config);
            }

            if (gameplayStartTick == long.MaxValue || tick < gameplayStartTick)
            {
                return -1;
            }

            EnsureBuilt();
            long elapsed = tick - gameplayStartTick;
            if (elapsed >= MatchDurationTicks)
            {
                return StepCount;   // 순서가 끝났다 — 부르는 쪽은 이 값을 "더 없다"로 읽는다
            }

            for (int i = stepStartTicks.Length - 1; i >= 0; i--)
            {
                if (elapsed >= stepStartTicks[i])
                {
                    return i;
                }
            }
            return 0;
        }

        /// <summary>그 단계의 과녁을 채운다(먼저 비운다).</summary>
        public void Fill(List<ArcheryTarget> into, int index, long gameplayStartTick)
        {
            if (config.CourseKind != ArcheryCourseKind.Range)
            {
                ArcheryWaveGenerator.Fill(into, matchSeed.Value, index, config, gameplayStartTick);
                return;
            }

            into.Clear();
            if (index < 0 || index >= StepCount)
            {
                return;   // 출발 전이거나 순서가 끝났다
            }

            EnsureBuilt();
            var stand = config.Range.Stands[order[index]];
            var kind = config.Range.Kind;
            long spawnTick = gameplayStartTick + stepStartTicks[index];
            float lifetime = stand.ExposureTicks * tickInterval;

            //  사수마다 자기 레인에 하나씩. 슬롯 번호가 곧 사수 번호라, 먹힌 과녁을 알리는
            //  비트마스크(ArcheryStateToC)를 그대로 쓸 수 있다.
            int count = Mathf.Min(owners.Count, layout.Lanes.Count);
            for (int slot = 0; slot < count; slot++)
            {
                var lane = layout.Lanes[slot];
                Vector3 center = lane.Stands[stand.StandIndex];

                into.Add(new ArcheryTarget(index, slot, center, 0f, spawnTick,
                                           kind.Radius, kind.Points, kind.IsTrap,
                                           kind.Shape, kind.Bands,
                                           -lane.Forward,        // 사수 쪽을 바라본다
                                           lifetime, owners[slot]));
            }
        }

        //  씨앗은 서버가 보내 주므로 만들 때는 아직 없을 수 있다 — 처음 쓸 때 만든다.
        private void EnsureBuilt()
        {
            if (order != null)
            {
                return;
            }

            //  여기 오는 것은 이미 판이 시작한 뒤다(위 호출부가 출발 전이면 먼저 돌아간다) —
            //  그래서 맵 씬은 확실히 떠 있다.
            layout = layoutSource();

            int count = config.Range.Stands.Count;
            var draw = new int[count];
            for (int i = 0; i < count; i++)
            {
                draw[i] = i;
            }

            //  피셔–예이츠. 뒤에서부터 한 칸씩 자리를 바꾼다 — 난수를 정확히 count−1번 쓴다.
            var rng = new GameFramework.Rng.DeterministicRandom(
                GameFramework.Rng.Hashing.Combine(matchSeed.Value, CourseSalt));
            for (int i = count - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                (draw[i], draw[j]) = (draw[j], draw[i]);
            }

            var starts = new long[count];
            long cursor = 0;
            for (int i = 0; i < count; i++)
            {
                starts[i] = cursor;
                cursor += config.Range.Stands[draw[i]].ExposureTicks + config.Range.StepGapTicks;
            }

            stepStartTicks = starts;
            order = draw;   // 마지막에 넣는다 — 중간에 끊겨도 반쯤 만들어진 상태가 안 보이게
        }
    }
}
