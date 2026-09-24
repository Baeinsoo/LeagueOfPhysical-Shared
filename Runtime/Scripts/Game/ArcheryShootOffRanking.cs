using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>한 라운드에서 한 사수가 남긴 결과. 거리·좌표는 미터다.</summary>
    public readonly struct ArcheryRoundShot
    {
        public readonly string ShooterId;
        public readonly bool Hit;
        public readonly Vector2 FaceOffset;
        public readonly float Distance;

        public ArcheryRoundShot(string shooterId, bool hit, Vector2 faceOffset, float distance)
        {
            ShooterId = shooterId;
            Hit = hit;
            FaceOffset = faceOffset;
            Distance = distance;
        }
    }

    /// <summary>순위가 매겨진 결과. <see cref="Rank"/>는 0이 1등이다.</summary>
    public readonly struct ArcheryRoundPlacement
    {
        public readonly string ShooterId;
        public readonly bool Hit;
        public readonly Vector2 FaceOffset;
        public readonly float Distance;
        public readonly int Rank;
        public readonly int Points;

        public ArcheryRoundPlacement(string shooterId, bool hit, Vector2 faceOffset, float distance,
                                     int rank, int points)
        {
            ShooterId = shooterId;
            Hit = hit;
            FaceOffset = faceOffset;
            Distance = distance;
            Rank = rank;
            Points = points;
        }
    }

    /// <summary>
    /// 한 발 승부의 라운드 순위. 가운데에 가까운 순서로 <c>(인원 − 1 − 순위) × 배수</c>점을 준다.
    /// 못 맞힌 사람은 맞힌 사람 모두의 뒤, 공동 꼴찌로 0점이다.
    /// </summary>
    public static class ArcheryShootOffRanking
    {
        public static List<ArcheryRoundPlacement> Rank(IReadOnlyList<ArcheryRoundShot> shots, int multiplier)
        {
            var sorted = new List<ArcheryRoundShot>(shots);
            //  결과 순서가 입력 순서(엔티티 순회)에 기대지 않게 id로 끝까지 가른다.
            sorted.Sort((a, b) =>
            {
                if (a.Hit != b.Hit)
                {
                    return a.Hit ? -1 : 1;
                }
                int byDistance = a.Hit ? a.Distance.CompareTo(b.Distance) : 0;
                return byDistance != 0 ? byDistance : string.CompareOrdinal(a.ShooterId, b.ShooterId);
            });

            int count = sorted.Count;
            var result = new List<ArcheryRoundPlacement>(count);
            for (int i = 0; i < count; i++)
            {
                var shot = sorted[i];
                int rank;
                int points;
                if (shot.Hit)
                {
                    //  공동 순위: 나보다 확실히 가까운 사람 수가 내 순위다.
                    rank = 0;
                    while (rank < i && sorted[rank].Distance < shot.Distance)
                    {
                        rank++;
                    }
                    points = (count - 1 - rank) * multiplier;
                }
                else
                {
                    rank = count - 1;
                    points = 0;
                }
                result.Add(new ArcheryRoundPlacement(shot.ShooterId, shot.Hit, shot.FaceOffset,
                                                     shot.Distance, rank, points));
            }
            return result;
        }
    }
}
