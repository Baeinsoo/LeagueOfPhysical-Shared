using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 맵 씬에서 읽어 낸 <b>값</b>. 레인마다 사대 자리·보는 쪽·과녁 자리들을 담는다.
    /// 씬을 뒤지는 것부터 정렬·검증까지 <b>전부 여기 한 곳</b>에 둔다 — 클·서가 같은 씬에서
    /// 같은 값을 얻어야 과녁이 같은 자리에 선다.
    /// </summary>
    public sealed class ArcheryRangeLayout
    {
        public readonly struct Lane
        {
            /// <summary>사수가 서는 자리.</summary>
            public readonly Vector3 ShooterPosition;

            /// <summary>사대에서 과녁 쪽(단위 벡터). 과녁은 이 반대를 바라보고 선다.</summary>
            public readonly Vector3 Forward;

            /// <summary>과녁이 설 자리들(세계 좌표). 가까운 것부터 먼 순서.</summary>
            public readonly IReadOnlyList<Vector3> Stands;

            public Lane(Vector3 shooterPosition, Vector3 forward, IReadOnlyList<Vector3> stands)
            {
                ShooterPosition = shooterPosition;
                Forward = forward;
                Stands = stands;
            }
        }

        public IReadOnlyList<Lane> Lanes { get; }

        /// <summary>레인 하나가 가진 과녁 자리 수. 레인이 없으면 0이다.</summary>
        public int StandCount => Lanes.Count == 0 ? 0 : Lanes[0].Stands.Count;

        /// <summary>레인이 하나도 없나. 원형 맵이 그렇다.</summary>
        public bool IsEmpty => Lanes.Count == 0;

        private ArcheryRangeLayout(IReadOnlyList<Lane> lanes)
        {
            Lanes = lanes;
        }

        /// <summary>
        /// 지금 열려 있는 씬들에서 레인을 찾아 값으로 옮긴다. <b>맵 씬이 다 뜬 뒤에 불러야 한다</b> —
        /// 먼저 부르면 빈 레이아웃이 나오는데 에러가 안 난다.
        ///
        /// <para>씬을 뒤지는 이 한 줄도 공유 코드에 둔다 — 클·서가 <b>같은 조건</b>으로 찾아야
        /// 같은 레인 목록을 얻는다(비활성 오브젝트 포함 여부만 달라도 갈린다).</para>
        /// </summary>
        public static ArcheryRangeLayout FromOpenScenes()
        {
            return From(Object.FindObjectsByType<ArcheryLane>(
                FindObjectsInactive.Include, FindObjectsSortMode.None));
        }

        /// <summary>
        /// 씬에서 찾은 레인들을 값으로 옮긴다. <see cref="ArcheryLane.Order"/> 오름차순이고,
        /// Order가 같으면 오브젝트 이름으로 가른다 — 그러지 않으면 찾아온 순서가 그대로 남아
        /// 실행할 때마다 사수 배정이 바뀔 수 있다(<see cref="SpawnPlacement.Arrange"/>와 같은 규칙).
        /// </summary>
        public static ArcheryRangeLayout From(IEnumerable<ArcheryLane> lanes)
        {
            var result = new List<Lane>();
            if (lanes == null)
            {
                return new ArcheryRangeLayout(result);
            }

            var ordered = lanes
                .Where(lane => lane != null)
                .OrderBy(lane => lane.Order)
                .ThenBy(lane => lane.name, System.StringComparer.Ordinal)
                .ToList();

            for (int i = 0; i < ordered.Count; i++)
            {
                var lane = ordered[i];
                var stands = new List<Vector3>();
                if (lane.Stands != null)
                {
                    for (int s = 0; s < lane.Stands.Length; s++)
                    {
                        if (lane.Stands[s] == null)
                        {
                            throw new System.InvalidOperationException(
                                $"레인 '{lane.name}'의 과녁 자리 {s}가 비어 있다 — 씬에서 자리를 지우고 "
                                + "배열 칸을 안 지웠을 때 이렇게 된다");
                        }
                        stands.Add(lane.Stands[s].position);
                    }
                }

                if (i > 0 && stands.Count != result[0].Stands.Count)
                {
                    //  모두가 같은 순서로 같은 거리를 본다는 것이 이 맵의 전제다(같은 시험지).
                    //  한 레인만 자리가 모자라면 그 사수만 과녁이 안 뜨는데 에러가 안 난다.
                    throw new System.InvalidOperationException(
                        $"레인 '{lane.name}'의 과녁 자리가 {stands.Count}개인데 첫 레인은 "
                        + $"{result[0].Stands.Count}개다 — 레인마다 같아야 한다");
                }

                result.Add(new Lane(lane.transform.position, lane.transform.forward.normalized, stands));
            }

            return new ArcheryRangeLayout(result);
        }
    }
}
