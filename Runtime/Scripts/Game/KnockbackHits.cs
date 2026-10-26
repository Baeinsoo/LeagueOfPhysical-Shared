using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>서버의 맞음 판정 식. 탄 목록과 서 있는 몸들만 받아 "누가 어느 탄에 맞았나"를 돌려준다.</summary>
    public static class KnockbackHits
    {
        /// <summary>
        /// 탄마다 이번 틱에 지나온 선분(앞 틱 자리 → 이번 틱 자리, 쏜 틱이면 원점 → 이번 틱 자리)을
        /// 자기 아닌 몸의 원(몸 + 탄 반지름)에 대어, 가장 먼저 닿는 몸 하나만 고른다.
        /// 같은 틱에 두 탄이 한 몸을 맞히면 둘 다 나온다. 결과 순서는 탄 목록 순서다.
        /// 떨어지는 몸·탈락한 몸은 부르는 쪽이 targets에서 뺀다.
        /// </summary>
        public static List<(KnockbackShot shot, string victimId)> Find(
            IReadOnlyList<KnockbackShot> shots,
            IReadOnlyList<(string id, Vector2 position)> targets,
            long tick, KnockbackConfig cfg, float dt)
        {
            var hits = new List<(KnockbackShot, string)>();
            float reach = cfg.HitRadius + cfg.ShotRadius;
            for (int i = 0; i < shots.Count; i++)
            {
                var shot = shots[i];
                if (tick < shot.FireTick)
                {
                    continue;
                }

                Vector2 from = tick == shot.FireTick ? shot.Origin : shot.PositionAt(tick - 1, cfg.ShotSpeed, dt);
                Vector2 to = shot.PositionAt(tick, cfg.ShotSpeed, dt);

                string victim = null;
                float best = float.MaxValue;
                for (int j = 0; j < targets.Count; j++)
                {
                    var target = targets[j];
                    if (target.id == shot.ShooterId)
                    {
                        continue;
                    }
                    if (KnockbackHitTest.SegmentHitsCircle(from, to, target.position, reach, out float t) && t < best)
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
