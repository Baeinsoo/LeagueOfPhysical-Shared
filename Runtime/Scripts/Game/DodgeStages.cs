using System.Collections.Generic;

namespace LOP
{
    /// <summary>스테이지 하나(마스터데이터 TbDodgeStage 한 행). 시간은 초로 받아 틱으로 들고 있는다.</summary>
    public readonly struct DodgeStage
    {
        public readonly string Name;
        public readonly int DurationTicks;
        /// <summary>이 스테이지에 나오는 종류. 비어 있으면 전부 — 빈 목록으로 진행기가 죽지 않게.</summary>
        public readonly DodgePatternKind[] Kinds;
        public readonly float BaseIntensity;
        /// <summary>스테이지 끝에서 세기가 기본의 (1 + 이 값)배가 된다.</summary>
        public readonly float Tighten;
        public readonly int IntervalTicks;

        public DodgeStage(string name, float durationSeconds, DodgePatternKind[] kinds,
                          float baseIntensity, float tighten, float intervalSeconds)
        {
            Name = name;
            DurationTicks = DodgeConfig.Ticks(durationSeconds);
            Kinds = kinds == null || kinds.Length == 0 ? DodgeStageTable.AllKinds : kinds;
            BaseIntensity = baseIntensity;
            Tighten = tighten;
            IntervalTicks = DodgeConfig.Ticks(intervalSeconds);
        }
    }

    /// <summary>어느 틱이 어느 스테이지의 어디쯤인가. Index가 -1이면 시작 전, 스테이지 수와 같으면 서든데스.</summary>
    public readonly struct DodgeStagePoint
    {
        public static readonly DodgeStagePoint NotStarted =
            new DodgeStagePoint(-1, false, long.MaxValue, long.MaxValue, 0f, 0f, DodgeStageTable.AllKinds, 0);

        public readonly int Index;
        public readonly bool SuddenDeath;
        public readonly long StartTick;
        public readonly long EndTick;
        public readonly float Progress;
        public readonly float Intensity;
        public readonly DodgePatternKind[] Kinds;
        public readonly int IntervalTicks;

        public bool Started => Index >= 0;

        public DodgeStagePoint(int index, bool suddenDeath, long startTick, long endTick, float progress,
                               float intensity, DodgePatternKind[] kinds, int intervalTicks)
        {
            Index = index; SuddenDeath = suddenDeath; StartTick = startTick; EndTick = endTick;
            Progress = progress; Intensity = intensity; Kinds = kinds; IntervalTicks = intervalTicks;
        }
    }

    /// <summary>
    /// 스테이지 시간표. 경기 시작 틱과 표만으로 정해진다 — 서버 진행기와 클라 HUD가 같은 식을 각자 계산하므로
    /// 스테이지를 와이어에 싣지 않는다(위험 모양과 같은 원칙, 스펙 §3.1).
    /// </summary>
    public sealed class DodgeStageTable
    {
        public static readonly DodgePatternKind[] AllKinds =
        {
            DodgePatternKind.Ring, DodgePatternKind.Bomb, DodgePatternKind.BulletWall, DodgePatternKind.Laser,
            DodgePatternKind.BulletAimed, DodgePatternKind.Rock, DodgePatternKind.Spiral, DodgePatternKind.Tiles,
        };

        private readonly DodgeStage[] stages;

        public DodgeStageTable(IReadOnlyList<DodgeStage> stages)
        {
            this.stages = new DodgeStage[stages.Count];
            for (int i = 0; i < stages.Count; i++) this.stages[i] = stages[i];
        }

        public int Count => stages.Length;
        public DodgeStage this[int index] => stages[index];

        public DodgeStagePoint At(long tick, long gameplayStartTick, in DodgeConfig c)
        {
            // 시작 전엔 시작 틱이 long.MaxValue다 — 더하기 전에 걸러야 넘치지 않는다.
            if (gameplayStartTick == long.MaxValue || tick < gameplayStartTick)
            {
                return DodgeStagePoint.NotStarted;
            }

            long start = gameplayStartTick;
            for (int i = 0; i < stages.Length; i++)
            {
                var s = stages[i];
                long end = start + s.DurationTicks;
                if (tick < end)   // 경계 틱은 다음 스테이지 — [start, end)
                {
                    float progress = (float)(tick - start) / s.DurationTicks;
                    return new DodgeStagePoint(i, false, start, end, progress,
                                               s.BaseIntensity * (1f + progress * s.Tighten), s.Kinds, s.IntervalTicks);
                }
                start = end;
            }

            float seconds = (float)(tick - start) / DodgeConfig.TicksPerSecond;
            return new DodgeStagePoint(stages.Length, true, start, long.MaxValue, 0f,
                                       c.SuddenDeathBase * (1f + seconds * c.SuddenDeathGrowth), AllKinds,
                                       c.PatternIntervalTicks);
        }
    }
}
