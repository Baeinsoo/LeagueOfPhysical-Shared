using System.Collections.Generic;

namespace LOP
{
    /// <summary>타격 하나의 사실 — 뒤집은 개수와 파울(낙) 여부. 파울이면 개수는 세지 않는다.</summary>
    public readonly struct PanchigiRoll
    {
        public readonly int Flipped;
        public readonly bool Foul;

        public PanchigiRoll(int flipped, bool foul = false)
        {
            Flipped = flipped;
            Foul = foul;
        }

        public static PanchigiRoll Fouled => new PanchigiRoll(0, true);

        public int Down => Foul ? 0 : System.Math.Max(0, Flipped);
    }

    /// <summary>다음 타격이 몇 번째 프레임의 몇 번째인가, 판에 동전이 몇 개 서 있나, 다 끝났나.</summary>
    public readonly struct PanchigiBowlingPosition
    {
        public readonly int Frame;
        public readonly int RollInFrame;
        public readonly int Standing;
        public readonly bool Complete;

        public PanchigiBowlingPosition(int frame, int rollInFrame, int standing, bool complete)
        {
            Frame = frame;
            RollInFrame = rollInFrame;
            Standing = standing;
            Complete = complete;
        }
    }

    /// <summary>점수판 한 칸. 누적은 보너스가 아직 안 정해졌으면 null.</summary>
    public sealed class PanchigiFrameView
    {
        public IReadOnlyList<string> Marks;
        public int? Cumulative;
    }

    /// <summary>
    /// 판치기 볼링 점수 — 동전이 핀이다. 프레임당 최대 두 번, 스트라이크는 다음 두 번, 스페어는 다음 한 번을 더하고
    /// 마지막 프레임은 스트라이크면 두 번, 스페어면 한 번 더 친다(볼링 10프레임과 같다).
    /// 서버는 순위를, 클라는 점수판을 이 코드 하나로 계산한다 — 표기(X / F -)도 여기서만 만든다.
    /// </summary>
    public static class PanchigiBowlingScore
    {
        private struct Walk
        {
            public List<List<int>> FrameRolls;   // 프레임마다 그 프레임에 속한 타격 번호
            public PanchigiBowlingPosition Position;
        }

        public static PanchigiBowlingPosition Locate(IReadOnlyList<PanchigiRoll> rolls, int frameCount, int pinCount)
        {
            return Run(rolls, frameCount, pinCount).Position;
        }

        public static List<PanchigiFrameView> Frames(IReadOnlyList<PanchigiRoll> rolls, int frameCount, int pinCount)
        {
            Walk walk = Run(rolls, frameCount, pinCount);
            var views = new List<PanchigiFrameView>(frameCount);
            int? running = 0;

            for (int f = 0; f < frameCount; f++)
            {
                List<int> mine = f < walk.FrameRolls.Count ? walk.FrameRolls[f] : new List<int>();
                int? score = FrameScore(rolls, mine, f == frameCount - 1, walk.Position.Complete, pinCount);
                running = running.HasValue && score.HasValue ? running + score : null;

                views.Add(new PanchigiFrameView { Marks = FrameMarks(rolls, mine, pinCount), Cumulative = running });
            }

            return views;
        }

        public static int Total(IReadOnlyList<PanchigiRoll> rolls, int frameCount, int pinCount)
        {
            int total = 0;
            foreach (PanchigiFrameView view in Frames(rolls, frameCount, pinCount))
            {
                if (view.Cumulative.HasValue) { total = view.Cumulative.Value; }
            }
            return total;
        }

        private static Walk Run(IReadOnlyList<PanchigiRoll> rolls, int frameCount, int pinCount)
        {
            var frames = new List<List<int>> { new List<int>() };
            int frame = 0;
            int standing = pinCount;

            for (int i = 0; i < rolls.Count && frame < frameCount; i++)
            {
                int down = System.Math.Min(rolls[i].Down, standing);
                standing -= down;
                List<int> current = frames[frame];
                current.Add(i);

                bool last = frame == frameCount - 1;
                bool frameOver;
                if (last == false)
                {
                    frameOver = standing == 0 || current.Count == 2;
                }
                else if (current.Count == 1)
                {
                    frameOver = false;
                }
                else if (current.Count == 2)
                {
                    int first = rolls[current[0]].Down;
                    bool bonus = first == pinCount || first + rolls[current[1]].Down == pinCount;
                    frameOver = bonus == false;
                }
                else
                {
                    frameOver = true;
                }

                if (frameOver)
                {
                    frame++;
                    standing = pinCount;
                    if (frame < frameCount) { frames.Add(new List<int>()); }
                }
                else if (standing == 0)
                {
                    standing = pinCount;   // 마지막 프레임 보너스 — 다 뒤집혔으면 다시 세운다
                }
            }

            bool complete = frame >= frameCount;
            int rollInFrame = complete ? 0 : frames[frame].Count;
            return new Walk
            {
                FrameRolls = frames,
                Position = new PanchigiBowlingPosition(complete ? frameCount : frame, rollInFrame, complete ? 0 : standing, complete),
            };
        }

        private static int? FrameScore(IReadOnlyList<PanchigiRoll> rolls, List<int> mine, bool last, bool complete, int pinCount)
        {
            if (mine.Count == 0) { return null; }

            if (last)
            {
                if (complete == false) { return null; }
                int sum = 0;
                foreach (int i in mine) { sum += rolls[i].Down; }
                return sum;
            }

            int first = rolls[mine[0]].Down;
            int start = mine[0];
            if (mine.Count == 1 && first == pinCount)
            {
                return Bonus(rolls, start + 1, 2, pinCount);
            }
            if (mine.Count < 2) { return null; }

            int two = first + rolls[mine[1]].Down;
            return two == pinCount ? Bonus(rolls, start + 2, 1, pinCount) : two;
        }

        private static int? Bonus(IReadOnlyList<PanchigiRoll> rolls, int from, int count, int pinCount)
        {
            if (from + count > rolls.Count) { return null; }
            int sum = pinCount;
            for (int i = 0; i < count; i++) { sum += rolls[from + i].Down; }
            return sum;
        }

        private static List<string> FrameMarks(IReadOnlyList<PanchigiRoll> rolls, List<int> mine, int pinCount)
        {
            var marks = new List<string>();
            int rackDown = 0;
            bool rackFirst = true;

            foreach (int i in mine)
            {
                PanchigiRoll roll = rolls[i];
                int down = roll.Down;

                if (roll.Foul) { marks.Add("F"); }
                else if (rackFirst && down == pinCount) { marks.Add("X"); }
                else if (rackFirst == false && rackDown + down == pinCount) { marks.Add("/"); }
                else if (down == 0) { marks.Add("-"); }
                else { marks.Add(down.ToString()); }

                rackDown += down;
                rackFirst = false;
                if (rackDown >= pinCount)
                {
                    rackDown = 0;     // 마지막 프레임 보너스 — 판을 다시 세웠다
                    rackFirst = true;
                }
            }

            return marks;
        }
    }
}
