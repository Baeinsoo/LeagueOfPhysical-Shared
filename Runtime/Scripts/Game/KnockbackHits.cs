using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>서버의 맞음 판정 식. 탄 목록과 서 있는 몸들만 받아 "누가 어느 탄에 맞았나"를 돌려준다.</summary>
    public static class KnockbackHits
    {
        /// <summary>
        /// 탄마다 이번 틱에 지나온 선분(앞 틱 자리 → 이번 틱 자리, 쏜 틱이면 원점 → 이번 틱 자리, 사거리 끝에서 자름)을
        /// 자기 아닌 몸의 원(몸 + 탄 반지름)에 대어, 가장 먼저 닿는 몸 하나만 고른다.
        /// 같은 틱에 두 탄이 한 몸을 맞히면 둘 다 나온다. 결과 순서는 탄 목록 순서다.
        /// 같은 선분에서 서 있는 기둥이 몸보다 먼저 닿으면 그 몸은 맞지 않는다(기둥 뒤에 숨기).
        /// 떨어지는 몸·탈락한 몸은 부르는 쪽이 targets에서 뺀다.
        /// </summary>
        /// <summary>탄이 그 틱에 지나온 선분(쏜 틱이면 원점 → 이번 자리). 서버 판정과 클라 기둥 막기가 같은 선분을 쓰게 한 곳에 둔다.</summary>
        public static void Segment(in KnockbackShot shot, long tick, KnockbackConfig cfg, float dt, out Vector2 from, out Vector2 to)
        {
            from = tick == shot.FireTick ? shot.Origin : shot.PositionWithinRange(tick - 1, cfg.ShotSpeed, cfg.ShotRange, dt);
            to = shot.PositionWithinRange(tick, cfg.ShotSpeed, cfg.ShotRange, dt);
        }

        /// <summary>탄이 그 틱 선분에서 몸의 원(몸 + 탄 반지름)에 처음 닿은 자리. 폭탄이 몸에 맞아 터지는 자리다.</summary>
        public static Vector2 HitPoint(in KnockbackShot shot, Vector2 bodyPosition, long tick, KnockbackConfig cfg, float dt)
        {
            Segment(shot, tick, cfg, dt, out var from, out var to);
            KnockbackHitTest.SegmentHitsCircle(from, to, bodyPosition, cfg.HitRadius + cfg.ShotRadius, out float t);
            return Vector2.Lerp(from, to, t);
        }

        public static List<(KnockbackShot shot, string victimId)> Find(
            IReadOnlyList<KnockbackShot> shots,
            IReadOnlyList<(string id, Vector2 position)> targets,
            IReadOnlyList<Vector2> standingPillars, float pillarRadius,
            long tick, KnockbackConfig cfg, float dt)
        {
            var hits = new List<(KnockbackShot, string)>();
            float reach = cfg.HitRadius + cfg.ShotRadius;
            float pillarReach = pillarRadius + cfg.ShotRadius;
            for (int i = 0; i < shots.Count; i++)
            {
                var shot = shots[i];
                if (tick < shot.FireTick)
                {
                    continue;
                }

                Segment(shot, tick, cfg, dt, out Vector2 from, out Vector2 to);

                float wall = float.MaxValue;
                for (int p = 0; p < standingPillars.Count; p++)
                {
                    if (KnockbackHitTest.SegmentHitsCircle(from, to, standingPillars[p], pillarReach, out float pt) && pt < wall)
                    {
                        wall = pt;
                    }
                }

                string victim = null;
                float best = float.MaxValue;
                for (int j = 0; j < targets.Count; j++)
                {
                    var target = targets[j];
                    if (target.id == shot.ShooterId)
                    {
                        continue;
                    }
                    if (KnockbackHitTest.SegmentHitsCircle(from, to, target.position, reach, out float t) && t < best && t < wall)
                    {
                        best = t;
                        victim = target.id;
                    }
                }
                if (victim != null)
                {
                    hits.Add((shot, victim));
                }
            }
            return hits;
        }
    }
}
