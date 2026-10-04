using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 탄막 투척 심판의 동선. 탄막 종류가 있는 스테이지(스테이지 1·서든데스)가 열리면 북쪽 관중석 너머(Gate)에서
    /// 뛰어 들어와 가운데(Spot)에 서서 쏘고, 그 스테이지가 끝나면 같은 길로 나간다. (틱, 경기 시작, 표)만으로 정해진다 —
    /// 서버는 이 자리에 심판 캐릭터를 세우고(몸이 선수를 막는다), 진행기는 이 자리에서 쏘고, 시뮬도 같은 식을 쓴다.
    /// </summary>
    public static class DodgeReferee
    {
        public static readonly Vector2 Spot = Vector2.zero;
        /// <summary>대기 자리 — 북쪽 관중석(z 10.2~13.8) 너머. 화면 위쪽 끝.</summary>
        public static readonly Vector2 Gate = new Vector2(0f, 14.5f);
        public const float WalkSeconds = 2.5f;

        public readonly struct Pose
        {
            public readonly Vector2 Position;
            public readonly Vector2 Velocity;

            public Pose(Vector2 position, Vector2 velocity)
            {
                Position = position;
                Velocity = velocity;
            }
        }

        public static bool Throws(in DodgeStagePoint at)
        {
            if (!at.Started) return false;
            foreach (var k in at.Kinds)
            {
                if (k == DodgePatternKind.Ring || k == DodgePatternKind.Spiral) return true;
            }
            return false;
        }

        public static Pose PoseAt(long tick, long gameplayStartTick, DodgeStageTable stages, in DodgeConfig c)
        {
            var at = stages.At(tick, gameplayStartTick, c);
            if (!at.Started) return new Pose(Gate, Vector2.zero);

            long since = tick - at.StartTick;
            int walk = DodgeConfig.Ticks(WalkSeconds);
            if (Throws(at))
            {
                return since >= walk ? new Pose(Spot, Vector2.zero) : Walking(Gate, Spot, since, walk);
            }
            // 탄막이 아닌 스테이지 — 바로 앞 스테이지가 탄막이었으면 나가는 중.
            bool prevThrew = at.Index > 0 && Throws(stages.At(at.StartTick - 1, gameplayStartTick, c));
            return prevThrew && since < walk ? Walking(Spot, Gate, since, walk) : new Pose(Gate, Vector2.zero);
        }

        private static Pose Walking(Vector2 from, Vector2 to, long since, int walk)
        {
            float t = since / (float)walk;
            return new Pose(Vector2.Lerp(from, to, t), (to - from) / (walk / (float)DodgeConfig.TicksPerSecond));
        }
    }
}
