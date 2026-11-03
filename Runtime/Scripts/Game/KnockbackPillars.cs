using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 기둥 자리와 "그 틱에 서 있나". 자리는 설정에서 계산하고(씬에 놓지 않는다 — 클·서가 늘 같은 배치),
    /// 서 있는지는 기둥 중심 아래 칸이 살아 있는지로 정한다(칸이 무너지면 기둥도 같이 가라앉는다).
    /// </summary>
    public sealed class KnockbackPillars
    {
        //  출발 자리(0도부터 45도 간격)와 겹치지 않게 반 칸 비켜 서고, 각은 KnockbackSpawn과 같게 +z가 0도다.
        private const float FirstAngleDegrees = 22.5f;
        private const float StepDegrees = 90f;

        private readonly KnockbackFloor floor;
        private readonly KnockbackPillarLayout layout;
        private readonly IMatchSeed seed;
        private readonly Vector2[] centers;
        private ulong syncedSeed;

        public int Count => centers.Length;
        /// <summary>고리를 돌린 각(도). 씨앗이 없으면 0.</summary>
        public float RotationDegrees { get; private set; }
        /// <summary>자리가 다시 계산될 때마다 오른다. 자리를 복사해 둔 쪽(콜라이더)이 바뀌었는지 알아보는 표.</summary>
        public int Version
        {
            get
            {
                Sync();
                return version;
            }
        }
        private int version;
        public float Radius { get; }
        public float Height { get; }

        /// <param name="seed">주면 판마다 이 씨앗으로 고리를 돌린다. 클라는 씨앗을 늦게 받으므로
        /// 만들 때 한 번 읽지 않고 쓸 때마다 확인해, 씨앗이 바뀌면 그때 자리를 다시 계산한다.</param>
        public KnockbackPillars(KnockbackPillarLayout layout, KnockbackFloor floor, IMatchSeed seed = null)
        {
            this.floor = floor;
            this.layout = layout;
            this.seed = seed;
            centers = new Vector2[Mathf.Max(0, layout.Count)];
            Radius = layout.Radius;
            Height = layout.Height;
            Recompute(seed?.Value ?? 0UL);
        }

        private void Sync()
        {
            if (seed != null && seed.Value != syncedSeed)
            {
                Recompute(seed.Value);
            }
        }

        private void Recompute(ulong matchSeed)
        {
            syncedSeed = matchSeed;
            RotationDegrees = seed == null ? 0f : KnockbackMapRandom.PillarRotationDegrees(matchSeed, centers.Length);
            for (int i = 0; i < centers.Length; i++)
            {
                float angle = (FirstAngleDegrees + StepDegrees * i + RotationDegrees) * Mathf.Deg2Rad;
                centers[i] = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * layout.RingRadius;
            }
            version++;
        }

        public Vector2 Center(int i)
        {
            Sync();
            return centers[i];
        }

        public bool IsStanding(int i, long tick)
        {
            Sync();
            return floor.IsSolid(centers[i].x, centers[i].y, tick);
        }

        // 원은 줄기만 해서 한번 무너진 바닥은 다시 서지 않는다 — 그래서 이진 탐색이 된다.
        private const long SearchLimitTicks = 1L << 24;

        /// <summary>이 기둥 아래 칸이 무너지는 틱(그 틱부터 쓰러져 있다). 끝내 안 무너지면 long.MaxValue.</summary>
        public long CollapseTick(int i)
        {
            if (!IsStanding(i, 0)) return 0;
            if (IsStanding(i, SearchLimitTicks)) return long.MaxValue;
            long lo = 0, hi = SearchLimitTicks;
            while (hi - lo > 1)
            {
                long mid = lo + (hi - lo) / 2;
                if (IsStanding(i, mid)) lo = mid; else hi = mid;
            }
            return hi;
        }

        /// <summary>그 틱에 서 있는 기둥들의 중심을 into에 담는다(비우고 시작).</summary>
        public void CollectStanding(long tick, List<Vector2> into)
        {
            into.Clear();
            Sync();
            for (int i = 0; i < centers.Length; i++)
            {
                if (IsStanding(i, tick))
                {
                    into.Add(centers[i]);
                }
            }
        }
    }
}
