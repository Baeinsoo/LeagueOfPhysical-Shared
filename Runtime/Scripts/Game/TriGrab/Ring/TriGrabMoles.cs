using System;
using System.Collections.Generic;
using System.Numerics;

namespace LOP.TriGrab
{
    /// <summary>
    /// 두더지 한 마리. 모양은 틱만 넣으면 나오는 식이라 따로 상태를 들고 있지 않는다.
    /// 솟기 → 서 있기 → 숨기. 맞으면 서 있는 시간이 짧아진다.
    /// </summary>
    public sealed class TriGrabMole
    {
        public const int RiseTicks = 8;
        public const int SinkTicks = 10;
        public const int BonkedStandTicks = 23;

        public int Id;
        public Vector2 Pos;
        public long SpawnTick;
        /// <summary>솟기를 포함한, 맞지 않았을 때 서 있는 시간의 끝(생긴 지 몇 틱).</summary>
        public int StandTicks;
        public long BonkTick = -1;
        public int SayIndex;
        public int BonkLineIndex;

        long StandEndAge => BonkTick >= 0 ? BonkTick - SpawnTick + BonkedStandTicks : StandTicks;

        /// <summary>0(땅속)~1(다 솟음).</summary>
        public float HeightAt(long tick)
        {
            long age = tick - SpawnTick;
            if (age < 0) return 0f;
            if (age < RiseTicks) return (float)age / RiseTicks;
            long end = StandEndAge;
            if (age < end) return 1f;
            return Math.Max(0f, 1f - (float)(age - end) / SinkTicks);
        }

        /// <summary>서 있고 아직 안 맞은 두더지만 카드를 막는다.</summary>
        public bool Blocks(long tick) => BonkTick < 0 && HeightAt(tick) > 0.5f;

        public bool GoneAt(long tick) => tick - SpawnTick > RiseTicks && HeightAt(tick) <= 0f;
    }

    public static class TriGrabMoles
    {
        public static readonly string[] SayLines = { "?", "!", "안녕", "ㅎㅎ", "…" };
        public static readonly string[] BonkLines = { "뿅!", "꽥!", "아야", "왜 나를", "두더지입니다", "잡지 마요" };
    }

    public sealed class TriGrabMoleSpawner
    {
        public const int MaxMoles = 2;
        public const int MinGapTicks = 40;
        public const int MaxGapTicks = 110;
        public const int MinStandTicks = 55;
        public const int MaxStandTicks = 100;
        public const float MinSpawnRadius = 1.0f;
        public const float SpawnEdgeMargin = 0.4f;
        public const float CardClearance = TriGrabConfig.CardBumpRadius + TriGrabConfig.MoleRadius + 0.1f;
        public const float MoleClearance = 1.4f;
        public const int SpawnTries = 60;

        public long NextTick;
        public int NextId;

        public void Step(List<TriGrabMole> moles, IList<TriGrabCard> onDisc, long tick, Random rng)
        {
            moles.RemoveAll(m => m.GoneAt(tick));
            if (tick < NextTick || moles.Count >= MaxMoles) return;

            NextTick = tick + MinGapTicks + rng.Next(MaxGapTicks - MinGapTicks + 1);
            float maxR = TriGrabConfig.CardWallRadius - SpawnEdgeMargin;
            for (int i = 0; i < SpawnTries; i++)
            {
                float ang = (float)(rng.NextDouble() * Math.PI * 2);
                float r = MinSpawnRadius + (float)rng.NextDouble() * (maxR - MinSpawnRadius);
                var pos = new Vector2((float)Math.Cos(ang) * r, (float)Math.Sin(ang) * r);
                if (!ClearOfCards(pos, onDisc) || !ClearOfMoles(pos, moles)) continue;
                moles.Add(new TriGrabMole
                {
                    Id = NextId++,
                    Pos = pos,
                    SpawnTick = tick,
                    StandTicks = MinStandTicks + rng.Next(MaxStandTicks - MinStandTicks + 1),
                    SayIndex = rng.Next(TriGrabMoles.SayLines.Length),
                    BonkLineIndex = rng.Next(TriGrabMoles.BonkLines.Length),
                });
                return;
            }
        }

        static bool ClearOfCards(Vector2 pos, IList<TriGrabCard> cards)
        {
            for (int i = 0; i < cards.Count; i++)
                if (Vector2.Distance(cards[i].Pos, pos) <= CardClearance) return false;
            return true;
        }

        static bool ClearOfMoles(Vector2 pos, List<TriGrabMole> moles)
        {
            for (int i = 0; i < moles.Count; i++)
                if (Vector2.Distance(moles[i].Pos, pos) <= MoleClearance) return false;
            return true;
        }
    }
}
