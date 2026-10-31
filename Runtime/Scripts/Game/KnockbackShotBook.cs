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

        /// <summary>그 틱에 지나온 선분이 서 있는 기둥에 닿은 탄을 지우고, 지운 수를 돌려준다.</summary>
        public int RemoveBlocked(long tick, IReadOnlyList<UnityEngine.Vector2> standingPillars, float pillarRadius,
                                 KnockbackConfig cfg, float dt)
        {
            if (standingPillars.Count == 0)
            {
                return 0;
            }
            float reach = pillarRadius + cfg.ShotRadius;
            return shots.RemoveAll(s =>
            {
                if (tick < s.FireTick)
                {
                    return false;
                }
                var from = tick == s.FireTick ? s.Origin : s.PositionWithinRange(tick - 1, cfg.ShotSpeed, cfg.ShotRange, dt);
                var to = s.PositionWithinRange(tick, cfg.ShotSpeed, cfg.ShotRange, dt);
                for (int p = 0; p < standingPillars.Count; p++)
                {
                    if (KnockbackHitTest.SegmentHitsCircle(from, to, standingPillars[p], reach, out _))
                    {
                        return true;
                    }
                }
                return false;
            });
        }

        public List<KnockbackShot> Capture() => new List<KnockbackShot>(shots);

        public void Restore(List<KnockbackShot> saved)
        {
            shots.Clear();
            shots.AddRange(saved);
        }
    }
}
