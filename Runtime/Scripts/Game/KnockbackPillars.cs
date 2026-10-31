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
        private readonly Vector2[] centers;

        public int Count => centers.Length;
        public float Radius { get; }
        public float Height { get; }

        public KnockbackPillars(KnockbackPillarLayout layout, KnockbackFloor floor)
        {
            this.floor = floor;
            int count = Mathf.Max(0, layout.Count);
            centers = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                float angle = (FirstAngleDegrees + StepDegrees * i) * Mathf.Deg2Rad;
                centers[i] = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * layout.RingRadius;
            }
            Radius = layout.Radius;
            Height = layout.Height;
        }

        public Vector2 Center(int i) => centers[i];

        public bool IsStanding(int i, long tick) => floor.IsSolid(centers[i].x, centers[i].y, tick);

        /// <summary>그 틱에 서 있는 기둥들의 중심을 into에 담는다(비우고 시작).</summary>
        public void CollectStanding(long tick, List<Vector2> into)
        {
            into.Clear();
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
