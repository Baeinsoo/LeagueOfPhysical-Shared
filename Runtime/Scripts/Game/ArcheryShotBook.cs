using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// 떠 있는 화살 목록. 수명 정리, 서버가 취소한 발 걸러내기, 되감기용 복사/복원을 맡는다.
    /// 화살은 엔티티가 아니라 발사 정보만 있으면 위치가 계산되므로 레지스트리에 넣지 않는다.
    /// </summary>
    public sealed class ArcheryShotBook
    {
        private readonly List<ArcheryShot> shots = new List<ArcheryShot>();

        //  서버가 "그 사이엔 활을 못 들었다"고 확정해 지운 내 화살. 되감기 저장본에는 남아 있으므로
        //  복원할 때 다시 걸러야 한다. 서버 확정 사실이라 되감기 밖에 둔다(ArcheryConsumed와 같은 이유).
        private readonly HashSet<(string shooterId, long fireTick)> cancelled = new HashSet<(string, long)>();

        public IReadOnlyList<ArcheryShot> Shots => shots;

        public void Add(in ArcheryShot shot)
        {
            shots.Add(shot);
        }

        /// <summary>
        /// 그 사람이 (<paramref name="afterTick"/>, <paramref name="upToTick"/>] 구간에 쏜 화살을 지운다.
        /// </summary>
        public void Cancel(string shooterId, long afterTick, long upToTick)
        {
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var shot = shots[i];
                if (shot.ShooterId == shooterId && shot.FireTick > afterTick && shot.FireTick <= upToTick)
                {
                    cancelled.Add((shot.ShooterId, shot.FireTick));
                    shots.RemoveAt(i);
                }
            }
        }

        // 화면 밖으로 나간 화살을 계속 들고 있으면 목록이 한 판 내내 자란다.
        public void RemoveExpired(long tick, float tickInterval)
        {
            float lifetimeTicks = ArcheryTrajectory.LifetimeSeconds / tickInterval;
            for (int i = shots.Count - 1; i >= 0; i--)
            {
                if (tick - shots[i].FireTick > lifetimeTicks)
                {
                    shots.RemoveAt(i);
                }
            }
            cancelled.RemoveWhere(c => tick - c.fireTick > lifetimeTicks);
        }

        public List<ArcheryShot> Capture() => new List<ArcheryShot>(shots);

        public void Restore(List<ArcheryShot> saved)
        {
            shots.Clear();
            shots.AddRange(saved);
            shots.RemoveAll(s => cancelled.Contains((s.ShooterId, s.FireTick)));
        }
    }
}
