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

        public List<KnockbackShot> Capture() => new List<KnockbackShot>(shots);

        public void Restore(List<KnockbackShot> saved)
        {
            shots.Clear();
            shots.AddRange(saved);
        }
    }
}
