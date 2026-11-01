using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// 날아가는 넉백 탄 목록. 중복 거르기, 다 쓴 탄 정리, 되감기용 복사/복원을 맡는다.
    /// (발사 틱, 쏜 사람) 순으로 정렬해 두어 클·서가 같은 순서로 탄을 처리한다.
    /// </summary>
    public sealed class KnockbackShotBook
    {
        private readonly List<KnockbackShot> shots = new List<KnockbackShot>();

        public IReadOnlyList<KnockbackShot> Shots => shots;

        static int Compare(in KnockbackShot a, in KnockbackShot b)
        {
            int c = a.FireTick.CompareTo(b.FireTick);
            return c != 0 ? c : string.CompareOrdinal(a.ShooterId, b.ShooterId);
        }

        /// <summary>같은 (쏜 사람, 발사 틱)이 이미 있으면 false — 재전송·재접속으로 같은 사건이 또 올 수 있다.</summary>
        public bool Add(in KnockbackShot shot)
        {
            int at = shots.Count;
            for (int i = 0; i < shots.Count; i++)
            {
                if (shots[i].FireTick == shot.FireTick && shots[i].ShooterId == shot.ShooterId) return false;
                if (at == shots.Count && Compare(shot, shots[i]) < 0) at = i;
            }
            shots.Insert(at, shot);
            return true;
        }

        public bool Remove(string shooterId, long fireTick)
        {
            for (int i = 0; i < shots.Count; i++)
            {
                if (shots[i].FireTick == fireTick && shots[i].ShooterId == shooterId)
                {
                    shots.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public void RemoveSpent(long tick, float speed, float range, float dt)
        {
            shots.RemoveAll(s => s.IsSpent(tick, speed, range, dt));
        }

        private readonly List<UnityEngine.Vector2> standingScratch = new List<UnityEngine.Vector2>();

        /// <summary>
        /// 쏜 틱부터 tick까지 각 틱의 선분이 "그 틱에 서 있던" 기둥에 닿은 탄을 지우고, 지운 수를 돌려준다.
        /// 남의 탄은 왕복 시간만큼 늦게 도착해 이미 기둥을 지나 있을 수 있어서, 한 틱이 아니라 지나온 길 전체를 다시 훑는다.
        /// 결과는 틱과 탄 사실만으로 정해지니 되감아 다시 해도 같다.
        /// </summary>
        public int RemoveBlocked(long tick, System.Action<long, List<UnityEngine.Vector2>> collectStanding, float pillarRadius,
                                 KnockbackConfig cfg, float dt)
        {
            if (shots.Count == 0)
            {
                return 0;
            }
            long first = long.MaxValue;
            for (int i = 0; i < shots.Count; i++)
            {
                if (shots[i].FireTick < first) first = shots[i].FireTick;
            }

            float reach = pillarRadius + cfg.ShotRadius;
            var blocked = new HashSet<int>();
            for (long k = first; k <= tick; k++)
            {
                collectStanding(k, standingScratch);
                if (standingScratch.Count == 0)
                {
                    continue;
                }
                for (int i = 0; i < shots.Count; i++)
                {
                    if (k < shots[i].FireTick || blocked.Contains(i))
                    {
                        continue;
                    }
                    KnockbackHits.Segment(shots[i], k, cfg, dt, out var from, out var to);
                    for (int p = 0; p < standingScratch.Count; p++)
                    {
                        if (KnockbackHitTest.SegmentHitsCircle(from, to, standingScratch[p], reach, out _))
                        {
                            blocked.Add(i);
                            break;
                        }
                    }
                }
            }

            int index = 0;
            return shots.RemoveAll(_ => blocked.Contains(index++));
        }

        public List<KnockbackShot> Capture() => new List<KnockbackShot>(shots);

        public void Restore(List<KnockbackShot> saved)
        {
            shots.Clear();
            shots.AddRange(saved);
        }
    }
}
